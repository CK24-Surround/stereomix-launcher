using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace StereoMix_Launcher;

public partial class MainWindow : Window
{
    private const string ExtractPath = "./StereoMix";
    private const string GamePath = $"{ExtractPath}/StereoMix.exe";
    private const string DownloadUrl = "http://192.168.0.6:49152/_d/0C7C75CD13B6054";
    
    private DateTime _lastUpdateTime;
    private long _lastBytesReceived;

    public MainWindow()
    {
        InitializeComponent();
        StateChanged += MainWindow_StateChanged;
        CheckGameInstallation();
    }

    private void CheckGameInstallation()
    {
        StartButton.Content = File.Exists(GamePath) ? "게임 실행" : "게임 설치";
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (File.Exists(GamePath))
        {
            LaunchGame();
        }
        else
        {
            StartButton.IsEnabled = false;
            DownloadLatest();
        }
    }

    private void LaunchGame()
    {
        var processStartInfo = new ProcessStartInfo(GamePath);
        try
        {
            Process.Start(processStartInfo);
            Environment.Exit(0);
        }
        catch (Win32Exception error)
        {
            MessageBox.Show(error.Message);
        }
    }

    private async void DownloadLatest()
    {
        DownloadProgressBar.Visibility = Visibility.Visible;
        DownloadProgressText.Visibility = Visibility.Visible;
        await DownloadAndExtractZip(DownloadUrl, ExtractPath);
    }

    private async Task DownloadAndExtractZip(string url, string extractPath)
    {
        var tempZipPath = Path.Combine(Path.GetTempPath(), "StereoMix.zip");

        await Task.Run(async () =>
        {
            using var client = new HttpClient();
            try
            {
                var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? 1L;

                await using (var fs = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var contentStream = await response.Content.ReadAsStreamAsync();
                    var buffer = new byte[8192];
                    int bytesRead;
                    long totalRead = 0;
                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fs.WriteAsync(buffer, 0, bytesRead);
                        totalRead += bytesRead;
                        UpdateProgress(totalRead, totalBytes);
                    }
                }

                if (Directory.Exists(extractPath))
                {
                    Directory.Delete(extractPath, true);
                }

                ZipFile.ExtractToDirectory(tempZipPath, extractPath);

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Visibility = Visibility.Hidden;
                    DownloadProgressText.Visibility = Visibility.Hidden;
                    StartButton.Content = "게임 실행";
                    StartButton.IsEnabled = true;
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"오류 발생: {ex.Message}");
                    DownloadProgressBar.Visibility = Visibility.Hidden;
                    DownloadProgressText.Visibility = Visibility.Hidden;
                    StartButton.IsEnabled = true;
                });
            }
            finally
            {
                if (File.Exists(tempZipPath))
                {
                    File.Delete(tempZipPath);
                }
            }
        });
    }

    private void UpdateProgress(long bytesReceived, long totalBytes)
    {
        Dispatcher.Invoke(() =>
        {
            DateTime now = DateTime.Now;
            TimeSpan timeSinceLastUpdate = now - _lastUpdateTime;
            
            if (timeSinceLastUpdate.TotalSeconds >= 0.2f)
            {
                _lastUpdateTime = now;
                _lastBytesReceived = bytesReceived;
            }

            long bytesSinceLastUpdate = bytesReceived - _lastBytesReceived;
            double downloadSpeed = (bytesSinceLastUpdate / 1024d / 1024d) / timeSinceLastUpdate.TotalSeconds;

            double progressPercentage = (double)bytesReceived / totalBytes * 100;
            DownloadProgressBar.Value = progressPercentage;
            DownloadProgressText.Text = $"{downloadSpeed:F2} MB/s ({progressPercentage:F1}%)";
        }, DispatcherPriority.Background);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        var storyboard = new Storyboard();

        var opacityAnimation = new DoubleAnimation
        {
            To = 0,
            Duration = TimeSpan.FromSeconds(0.3)
        };
        Storyboard.SetTarget(opacityAnimation, this);
        Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(OpacityProperty));

        storyboard.Children.Add(opacityAnimation);

        storyboard.Completed += (_, _) => Close();

        storyboard.Begin();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        var storyboard = new Storyboard();

        var heightAnimation = new DoubleAnimation
        {
            To = 0,
            Duration = TimeSpan.FromSeconds(0.3)
        };
        Storyboard.SetTarget(heightAnimation, this);
        Storyboard.SetTargetProperty(heightAnimation, new PropertyPath(HeightProperty));

        var opacityAnimation = new DoubleAnimation
        {
            To = 0,
            Duration = TimeSpan.FromSeconds(0.3)
        };
        Storyboard.SetTarget(opacityAnimation, this);
        Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(OpacityProperty));

        storyboard.Children.Add(heightAnimation);
        storyboard.Children.Add(opacityAnimation);

        storyboard.Completed += (_, _) => WindowState = WindowState.Minimized;

        storyboard.Begin();
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Normal)
        {
            var storyboard = new Storyboard();

            var heightAnimation = new DoubleAnimation
            {
                To = 700,
                Duration = TimeSpan.FromSeconds(0.3)
            };
            Storyboard.SetTarget(heightAnimation, this);
            Storyboard.SetTargetProperty(heightAnimation, new PropertyPath(HeightProperty));

            var opacityAnimation = new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromSeconds(0.3)
            };
            Storyboard.SetTarget(opacityAnimation, this);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(OpacityProperty));

            storyboard.Children.Add(heightAnimation);
            storyboard.Children.Add(opacityAnimation);

            storyboard.Begin();
        }
    }

    private void SNS_X_Button_Click(object sender, RoutedEventArgs e)
    {
        var url = "https://x.com/StereomixGame";
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Win32Exception error)
        {
            MessageBox.Show(error.Message);
        }
    }
}
