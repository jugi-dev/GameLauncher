using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows;
using System.Windows.Input;

namespace GameLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string downloadURL;
        string versionCheckUrl;
        string urlFile = Directory.GetCurrentDirectory() + "\\download_url.txt";
        string gameFolder = Directory.GetCurrentDirectory() + "\\game";
        string downloadedGameZip = Directory.GetCurrentDirectory() + "\\ExtractionPvE.zip";
        string gameExecutable = Directory.GetCurrentDirectory() + "\\game\\ExtractionPvE.exe";
        string versionFile = Directory.GetCurrentDirectory() + "\\version.txt";

        string clientChecksumString = null;
        string serverChecksumString = null;

        private Process _gameProcess;

        Process gameProcess
        {
            get { return _gameProcess; }
            set
            {
                _gameProcess = value;
                OnGameProcessLaunched();
            }
        }

        enum ButtonState
        {
            CheckUpdates,
            CheckingUpdates,
            DownloadUpdates,
            DownloadingUpdates,
            LaunchGame,
            LaunchingGame
        }

        private ButtonState _buttonState;

        ButtonState buttonState
        {
            get {  return _buttonState; }
            set { 
                _buttonState = value;
                OnButtonStateChanged();
            }
        }

        void OnButtonStateChanged()
        {
            switch (buttonState)
            {
                case ButtonState.CheckUpdates:
                    UpdateLaunchButton.Content = "Check for updates";
                break;
                case ButtonState.CheckingUpdates:
                    CheckUpdates();
                break;
                case ButtonState.DownloadUpdates:
                    UpdateLaunchButton.Content = "Download update";
                break;
                case ButtonState.DownloadingUpdates:
                    DownloadGameBuildFromServer();
                break;
                case ButtonState.LaunchGame:
                    UpdateLaunchButton.Content = "Launch Game";
                break;
                case ButtonState.LaunchingGame:
                    UpdateLaunchButton.Content = "Launching...";
                    try
                    {
                        gameProcess = Process.Start(gameExecutable);
                    }
                    catch (Exception ex)
                    {
                        File.Delete(versionFile); // Delete version file to re-download game files.
                        if (MessageBox.Show("Error occurred while trying to start the game.", "Error") == MessageBoxResult.OK)
                        {
                            buttonState = ButtonState.CheckUpdates;
                        }
                    }
                break;
            }
        }

        void OnGameProcessLaunched()
        {
            Application.Current.Shutdown();
        }

        public MainWindow()
        {
            InitializeComponent();

            if (!File.Exists(urlFile))
            {
                var file = File.CreateText(urlFile);
                file.Close();
                if (MessageBox.Show("Please put your version check link on the first line and download link on the second line inside the download_url.txt file.", "Error") == MessageBoxResult.OK)
                {
                    Application.Current.Shutdown();
                }
            }
            else
            {
                string[] lines = File.ReadAllLines(urlFile);
                versionCheckUrl = lines[0];
                downloadURL = lines[1];
            }

            if (!File.Exists(versionFile))
            {
                var file = File.CreateText(versionFile);
                file.Close();
            }
            else
            {
                clientChecksumString = File.ReadAllText(versionFile);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            switch (buttonState)
            {
                case ButtonState.CheckUpdates:
                    buttonState = ButtonState.CheckingUpdates;
                break;
                case ButtonState.DownloadUpdates:
                    buttonState = ButtonState.DownloadingUpdates;
                break;
                case ButtonState.LaunchGame:
                    buttonState = ButtonState.LaunchingGame;
                break;
            }
        }

        private void QuitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        void CheckUpdates()
        {
            WebClient webClient = new WebClient();
            Uri uri = new Uri(versionCheckUrl);
            webClient.DownloadStringCompleted += WebClient_DownloadStringCompleted;
            webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
            webClient.DownloadStringAsync(uri);
            webClient.Dispose();
        }

        private void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            UpdateLaunchButton.Content = e.ProgressPercentage.ToString() + "%";
        }

        private void WebClient_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                serverChecksumString = e.Result;
                File.WriteAllText(versionFile, serverChecksumString);
                CompareChecksums();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error occurred while trying to download update.", "Error");
            }
        }

        void CompareChecksums()
        {
            if (clientChecksumString == serverChecksumString)
            {
                buttonState = ButtonState.LaunchGame;
            }
            else
            {
                buttonState = ButtonState.DownloadUpdates;
            }
        }

        void CleanInstallationFolder()
        {
            DirectoryInfo di = new DirectoryInfo(gameFolder);
            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
        }

        void DownloadGameBuildFromServer()
        {
            if (Directory.Exists(gameFolder))
                CleanInstallationFolder();
            WebClient webClient = new WebClient();
            Uri uri = new Uri(downloadURL);
            webClient.DownloadProgressChanged += GameUpdate_DownloadProgressChanged;
            webClient.DownloadFileCompleted += GameUpdate_DownloadFileCompleted;
            webClient.DownloadFileAsync(uri, downloadedGameZip);
        }

        private void GameUpdate_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (File.Exists(downloadedGameZip))
            {
                ZipArchive zipArhive = ZipFile.OpenRead(downloadedGameZip);
                zipArhive.ExtractToDirectory(gameFolder);
                zipArhive.Dispose();
                File.Delete(downloadedGameZip);
                buttonState = ButtonState.LaunchGame;
            }
        }

        private void GameUpdate_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            UpdateLaunchButton.Content = e.ProgressPercentage.ToString() + "%";
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
