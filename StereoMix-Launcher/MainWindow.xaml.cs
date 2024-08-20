using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Media.Imaging;

namespace StereoMix_Launcher;

public partial class MainWindow : Window
{
    private const string ExtractPath = "./StereoMix";
    private const string GamePath = $"{ExtractPath}/StereoMix.exe";
    private const string VersionPath = "./Version.json";
    
    private const string DownloadUrl = "https://api.github.com/repos/CK24-Surround/stereomix/releases/latest";
    private const string EventsUrl = "https://raw.githubusercontent.com/CK24-Surround/stereomix-launcher/main/StereoMix-Launcher/events/events.json";

    private const string LatestBackgroundImage = "https://github.com/CK24-Surround/stereomix-launcher/blob/main/StereoMix-Launcher/resources/Background.png?raw=true";
    private const string LatestGradientBackgroundImage = "https://github.com/CK24-Surround/stereomix-launcher/blob/main/StereoMix-Launcher/resources/GradientBackground.png?raw=true";
    
    private DateTime _lastUpdateTime;
    private long _lastBytesReceived;

    public MainWindow()
    {
        InitializeComponent();
        StateChanged += MainWindow_StateChanged;
        
        BindSnsButtons();
        CheckGameEvents();
        CheckGameInstallation();
        FetchLatestBackgroundImage();
    }

    private async void FetchLatestBackgroundImage()
    {
        var backgroundImageUri = new Uri(LatestBackgroundImage);
        var gradientBackgroundImageUri = new Uri(LatestGradientBackgroundImage);
        
        try
        {
            var backgroundImage = await DownloadImageAsync(backgroundImageUri);
            BackgroundImage.Source = backgroundImage ?? new BitmapImage(new Uri("pack://application:,,,/resources/Background.png"));
        }
        catch
        {
            BackgroundImage.Source = new BitmapImage(new Uri("pack://application:,,,/resources/Background.png"));
        }

        try
        {
            var gradientImage = await DownloadImageAsync(gradientBackgroundImageUri);
            GradientBackgroundImage.Source = gradientImage ?? new BitmapImage(new Uri("pack://application:,,,/resources/GradientBackground.png"));
        }
        catch
        {
            GradientBackgroundImage.Source = new BitmapImage(new Uri("pack://application:,,,/resources/GradientBackground.png"));
        }
    }

