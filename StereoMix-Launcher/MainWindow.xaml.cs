using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Diagnostics;

namespace StereoMix_Launcher;

public partial class MainWindow : Window
{
    public string InstallDirectory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StereoMix");
    public string GamePath => Path.Combine(InstallDirectory, "StereoMix.exe");
    public string GameVersionPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Version.json");

    public string DevInstallDirectory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StereoMixDev");
    public string DevGamePath => Path.Combine(DevInstallDirectory, "StereoMix.exe");
    public string DevGameVersionPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DevVersion.json");

#if DEBUG
    private string BaseAPIUrl => "http://localhost:15600";
#else
    private string BaseAPIUrl => "https://stereomix-502920527569.asia-northeast3.run.app";
#endif

    public string BaseDownloadUrl => $"{BaseAPIUrl}/release/version/download";
    public string LauncherDownloadUrl => $"{BaseAPIUrl}/dev/version/launcher/latest";
    public string GameDownloadUrl => $"{BaseAPIUrl}/release/version/game/latest";
    public string DevBaseDownloadUrl => $"{BaseAPIUrl}/dev/version/download";
    public string DevLauncherDownloadUrl => $"{BaseAPIUrl}/dev/version/launcher/latest";
    public string DevGameDownloadUrl => $"{BaseAPIUrl}/dev/version/game/latest";

    public string EventsUrl => "https://raw.githubusercontent.com/CK24-Surround/stereomix-launcher/main/StereoMix-Launcher/events/events.json";
    public string BaseRawUrl => "https://github.com/CK24-Surround/stereomix-launcher/blob/main/StereoMix-Launcher";

    public MainWindow()
    {
        InitializeComponent();
        StateChanged += MainWindow_StateChanged;
        LauncherVersion.Text = FileHelper.GetLauncherVersion();

        SetDownloadVisibility(Visibility.Hidden);
        DevSetDownloadVisibility(Visibility.Hidden);

        EventHelper.BindSnsButtons(this);
        EventHelper.CheckGameEvents(this);
        FileHelper.CheckGameInstallation(this);
        FileHelper.DevCheckGameInstallation(this);
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        FileHelper.HandleStartButtonClick(this);
    }

    private void DevStartButton_Click(object sender, RoutedEventArgs e)
    {
        FileHelper.DevHandleStartButtonClick(this);
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        AnimateWindowHeight(0, () => WindowState = WindowState.Minimized);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        AnimateWindowOpacity(0, Close);
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Normal)
        {
            AnimateWindowHeight(700);
        }
    }

    private void AnimateWindowOpacity(double to, Action? onCompleted = null)
    {
        var storyboard = new Storyboard();
        var animation = new DoubleAnimation
        {
            To = to,
            Duration = TimeSpan.FromSeconds(0.3)
        };
        Storyboard.SetTarget(animation, this);
        Storyboard.SetTargetProperty(animation, new PropertyPath(OpacityProperty));
        storyboard.Children.Add(animation);
        storyboard.Completed += (_, _) => Application.Current.Dispatcher.Invoke(() => onCompleted?.Invoke());
        storyboard.Begin();
    }

    private void AnimateWindowHeight(double to, Action? onCompleted = null)
    {
        var storyboard = new Storyboard();
        var animation = new DoubleAnimation
        {
            To = to,
            Duration = TimeSpan.FromSeconds(0.3)
        };
        Storyboard.SetTarget(animation, this);
        Storyboard.SetTargetProperty(animation, new PropertyPath(HeightProperty));
        storyboard.Children.Add(animation);

        var opacityAnimation = new DoubleAnimation
        {
            To = to == 0 ? 0 : 1,
            Duration = TimeSpan.FromSeconds(0.3)
        };
        Storyboard.SetTarget(opacityAnimation, this);
        Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(OpacityProperty));
        storyboard.Children.Add(opacityAnimation);

        storyboard.Completed += (_, _) => Application.Current.Dispatcher.Invoke(() => onCompleted?.Invoke());
        storyboard.Begin();
    }

    public void AnimateBannerChange(BitmapImage newImage)
    {
        var fadeOutAnimation = new DoubleAnimation(0, TimeSpan.FromMilliseconds(500));
        fadeOutAnimation.Completed += (_, _) =>
        {
            EventBannerImage.Source = newImage;
            var fadeInAnimation = new DoubleAnimation(1, TimeSpan.FromMilliseconds(500));
            EventBanner.BeginAnimation(OpacityProperty, fadeInAnimation);
        };
        EventBanner.BeginAnimation(OpacityProperty, fadeOutAnimation);
    }

    private DateTime _lastUpdateTime;
    private long _lastBytesReceived;
    public void UpdateProgress(long bytesReceived, long totalBytes)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            var now = DateTime.Now;
            var timeSinceLastUpdate = now - _lastUpdateTime;

            if (timeSinceLastUpdate.TotalSeconds >= 0.2f)
            {
                _lastUpdateTime = now;
                _lastBytesReceived = bytesReceived;
            }

            var bytesSinceLastUpdate = bytesReceived - _lastBytesReceived;
            var downloadSpeed = (bytesSinceLastUpdate / 1024d / 1024d) / timeSinceLastUpdate.TotalSeconds;

            var progressPercentage = (double)bytesReceived / totalBytes * 100;
            DownloadProgressBar.Value = progressPercentage;
            DownloadProgressText.Text = $"{downloadSpeed:F2} MB/s ({progressPercentage:F1}%)";
        }), DispatcherPriority.Background);
    }

    private DateTime _devLastUpdateTime;
    private long _devLastBytesReceived;
    public void DevUpdateProgress(long bytesReceived, long totalBytes)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            var now = DateTime.Now;
            var timeSinceLastUpdate = now - _devLastUpdateTime;

            if (timeSinceLastUpdate.TotalSeconds >= 0.2f)
            {
                _devLastUpdateTime = now;
                _devLastBytesReceived = bytesReceived;
            }

            var bytesSinceLastUpdate = bytesReceived - _devLastBytesReceived;
            var downloadSpeed = (bytesSinceLastUpdate / 1024d / 1024d) / timeSinceLastUpdate.TotalSeconds;

            var progressPercentage = (double)bytesReceived / totalBytes * 100;
            DownloadProgressBarDev.Value = progressPercentage;
            DownloadProgressTextDev.Text = $"{downloadSpeed:F2} MB/s ({progressPercentage:F1}%)";
        }), DispatcherPriority.Background);
    }

    public void SetDownloadVisibility(Visibility visibility)
    {
        DownloadProgressBar.Visibility = visibility;
        DownloadProgressText.Visibility = visibility;

        if (visibility == Visibility.Visible)
        {
            DownloadProgressBar.Value = 0;
            DownloadProgressText.Text = "0 MB/s (0%)";
        }
    }

    public void DevSetDownloadVisibility(Visibility visibility)
    {
        DownloadProgressBarDev.Visibility = visibility;
        DownloadProgressTextDev.Visibility = visibility;

        if (visibility == Visibility.Visible)
        {
            DownloadProgressBarDev.Value = 0;
            DownloadProgressTextDev.Text = "0 MB/s (0%)";
        }
    }

    public void RunProcess(string path, Action? onCompleted = null, Action? onExited = null)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            },
            EnableRaisingEvents = true
        };

        process.Exited += (_, _) =>
        {
            Application.Current.Dispatcher.Invoke(() => onExited?.Invoke());
        };

        process.Start();

        Application.Current.Dispatcher.Invoke(() => onCompleted?.Invoke());
    }
}