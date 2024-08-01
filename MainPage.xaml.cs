using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace DownloadTimeUWP
{
    public class Item
    {
        public string ItemType { get; set; }
        public string ItemSpeed { get; set; }
        public string ItemTime { get; set; }
    }

    public sealed partial class MainPage : Page
    {
        public ObservableCollection<Item> ResultList { get; set; } = new ObservableCollection<Item>();
        private static readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();
        private static readonly string[] type = { "Custom speed", "Modem", "2G", "ADSL", "3G", "DSL", "4G", "Cable", "Fiber" };
        private static long[] bandwidth = { 0, 56000, 220000, 24000000, 42000000, 100000000, 300000000, 1000000000, 10000000000 };
        
        public MainPage()
        {
            this.InitializeComponent();
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(500, 460));
            ApplicationView.PreferredLaunchViewSize = new Size(500, 460);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
#if DEBUG
            Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "en";
#endif
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            Application.Current.Suspending += OnSuspending;
        }

        public void Calc()
        {
            ResultList.Clear();

            long customSpeed = 0;
            long bitsize = 1;
            long filesize = 0;
            long parsedCustomSpeed = 0;

            bool isFileSizeValid = !string.IsNullOrEmpty(TextBox_FileSize.Text) && long.TryParse(TextBox_FileSize.Text, out filesize) && filesize > 0;
            bool isCustomSpeedValid = !string.IsNullOrEmpty(TextBox_CustomSpeed.Text) && long.TryParse(TextBox_CustomSpeed.Text, out parsedCustomSpeed) && parsedCustomSpeed > 0;

            if (isCustomSpeedValid)
            {
                customSpeed = RadioButton_Mbit.IsChecked == true ? 1000000 :
                              RadioButton_Gbit.IsChecked == true ? 1000000000 : 1000;
                customSpeed *= parsedCustomSpeed;
            }

            if (RadioButton_MB.IsChecked == true) bitsize = 1024;
            if (RadioButton_GB.IsChecked == true) bitsize = 1048576;

            bandwidth[0] = customSpeed;

            for (int i = 0; i < bandwidth.Length; i++)
            {
                long currentBandwidth = bandwidth[i];
                string speed = "-";
                string formattedTime = "-";

                if (currentBandwidth > 0)
                {
                    long hour = 0, minute = 0, second = 0;

                    if (isFileSizeValid)
                    {
                        long filetime = (bitsize * filesize * 1024 * 8) / currentBandwidth;

                        hour = filetime / 3600;
                        minute = (filetime % 3600) / 60;
                        second = (filetime % 60 == 0) ? 1 : filetime % 60;
                    }

                    if (currentBandwidth / 100 < 10000)
                        speed = $"{currentBandwidth / 1000} KBit/s";
                    else if (currentBandwidth / 100 < 10000000)
                        speed = $"{currentBandwidth / 1000000} Mbit/s";
                    else
                        speed = $"{currentBandwidth / 1000000000} Gbit/s";

                    if (isFileSizeValid)
                    {
                        var timeSpan = new TimeSpan(0, (int)hour, (int)minute, (int)second, 0);
                        formattedTime = timeSpan.ToString(@"d\d\ hh\hmm\mss\s").TrimStart(' ', 'd', 'h', 'm', 's', '0');
                    }
                }

                ResultList.Add(new Item
                {
                    ItemType = type[i],
                    ItemSpeed = speed,
                    ItemTime = isFileSizeValid ? formattedTime : "-"
                });
            }

        }

        private void TextBox_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            args.Cancel = args.NewText.Any(c => !char.IsDigit(c));
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Calc();
        }

        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            Calc();
        }

        private async void AppBarButton_Reset_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog contentDialog = new ContentDialog
            {
                Title = resourceLoader.GetString("ContentDialog_ResetTitle"),
                Content = resourceLoader.GetString("ContentDialog_ResetContent"),
                PrimaryButtonText = "OK",
                CloseButtonText = resourceLoader.GetString("ContentDialog_ResetClose")
            };

            ContentDialogResult cdResult = await contentDialog.ShowAsync();
            if (cdResult == ContentDialogResult.Primary)
            {
                await ApplicationData.Current.ClearAsync();
                Frame.Navigate(this.GetType());
            }
        }

        private void AppBarButton_Feedback_Click(object sender, RoutedEventArgs e)
        {
            _ = Windows.System.Launcher.LaunchUriAsync(new Uri("ms-windows-store://review/?ProductId=9NS52Q7RKV97"));
        }

        private void AppBarButton_GitHub_Click(object sender, RoutedEventArgs e)
        {
            _ = Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/wagneradrian/Download-Time-Calculator-Ultimate"));
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            TextBox_FileSize.Text = localSettings.Values["FileSize"] as string ?? "200";
            TextBox_CustomSpeed.Text = localSettings.Values["CustomSpeed"] as string ?? "50";
            RadioButton_KB.IsChecked = bool.Parse(localSettings.Values["KB"] as string ?? "false");
            RadioButton_MB.IsChecked = bool.Parse(localSettings.Values["MB"] as string ?? "true");
            RadioButton_GB.IsChecked = bool.Parse(localSettings.Values["GB"] as string ?? "false");
            RadioButton_Kbit.IsChecked = bool.Parse(localSettings.Values["Kbit"] as string ?? "false");
            RadioButton_Mbit.IsChecked = bool.Parse(localSettings.Values["Mbit"] as string ?? "true");
            RadioButton_Gbit.IsChecked = bool.Parse(localSettings.Values["Gbit"] as string ?? "false");

            Calc();
        }

        private void OnSuspending(Object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["FileSize"] = TextBox_FileSize.Text;
            localSettings.Values["CustomSpeed"] = TextBox_CustomSpeed.Text;
            localSettings.Values["KB"] = RadioButton_KB.IsChecked.ToString();
            localSettings.Values["MB"] = RadioButton_MB.IsChecked.ToString();
            localSettings.Values["GB"] = RadioButton_GB.IsChecked.ToString();
            localSettings.Values["Kbit"] = RadioButton_Kbit.IsChecked.ToString();
            localSettings.Values["Mbit"] = RadioButton_Mbit.IsChecked.ToString();
            localSettings.Values["Gbit"] = RadioButton_Gbit.IsChecked.ToString();
            deferral.Complete();
        }
    }
}