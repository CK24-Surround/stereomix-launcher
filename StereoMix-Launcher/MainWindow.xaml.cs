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
    public string InstallDirectory => AppDomain.CurrentDomain.BaseDirectory;
    public string GamePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StereoMix.exe");
    public string GameVersionPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Version.json");
    public string LauncherDownloadUrl => "https://api.github.com/repos/CK24-Surround/stereomix-launcher/releases/latest";
    public string GameDownloadUrl => "https://api.github.com/repos/CK24-Surround/stereomix/releases/latest";
    public string EventsUrl => "https://raw.githubusercontent.com/CK24-Surround/stereomix-launcher/main/StereoMix-Launcher/events/events.json";
    public string BaseRawUrl => "https://github.com/CK24-Surround/stereomix-launcher/blob/main/StereoMix-Launcher";
    public string RawBackgroundImage => "https://github.com/CK24-Surround/stereomix-launcher/blob/main/StereoMix-Launcher/resources/Background.png?raw=true";
    public string RawGradientBackgroundImage => "https://github.com/CK24-Surround/stereomix-launcher/blob/main/StereoMix-Launcher/resources/GradientBackground.png?raw=true";

    public MainWindow()
    {
        InitializeComponent();
        StateChanged += MainWindow_StateChanged;
        LauncherVersion.Text = FileHelper.GetLauncherVersion();

        EventHelper.BindSnsButtons(this);
        EventHelper.CheckGameEvents(this);
        FileHelper.CheckGameInstallation(this);
        ImageHelper.FetchBackgroundImage(this);
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        FileHelper.HandleStartButtonClick(this);
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
        storyboard.Completed += (_, _) => onCompleted?.Invoke();
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

        storyboard.Completed += (_, _) => onCompleted?.Invoke();
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
        Dispatcher.Invoke(() =>
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
        }, DispatcherPriority.Background);
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
    
    public void RunProcess(string path)
    {
        Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
    }
}
