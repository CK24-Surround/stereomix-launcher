using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Net.Http;
using System.Text.Json;
using System.Reflection;

namespace StereoMix_Launcher;

public static class FileHelper
{
    public static string GetLauncherVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        return attribute != null ? attribute.InformationalVersion : "Unknown Version";
    }

    public static async void CheckGameInstallation(MainWindow window)
    {
        window.StartButton.IsEnabled = false;

        try
        {
            var localLauncherVersion = GetLauncherVersion();
            var remoteLauncherVersion = await HttpHelper.GetLatestTagFromGitHub(window.LauncherDownloadUrl);
            if (localLauncherVersion != remoteLauncherVersion)
            {
                if (string.IsNullOrEmpty(remoteLauncherVersion))
                {
                    throw new Exception("Fail to get latest launcher version.");
                }
                window.StartButton.Content = "런처 업데이트";
                window.StartButton.IsEnabled = true;
                return;
            }
        
            if (!File.Exists(window.GameVersionPath) || !File.Exists(window.GamePath))
            {
                window.StartButton.Content = "게임 설치";
                window.StartButton.IsEnabled = true;
                return;
            }
        
            var localVersion = await GetLocalGameVersion(window.GameVersionPath);
            var remoteVersion = await HttpHelper.GetLatestTagFromGitHub(window.GameDownloadUrl);
            if (localVersion != remoteVersion)
            {
                if (string.IsNullOrEmpty(remoteVersion))
                {
                    throw new Exception("Fail to get latest game version.");
                }
                window.StartButton.Content = "게임 업데이트";
                window.StartButton.IsEnabled = true;
                return;
            }
        }
        catch (Exception e)
        {
            MessageBox.Show($"Error: {e.Message}");
            throw;
        }
        
        window.StartButton.Content = "게임 실행";
        window.StartButton.IsEnabled = true;
    }

    private static async Task<string?> GetLocalGameVersion(string path)
    {
        try
        {
            var json = await File.ReadAllTextAsync(path);
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("tag_name").ToString();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}");
        }
        return null;
    }

    public static async void HandleStartButtonClick(MainWindow window)
    {
        window.StartButton.IsEnabled = false;

        try
        {
            var localLauncherVersion = GetLauncherVersion();
            var remoteLauncherVersion = await HttpHelper.GetLatestTagFromGitHub(window.LauncherDownloadUrl);
            if (localLauncherVersion != remoteLauncherVersion)
            {
                if (string.IsNullOrEmpty(remoteLauncherVersion))
                {
                    throw new Exception("Fail to get latest launcher version.");
                }
                window.StartButton.Content = "런처 업데이트";
                await DownloadAsset(window, DownloadType.Launcher);
                return;
            }
        
            if (!File.Exists(window.GameVersionPath) || !File.Exists(window.GamePath))
            {
                window.StartButton.Content = "게임 설치";
                await DownloadAsset(window, DownloadType.Game);
                return;
            }
        
            var localVersion = await GetLocalGameVersion(window.GameVersionPath);
            var remoteVersion = await HttpHelper.GetLatestTagFromGitHub(window.GameDownloadUrl);
            if (localVersion != remoteVersion)
            {
                if (string.IsNullOrEmpty(remoteVersion))
                {
                    throw new Exception("Fail to get latest game version.");
                }
                window.StartButton.Content = "게임 업데이트";
                await DownloadAsset(window, DownloadType.Game);
                return;
            }
        }
        catch (Exception e)
        {
            MessageBox.Show($"Error: {e.Message}");
            throw;
        }

        window.StartButton.Content = "게임 실행";
        window.RunProcess(window.GamePath, window.Hide, () =>
        {
            window.Show();
            CheckGameInstallation(window);
        });
    }

    private static async Task DownloadAsset(MainWindow window, DownloadType type)
    {
        window.StartButton.IsEnabled = false;
        window.SetDownloadVisibility(Visibility.Visible);

        var assetUrl = type == DownloadType.Game 
            ? await HttpHelper.GetDownloadUrl(window.GameDownloadUrl, ".zip") 
            : await HttpHelper.GetDownloadUrl(window.LauncherDownloadUrl, ".exe");

        if (string.IsNullOrEmpty(assetUrl))
        {
            MessageBox.Show($"Error: {nameof(type)} download URL not found.");
            window.SetDownloadVisibility(Visibility.Hidden);
            CheckGameInstallation(window);
            return;
        }

        switch (type)
        {
            case DownloadType.Game:
                await DownloadGame(window);
                break;
            case DownloadType.Launcher:
                await DownloadLauncher(window);
                break;
        }

        window.SetDownloadVisibility(Visibility.Hidden);
        CheckGameInstallation(window);
    }

    private static async Task DownloadGame(MainWindow window)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "StereoMix.zip");
        try
        {
            var versionInfo = await HttpHelper.GetValueFromUrl(window.GameDownloadUrl);
            
            await Task.Run(async () =>
            {
                var client = HttpHelper.CreateHttpClient();
                var downloadUrl = await HttpHelper.GetDownloadUrl(window.GameDownloadUrl, ".zip");
                using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                await using var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);
                await HttpHelper.SaveToFile(window, response, fileStream);
            });
            
            ZipFile.ExtractToDirectory(tempFile, window.InstallDirectory, true);
            File.Delete(tempFile);
            
            await File.WriteAllTextAsync(window.GameVersionPath, versionInfo);
        }
        catch (Exception e)
        {
            MessageBox.Show($"Error: {e.Message}");
            CheckGameInstallation(window);
        }
    }

    private static async Task DownloadLauncher(MainWindow window)
    {
        var tempFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StereoMix-Launcher-Installer.exe");
        try
        {
            await Task.Run(async () =>
            {
                var client = HttpHelper.CreateHttpClient();
                var downloadUrl = await HttpHelper.GetDownloadUrl(window.LauncherDownloadUrl, ".exe");
                using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                await using var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);
                await HttpHelper.SaveToFile(window, response, fileStream);
            });

            window.RunProcess(tempFile, Application.Current.Shutdown);
        }
        catch (Exception e)
        {
            MessageBox.Show($"Error: {e.Message}");
            CheckGameInstallation(window);
        }
    }

    public static async Task<string?> GetValueFromFile(string filePath)
    {
        try
        {
            return await File.ReadAllTextAsync(filePath);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}");
            return string.Empty;
        }
    }
}
