﻿using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Diagnostics;
using System.ComponentModel;

namespace StereoMix_Launcher;

public partial class MainWindow : Window
{
    private const string ExtractPath = "./StereoMix";
    private const string GamePath = $"{ExtractPath}/StereoMix.exe";
    private const string VersionPath = "./Version.json";
    private const string DownloadUrl = "https://api.github.com/repos/CK24-Surround/stereomix/releases/latest";
    
    private DateTime _lastUpdateTime;
    private long _lastBytesReceived;

    public MainWindow()
    {
        InitializeComponent();
        StateChanged += MainWindow_StateChanged;
        
        BindSnsButtons();
        CheckGameEvents();
        CheckGameInstallation();
    }

    private void BindSnsButtons()
    {
        XButton.Click += (_, _) => OpenUrl("https://x.com/StereomixGame");
        DiscordButton.Click += (_, _) => OpenUrl("https://discord.gg/bPCr4sy7QR");
    }

    private void CheckGameEvents()
    {
        LinkButton1.Click += (_, _) => OpenUrl("https://naver.com");
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
            MessageBox.Show(error.Message);
        }
    }

    private async Task DownloadLatest()
    {
        SetDownloadVisibility(Visibility.Visible);

        var assetUrl = await GetDownloadUrlFromGitHub();
        if (!string.IsNullOrEmpty(assetUrl))
        {
            await DownloadAndExtractZip(assetUrl);
        }
        else
        {
            ShowError("다운로드 URL을 가져오지 못했습니다.");
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
        return await GetValueFromGitHub("tag_name");
    }

    private void SaveJsonToFile(string? jsonContent, string filePath)
    {
        try
        {
            File.WriteAllText(filePath, jsonContent);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"파일 저장 중 오류 발생: {ex.Message}");
        }
    }
    
    private async Task<string?> GetDownloadUrlFromGitHub()
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
            ShowError($"오류 발생: {ex.Message}");
        }
        return string.Empty;
    }

    private async Task<string?> GetValueFromGitHub(string propertyName)
    {
        using var client = CreateHttpClient();
        try
        {
            var response = await client.GetFromJsonAsync<JsonDocument>(DownloadUrl);
            return response?.RootElement.GetProperty(propertyName).ToString();
        }
        catch (Exception ex)
        {
            ShowError($"오류 발생: {ex.Message}");
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
                    ShowError($"오류 발생: {ex.Message}");
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
        MessageBox.Show(message);
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

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Win32Exception error)
        {
            ShowError(error.Message);
        }
    }
}
