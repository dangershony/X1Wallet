using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Obsidian.Features.X1Wallet.NodeUI.Logging;

namespace Obsidian.Features.X1Wallet.NodeUI
{
    

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public sealed partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();
            MyLoggerProvider.Notify = ReceiveNotify;
            this.MyListViewBinding = new ObservableCollection<string>();
            this.ListViewLogs.ItemsSource = this.MyListViewBinding;
            this.Title = $"X1 Noderunner - ObsidianX";
            this.TbNodeStats.Text = $"{x1d.Util.Init.GetName()} - The node is turned off.";
            MakeAllHiddenExcept(nameof(this.BorderNodeStatsVisibility));
            Program.Started += Program_Started; Program.Stopped += Program_Stopped;
        }

        private void Program_Started(object sender, EventArgs e)
        {
            this.ButtonStop.Dispatcher?.Invoke(() =>
            {
                this.ButtonStop.IsEnabled = true;
            });
        }

        private void Program_Stopped(object sender, EventArgs e)
        {
            this.ButtonStart.Dispatcher?.Invoke(() =>
            {
                this.ButtonStart.IsEnabled = true;
            });
        }
      

        public ObservableCollection<string> MyListViewBinding { get; set; }

        public Visibility BorderNodeStatsVisibility { get; set; }
        public Visibility BorderLogVisibility { get; set; }
        public Visibility BorderMoreVisibility { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void ReceiveNotify()
        {
            while (MyLoggerProvider.Logs.TryDequeue(out string line))
            {
                var line1 = line;
                this.ListViewLogs.Dispatcher?.Invoke(() =>
                {
                    this.TbNodeStats.Text = MyLoggerProvider.NodeStats;

                    this.MyListViewBinding.Add(line1);
                    if (VisualTreeHelper.GetChildrenCount(this.ListViewLogs) > 0)
                    {
                        var child = VisualTreeHelper.GetChild(this.ListViewLogs, 0);
                        ScrollViewer scrollViewer = child as ScrollViewer;

                        scrollViewer?.ScrollToBottom();
                    }

                });
            }
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            this.ButtonStart.IsEnabled = false;
            var _ = Task.Run(() => Program.Start(new string[0]));
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            this.ButtonStop.IsEnabled = false;
            Program.FullNode.NodeService<WalletController>().ShutDown();
        }

        private void Wallet_Click(object sender, RoutedEventArgs e)
        {
            string path = "";
            try
            {
                path = NodeRunnerSettings.LoadSettings().UiPath;
                var process = new Process();
                process.StartInfo.FileName = path;
                process.StartInfo.UseShellExecute = true;
                process.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Can't start X1.exe at path '{path}': {ex.Message}");
            }
          
        }

        private void NodeStats_Click(object sender, RoutedEventArgs e)
        {
            MakeAllHiddenExcept(nameof(this.BorderNodeStatsVisibility));
        }

        private void Log_Click(object sender, RoutedEventArgs e)
        {
            MakeAllHiddenExcept(nameof(this.BorderLogVisibility));
        }

        private void More_Click(object sender, RoutedEventArgs e)
        {
            MakeAllHiddenExcept(nameof(this.BorderMoreVisibility));
            var settings = NodeRunnerSettings.LoadSettings();
            this.TextBoxArguments.Text =settings.Arguments;
            this.TextBoxPath.Text = settings.UiPath;
        }

        private void ResetSettings_Click(object sender, RoutedEventArgs e)
        {
            var settings = NodeRunnerSettings.ResetSettings();
            this.TextBoxArguments.Text = settings.Arguments;
            this.TextBoxPath.Text = settings.UiPath;
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            NodeRunnerSettings.SaveSettings(new NodeRunnerSettings
                {Arguments = this.TextBoxArguments.Text, UiPath = this.TextBoxPath.Text});
        }

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            More_Click(sender, e);
        }


        void MakeAllHiddenExcept(string except)
        {
            if (except != nameof(this.BorderNodeStatsVisibility))
            {
                this.BorderNodeStatsVisibility = Visibility.Hidden;
            }
            else
            {
                this.BorderNodeStatsVisibility = Visibility.Visible;
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.BorderNodeStatsVisibility)));

            if (except != nameof(this.BorderLogVisibility))
            {
                this.BorderLogVisibility = Visibility.Hidden;
            }
            else
            {
                this.BorderLogVisibility = Visibility.Visible;
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.BorderLogVisibility)));

            if (except != nameof(this.BorderMoreVisibility))
            {
                this.BorderMoreVisibility = Visibility.Hidden;
            }
            else
            {
                this.BorderMoreVisibility = Visibility.Visible;
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.BorderMoreVisibility)));
        }

       
    }
}
