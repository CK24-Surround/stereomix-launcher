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
    
    public static async void DevCheckGameInstallation(MainWindow window)
    {
        window.StartButtonDev.IsEnabled = false;

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
                window.StartButton.Visibility = Visibility.Hidden;
                window.StartButtonDev.Content = "런처 업데이트";
                window.StartButtonDev.IsEnabled = true;
                return;
            }
        
            if (!File.Exists(window.DevGameVersionPath) || !File.Exists(window.DevGamePath))
            {
                window.StartButtonDev.Content = "개발 빌드 설치";
                window.StartButtonDev.IsEnabled = true;
                return;
            }
        
            var localVersion = await GetLocalGameVersion(window.DevGameVersionPath);
            var remoteVersion = await HttpHelper.GetLatestTagFromGitHub(window.DevGameDownloadUrl);
            if (localVersion != remoteVersion)
            {
                if (string.IsNullOrEmpty(remoteVersion))
                {
                    throw new Exception("Fail to get latest game version.");
                }
                window.StartButtonDev.Content = "개발 빌드 업데이트";
                window.StartButtonDev.IsEnabled = true;
                return;
            }
        }
        catch (Exception e)
        {
            MessageBox.Show($"Error: {e.Message}");
            throw;
        }
        
        window.StartButtonDev.Content = "개발 빌드 실행";
        window.StartButtonDev.IsEnabled = true;
    }

    private static async Task<string?> GetLocalGameVersion(string path)
    {
        try
        {
            var json = await File.ReadAllTextAsync(path);
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("versionName").ToString();
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
    
    public static async void DevHandleStartButtonClick(MainWindow window)
    {
        window.StartButtonDev.IsEnabled = false;

        try
        {
            var localLauncherVersion = GetLauncherVersion();
            var remoteLauncherVersion = await HttpHelper.GetLatestTagFromGitHub(window.DevLauncherDownloadUrl);
            if (localLauncherVersion != remoteLauncherVersion)
            {
                if (string.IsNullOrEmpty(remoteLauncherVersion))
                {
                    throw new Exception("Fail to get latest launcher version.");
                }
                window.StartButton.Visibility = Visibility.Hidden;
                window.StartButtonDev.Content = "런처 업데이트";
                await DevDownloadAsset(window, DownloadType.Launcher);
                return;
            }
        
            if (!File.Exists(window.DevGameVersionPath) || !File.Exists(window.DevGamePath))
            {
                window.StartButtonDev.Content = "개발 빌드 설치";
                await DevDownloadAsset(window, DownloadType.Game);
                return;
            }
        
            var localVersion = await GetLocalGameVersion(window.DevGameVersionPath);
            var remoteVersion = await HttpHelper.GetLatestTagFromGitHub(window.DevGameDownloadUrl);
            if (localVersion != remoteVersion)
            {
                if (string.IsNullOrEmpty(remoteVersion))
                {
                    throw new Exception("Fail to get latest game version.");
                }
                window.StartButtonDev.Content = "개발 빌드 업데이트";
                await DevDownloadAsset(window, DownloadType.Game);
                return;
            }
        }
        catch (Exception e)
        {
            MessageBox.Show($"Error: {e.Message}");
            throw;
        }

        window.StartButtonDev.Content = "개발 빌드 실행";
        window.RunProcess(window.DevGamePath, window.Hide, () =>
        {
            window.Show();
            DevCheckGameInstallation(window);
        });
    }

    private static async Task DownloadAsset(MainWindow window, DownloadType type)
    {
        window.StartButton.IsEnabled = false;
        window.SetDownloadVisibility(Visibility.Visible);

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

    private static async Task DevDownloadAsset(MainWindow window, DownloadType type)
    {
        window.StartButtonDev.IsEnabled = false;
        window.DevSetDownloadVisibility(Visibility.Visible);

        switch (type)
        {
            case DownloadType.Game:
                await DevDownloadGame(window);
                break;
            case DownloadType.Launcher:
                await DownloadLauncher(window);
                break;
        }

        window.DevSetDownloadVisibility(Visibility.Hidden);
        DevCheckGameInstallation(window);
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
                var downloadUrl = await HttpHelper.GetDownloadUrl(window.BaseDownloadUrl, window.GameDownloadUrl);
                using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                await using var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);
                await HttpHelper.SaveToFile(window, response, fileStream);
            });
            
            Directory.Delete(window.InstallDirectory, true);
            
            ZipFile.ExtractToDirectory(tempFile, window.InstallDirectory, true);
            File.Delete(tempFile);
            
            await File.WriteAllTextAsync(window.GameVersionPath, versionInfo);
        }
        catch (Exception e)
        {
            MessageBox.Show($"Error: {e.Message}");
            window.SetDownloadVisibility(Visibility.Hidden);
            CheckGameInstallation(window);
        }
    }
    
    private static async Task DevDownloadGame(MainWindow window)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "StereoMixDev.zip");
        try
        {
            var versionInfo = await HttpHelper.GetValueFromUrl(window.DevGameDownloadUrl);
            
            await Task.Run(async () =>
            {
                var client = HttpHelper.CreateHttpClient();
                var downloadUrl = await HttpHelper.GetDownloadUrl(window.DevBaseDownloadUrl, window.DevGameDownloadUrl);
                using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                await using var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);
                await HttpHelper.DevSaveToFile(window, response, fileStream);
            });
            
            Directory.Delete(window.DevInstallDirectory, true);
            
            ZipFile.ExtractToDirectory(tempFile, window.DevInstallDirectory, true);
            File.Delete(tempFile);
            
            await File.WriteAllTextAsync(window.DevGameVersionPath, versionInfo);
        }
        catch (Exception e)
        {
            MessageBox.Show($"Error: {e.Message}");
            window.DevSetDownloadVisibility(Visibility.Hidden);
            DevCheckGameInstallation(window);
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
                var downloadUrl = await HttpHelper.GetDownloadUrl(window.BaseDownloadUrl, window.LauncherDownloadUrl);
                using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                await using var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);
                await HttpHelper.DevSaveToFile(window, response, fileStream);
            });

            window.RunProcess(tempFile, Application.Current.Shutdown);
        }
        catch (Exception e)
        {
            MessageBox.Show($"Error: {e.Message}");
            window.SetDownloadVisibility(Visibility.Hidden);
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
