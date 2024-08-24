using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using System.Text.Json;

namespace StereoMix_Launcher;

public static class EventHelper
{
    private static DispatcherTimer _eventTimer = new();
    
    private static int _currentBannerIndex;
    private static readonly List<string> BannerUrls = new();
    private static readonly List<BitmapImage> BannerImages = new();

    public static void BindSnsButtons(MainWindow window)
    {
        window.XButton.Click += (_, _) => window.RunProcess("https://x.com/StereomixGame");
        window.DiscordButton.Click += (_, _) => window.RunProcess("https://discord.gg/bPCr4sy7QR");
        window.GithubButton.Click += (_, _) => window.RunProcess("https://github.com/CK24-Surround");
    }

    public static async void CheckGameEvents(MainWindow window)
    {
#if DEBUG
        var events = await FileHelper.GetValueFromFile("../../../events/events.json");
#else
        var events = await HttpHelper.GetValueFromUrl(window.EventsUrl);
#endif
        if (string.IsNullOrEmpty(events))
        {
            MessageBox.Show("Fail to get events.json");
            return;
        }

        var json = JsonSerializer.Deserialize<JsonDocument>(events);
        if (json == null)
        {
            MessageBox.Show("Fail to deserialize events.json");
            return;
        }

        DisplayEvents(window, json);
    }

    private static void OnEventTimerTick(MainWindow window)
    {
        var oldIndex = _currentBannerIndex;
        _currentBannerIndex = (_currentBannerIndex + 1) % BannerImages.Count;
        if (oldIndex != _currentBannerIndex)
        {
            UpdateBanner(window);
        }
    }

    private static async void DisplayEvents(MainWindow window, JsonDocument json)
    {
        foreach (var (element, index) in json.RootElement.GetProperty("Links").GetProperty("Events")
                     .EnumerateArray().Select((e, i) => (e, i)).Take(3))
        {
            var title = element.GetProperty("Text").ToString();
            var date = element.GetProperty("Date");
            var formattedDate = $"{date.GetProperty("Month").GetInt32()}/{date.GetProperty("Day").GetInt32()}";
            var url = element.GetProperty("Url").ToString();
            
            var buttonLink = new Button
            {
                Style = (Style)window.FindResource("LinkButtonStyle"),
                Content = title
            };
            Grid.SetRow(buttonLink, index);
            buttonLink.Click += (_, _) => window.RunProcess(url);
            window.EventLink.Children.Add(buttonLink);
        
            var buttonDate = new Button
            {
                Style = (Style)window.FindResource("BaseLinkButtonStyle"),
                Content = formattedDate
            };
            Grid.SetRow(buttonDate, index);
            window.EventLinkDate.Children.Add(buttonDate);
        }
        
        foreach (var element in json.RootElement.GetProperty("Images").GetProperty("Events")
                     .EnumerateArray())
        {
            var imageUrl = element.GetProperty("Source").ToString();
            var url = element.GetProperty("Url").ToString();
            var bitmapImage = await ImageHelper.DownloadImageAsync(new Uri($"{window.BaseRawUrl}{imageUrl}?raw=true"));
            BannerImages.Add(bitmapImage ?? new BitmapImage(new Uri("pack://application:,,,/resources/ImageLoadFailed.png")));
            BannerUrls.Add(url);
        }
        
        if (BannerImages.Count != 0)
        {
            _eventTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            _eventTimer.Tick += (_, _) => OnEventTimerTick(window);
            _eventTimer.Start();
        
            window.EventBanner.Click += (_, _) => window.RunProcess(BannerUrls[_currentBannerIndex]);
            
            UpdateBanner(window);
        }
    }

    private static void UpdateBanner(MainWindow window)
    {
        var bannerImage = BannerImages[_currentBannerIndex];
        window.AnimateBannerChange(bannerImage);
    }
}
