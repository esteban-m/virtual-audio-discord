using System;
using System.Collections.Generic;
using System.Windows;
using Discord;
using Discord.WebSocket;

namespace WPFVoicemeeterDiscord
{
    public partial class ServerSelectionWindow : Window
    {
        public SocketGuild SelectedServer { get; private set; }

        public ServerSelectionWindow(IEnumerable<SocketGuild> servers)
        {
            InitializeComponent();
            PopulateServerListBox(servers);
        }

        private void PopulateServerListBox(IEnumerable<SocketGuild> servers)
        {
            foreach (var server in servers)
            {
                ServerListBox.Items.Add(server);
            }
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedServer = ServerListBox.SelectedItem as SocketGuild;

            if (SelectedServer != null)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please select a server.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
