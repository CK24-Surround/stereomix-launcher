using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace StereoMix_Launcher;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        StateChanged += MainWindow_StateChanged;
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
    
    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        var processStartInfo = new ProcessStartInfo("StereoMixClient.exe");
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
}