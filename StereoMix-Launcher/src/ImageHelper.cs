using System.IO;
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

    public static async void FetchBackgroundImage(MainWindow window)
    {
        var backgroundImageUri = new Uri(window.RawBackgroundImage);
        var gradientBackgroundImageUri = new Uri(window.RawGradientBackgroundImage);

        try
        {
            var backgroundImage = await DownloadImageAsync(backgroundImageUri);
            window.BackgroundImage.Source = backgroundImage ?? new BitmapImage(new Uri("pack://application:,,,/resources/Background.png"));
        }
        catch
        {
            window.BackgroundImage.Source = new BitmapImage(new Uri("pack://application:,,,/resources/Background.png"));
        }

        try
        {
            var gradientImage = await DownloadImageAsync(gradientBackgroundImageUri);
            window.GradientBackgroundImage.Source = gradientImage ?? new BitmapImage(new Uri("pack://application:,,,/resources/GradientBackground.png"));
        }
        catch
        {
            window.GradientBackgroundImage.Source = new BitmapImage(new Uri("pack://application:,,,/resources/GradientBackground.png"));
        }
    }
}