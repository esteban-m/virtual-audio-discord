using System;
using System.Collections.Generic;
using System.Windows;
using Discord;
using Discord.WebSocket;

namespace WPFVoicemeeterDiscord
{
    public partial class VoiceChannelSelectionWindow : Window
    {
        public SocketVoiceChannel SelectedVoiceChannel { get; private set; }

        public VoiceChannelSelectionWindow(IEnumerable<SocketVoiceChannel> voiceChannels)
        {
            InitializeComponent();
            PopulateVoiceChannelListBox(voiceChannels);
        }

        private void PopulateVoiceChannelListBox(IEnumerable<SocketVoiceChannel> voiceChannels)
        {
            foreach (var voiceChannel in voiceChannels)
            {
                VoiceChannelListBox.Items.Add(voiceChannel);
            }
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedVoiceChannel = VoiceChannelListBox.SelectedItem as SocketVoiceChannel;

            if (SelectedVoiceChannel != null)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please select a voice channel.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
