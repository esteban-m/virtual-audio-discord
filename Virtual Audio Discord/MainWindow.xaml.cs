using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows;
using Discord;
using Discord.WebSocket;
using ManagedBass;
using ManagedBass.Wasapi;
using Discord.Audio;
using System.Runtime.InteropServices;
using Configuration = System.Configuration.Configuration;

namespace WPFVoicemeeterDiscord
{
    public partial class MainWindow : Window
    {
        private DiscordSocketClient _client;

        public MainWindow()
        {
            InitializeComponent();
            try
            {
                PopulateVoicemeeterInputs();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing Voicemeeter inputs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Load saved token
            var botTokenSetting = ConfigurationManager.AppSettings["BotToken"];
            if (botTokenSetting != null)
            {
                BotTokenTextBox.Text = botTokenSetting;
            }
        }

        private void PopulateVoicemeeterInputs()
        {
            for (int i = 0; i < BassWasapi.DeviceCount; i++)
            {
                var deviceInfo = BassWasapi.GetDeviceInfo(i);
                if (deviceInfo.IsEnabled && deviceInfo.IsLoopback)
                {
                    VoicemeeterInputComboBox.Items.Add(new CustomWasapiDeviceInfo { Index = i, DeviceInfo = deviceInfo });
                }
            }
            if (VoicemeeterInputComboBox.Items.Count > 0) VoicemeeterInputComboBox.SelectedIndex = 0;
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            string botToken = BotTokenTextBox.Text;
            if (string.IsNullOrWhiteSpace(botToken))
            {
                MessageBox.Show("Please enter a valid bot token.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ConnectButton.IsEnabled = false;
            SaveBotToken(botToken);
            await ConnectToDiscordAsync(botToken);
            ConnectButton.IsEnabled = true;
        }

        private void SaveBotToken(string botToken)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var settings = config.AppSettings.Settings;

            if (settings["BotToken"] == null) settings.Add("BotToken", botToken);
            else settings["BotToken"].Value = botToken;

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        private async Task ConnectToDiscordAsync(string botToken)
        {
            _client = new DiscordSocketClient();
            _client.Log += msg => { Console.WriteLine(msg.ToString()); return Task.CompletedTask; };

            await _client.LoginAsync(TokenType.Bot, botToken);
            await _client.StartAsync();

            _client.Ready += () =>
            {
                Dispatcher.Invoke( async () =>
                {
                    var serverSelectionWindow = new ServerSelectionWindow(_client.Guilds);
                    var result = serverSelectionWindow.ShowDialog();

                    if (result.HasValue && result.Value)
                    {
                        // Le serveur sélectionné est disponible via serverSelectionWindow.SelectedServer

                        var voiceChannelSelectionWindow = new VoiceChannelSelectionWindow(serverSelectionWindow.SelectedServer.VoiceChannels);
                        var voiceChannelResult = voiceChannelSelectionWindow.ShowDialog();

                        if (voiceChannelResult.HasValue && voiceChannelResult.Value)
                        {
                            await voiceChannelSelectionWindow.SelectedVoiceChannel.ConnectAsync();
                            if (VoicemeeterInputComboBox.SelectedItem is CustomWasapiDeviceInfo selectedInputDevice)
                            {
                                await StreamAudioAsync(voiceChannelSelectionWindow.SelectedVoiceChannel, selectedInputDevice);
                            }
                        }
                    }
                });
                return Task.CompletedTask;
            };
            await Task.Delay(-1);
        }

        private async Task StreamAudioAsync(SocketVoiceChannel voiceChannel, CustomWasapiDeviceInfo outputDevice)
        {
            using (var client = await voiceChannel.ConnectAsync())
            {
                BassWasapi.Init(outputDevice.Index, 48000, 2);
                BassWasapi.Start();

                int bufferSize = 48000 * 2 * 2 / 50; // 50ms buffer
                byte[] buffer = new byte[bufferSize];

                while (true)
                {
                    GCHandle hGC = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    int bytesRead = BassWasapi.GetData(hGC.AddrOfPinnedObject(), bufferSize);
                    hGC.Free();
                    if (bytesRead < 0) break;

                    // Send the PCM data to the Discord stream
                    var audioStream = client.CreatePCMStream(AudioApplication.Mixed);
                    await audioStream.WriteAsync(buffer, 0, bytesRead);
                }

                BassWasapi.Stop();
            }
        }
    }

    public class CustomWasapiDeviceInfo
    {
        public int Index { get; set; }
        public WasapiDeviceInfo DeviceInfo { get; set; }

        public override string ToString()
        {
            return DeviceInfo.Name;
        }
    }
}