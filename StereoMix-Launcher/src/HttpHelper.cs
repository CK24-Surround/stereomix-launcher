﻿using System.IO;
using System.Windows;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Json;

namespace StereoMix_Launcher;

public static class HttpHelper
{
    public static HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
        return client;
    }

    public static async Task<string?> GetValueFromUrl(string url, string propertyName = "")
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
            MessageBox.Show($"Error: {ex.Message}");
        }
        return string.Empty;
    }

    public static async Task<string?> GetLatestTagFromGitHub(string url)
    {
        return await GetValueFromUrl(url, "versionName");
    }

    public static async Task<string?> GetDownloadUrl(string downloadUrl, string url)
    {
        using var client = CreateHttpClient();
        try
        {
            var response = await client.GetFromJsonAsync<JsonDocument>(url);
            var id = response?.RootElement.GetProperty("id").ToString();
            var downloadUrlResponse = await client.GetFromJsonAsync<string>($"{downloadUrl}/{id}");
            return downloadUrlResponse;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}");
        }
        return string.Empty;
    }
    
    public static async Task SaveToFile(MainWindow window, HttpResponseMessage response, FileStream fs)
    {
        var contentStream = await response.Content.ReadAsStreamAsync();
        var buffer = new byte[8192];
        int bytesRead;
        long totalRead = 0;
        while ((bytesRead = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
        {
            await fs.WriteAsync(buffer.AsMemory(0, bytesRead));
            totalRead += bytesRead;
            window.UpdateProgress(totalRead, response.Content.Headers.ContentLength ?? 1L);
        }
    }
}