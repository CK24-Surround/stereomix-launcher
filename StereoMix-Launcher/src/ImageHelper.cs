﻿using System.IO;
using System.Windows.Media.Imaging;

namespace StereoMix_Launcher;

public static class ImageHelper
{
    public static async Task<BitmapImage?> DownloadImageAsync(Uri uri)
    {
        using var client = HttpHelper.CreateHttpClient();
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
}