    private async Task<BitmapImage?> DownloadImageAsync(Uri uri)
    {
        using var client = new HttpClient();
        try
        {
            var imageData = await client.GetByteArrayAsync(uri);
            using var stream = new MemoryStream(imageData);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = stream;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private void BindSnsButtons()
    {
        XButton.Click += (_, _) => OpenUrl("https://x.com/StereomixGame");
        DiscordButton.Click += (_, _) => OpenUrl("https://discord.gg/bPCr4sy7QR");
    }

    private async void CheckGameEvents()
    {
#if DEBUG
        var events = await LoadEventsFromFile("../../../events/events.json");
#else
        var events = await GetValueFromUrl(EventsUrl);
#endif
        if (string.IsNullOrEmpty(events))
        {
            ShowError("Fail to get events.json");
            return;
        }

        var json = JsonSerializer.Deserialize<JsonDocument>(events);
        if (json == null)
        {
            ShowError("Fail to deserialize events.json");
            return;
        }

        DisplayEvents(json);
    }

    private async Task<string> LoadEventsFromFile(string filePath)
    {
        try
        {
            return await File.ReadAllTextAsync(filePath);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
            return string.Empty;
        }
    }

    private void DisplayEvents(JsonDocument json)
    {
        foreach (var (element, index) in json.RootElement.GetProperty("Links").GetProperty("Events").EnumerateArray().Select((e, i) => (e, i)))
        {
            if (index > 2)
            {
                break;
            }

            var title = element.GetProperty("Text").GetString();
            var date = element.GetProperty("Date");
            var formattedDate = $"{date.GetProperty("Month").GetInt32()}/{date.GetProperty("Day").GetInt32()}";
            var action = element.GetProperty("Action").GetString();
            var url = element.GetProperty("Url").GetString();

            if (action == "OpenUrl")
            {
                CreateEventButton(title, formattedDate, url, index);
            }
        }
    }

    private void CreateEventButton(string? title, string? date, string? url, int index)
    {
        var buttonLink = new Button
        {
            Style = (Style)FindResource("LinkButtonStyle"),
            Content = title
        };
        Grid.SetRow(buttonLink, index);
        buttonLink.Click += (_, _) => OpenUrl(url);
        EventLinkGrid.Children.Add(buttonLink);
        
        var buttonDate = new Button
        {
            Style = (Style)FindResource("BaseLinkButtonStyle"),
            Content = date
        };
        Grid.SetRow(buttonDate, index);
        EventLinkDateGrid.Children.Add(buttonDate);
    }

    private async void CheckGameInstallation()
    {
        if (!File.Exists(GamePath) || !File.Exists(VersionPath))
        {
            StartButton.Content = "게임 설치";
            return;
        }

        var version = await ReadVersionFromFile();
        if (version == null)
        {
            StartButton.Content = "게임 설치";
            return;
        }

        var latestVersion = await GetLatestTagFromGitHub();
        StartButton.Content = latestVersion == version ? "게임 실행" : "게임 업데이트";
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        StartButton.IsEnabled = false;

        if (!File.Exists(GamePath) || !File.Exists(VersionPath))
        {
            StartButton.Content = "게임 설치";
            await DownloadLatest();
            return;
        }

        var version = await ReadVersionFromFile();
        if (version == null || await GetLatestTagFromGitHub() != version)
        {
            StartButton.Content = version == null ? "게임 설치" : "게임 업데이트";
            await DownloadLatest();
        }
        else
        {
            StartButton.Content = "게임 실행";
            LaunchGame();
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
            ShowError(error.Message);
        }
    }

    private async Task DownloadLatest()
    {
        SetDownloadVisibility(Visibility.Visible);

        var assetUrl = await GetDownloadUrl();
        if (!string.IsNullOrEmpty(assetUrl))
        {
            await DownloadAndExtractZip(assetUrl);
        }
        else
        {
            ShowError("Fail to fetch Download Url.");
        }
    }

    private async Task<string?> ReadVersionFromFile()
    {
        try
        {
            var versionJson = await File.ReadAllTextAsync(VersionPath);
            return JsonSerializer.Deserialize<JsonDocument>(versionJson)?.RootElement.GetProperty("tag_name").GetString();
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> GetLatestTagFromGitHub()
    {
        return await GetValueFromUrl(DownloadUrl, "tag_name");
    }

    private void SaveJsonToFile(string? jsonContent, string filePath)
    {
        try
        {
            File.WriteAllText(filePath, jsonContent);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }
    
    private async Task<string?> GetDownloadUrl()
    {
        using var client = CreateHttpClient();
        try
        {
            var response = await client.GetFromJsonAsync<JsonDocument>(DownloadUrl);
            var assets = response?.RootElement.GetProperty("assets");
            if (assets.HasValue)
            {
                foreach (var asset in assets.Value.EnumerateArray().Where(a => a.GetProperty("name").GetString()?.EndsWith(".zip") == true))
                {
                    SaveJsonToFile(response?.RootElement.ToString(), VersionPath);
                    return asset.GetProperty("browser_download_url").GetString();
                }
            }
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        return string.Empty;
    }

    private async Task<string?> GetValueFromUrl(string url, string propertyName = "")
    {
        using var client = CreateHttpClient();
        try
        {
            var response = await client.GetFromJsonAsync<JsonDocument>(url);
            if (string.IsNullOrEmpty(propertyName))
            {
                return response?.RootElement.ToString();
            }
            return response?.RootElement.GetProperty(propertyName).ToString();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        return string.Empty;
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        return client;
    }

    private async Task DownloadAndExtractZip(string url)
    {
        var tempZipPath = Path.Combine(Path.GetTempPath(), "StereoMix.zip");

        await Task.Run(async () =>
        {
            using var client = CreateHttpClient();
            try
            {
                var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                await SaveToFile(response, tempZipPath);
                ExtractZip(tempZipPath);

                Dispatcher.Invoke(() =>
                {
                    SetDownloadVisibility(Visibility.Hidden);
                    StartButton.Content = "게임 실행";
                    StartButton.IsEnabled = true;
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    ShowError(ex.Message);
                    SetDownloadVisibility(Visibility.Hidden);
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

    private async Task SaveToFile(HttpResponseMessage response, string path)
    {
        await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        var contentStream = await response.Content.ReadAsStreamAsync();
        var buffer = new byte[8192];
        int bytesRead;
        long totalRead = 0;
        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await fs.WriteAsync(buffer, 0, bytesRead);
            totalRead += bytesRead;
            UpdateProgress(totalRead, response.Content.Headers.ContentLength ?? 1L);
        }
    }

    private void ExtractZip(string zipPath)
    {
        if (Directory.Exists(ExtractPath))
        {
            Directory.Delete(ExtractPath, true);
        }

        ZipFile.ExtractToDirectory(zipPath, ExtractPath);
    }

    private void UpdateProgress(long bytesReceived, long totalBytes)
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

    private void SetDownloadVisibility(Visibility visibility)
    {
        DownloadProgressBar.Visibility = visibility;
        DownloadProgressText.Visibility = visibility;
    }

    private void ShowError(string message)
    {
        MessageBox.Show($"Error: {message}");
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        AnimateWindowOpacity(0, Close);
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        AnimateWindowHeight(0, () => WindowState = WindowState.Minimized);
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Normal)
        {
            AnimateWindowHeight(700, () => { });
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

    private void OpenUrl(string? url)
    {
        try
        {
            if (url == null)
            {
                return;
            }
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Win32Exception error)
        {
            ShowError(error.Message);
        }
    }
}
