using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using Certify.Locales;
using Certify.UI.Shared;

namespace Certify.UI.Controls
{
    /// <summary>
    /// Interaction logic for AboutControl.xaml 
    /// </summary>
    public partial class AboutControl : UserControl
    {
        protected Certify.UI.ViewModel.AppViewModel MainViewModel => ViewModel.AppViewModel.Current;

        public AboutControl()
        {
            InitializeComponent();
        }

        private void PopulateAppInfo()
        {
            lblAppVersion.Text = ConfigResources.AppName + " " + Management.Util.GetAppVersion();

            creditLibs.Text = "";

            // add details of current languages translator team
            if (!string.IsNullOrEmpty(SR.LanguageAuthor) && !creditLibs.Text.Contains(SR.About_LanguageTranslator))
            {
                creditLibs.Text = SR.About_LanguageTranslator + SR.LanguageAuthor + Environment.NewLine + Environment.NewLine;
            }

            if (System.IO.File.Exists("THIRD_PARTY_LICENSES.txt"))
            {
                creditLibs.Text += System.IO.File.ReadAllText("THIRD_PARTY_LICENSES.txt");
            }
        }

        private async void UpdateCheck_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            await PerformCheckForUpdates(silent: false);

            Mouse.OverrideCursor = Cursors.Arrow;
        }

        private async Task PerformCheckForUpdates(bool silent)
        {
            var updateCheck = await new Management.Util().CheckForUpdates();

            if (updateCheck != null)
            {
                MainViewModel.UpdateCheckResult = updateCheck;
                if (updateCheck.IsNewerVersion)
                {
                    MainViewModel.IsUpdateAvailable = true;

                    var gotoDownload = MessageBox.Show(updateCheck.Message.Body + "\r\nVisit download page now?", ConfigResources.AppName, MessageBoxButton.YesNo);
                    if (gotoDownload == MessageBoxResult.Yes)
                    {
                        Utils.Helpers.LaunchBrowser(ConfigResources.AppWebsiteURL);
                    }
                }
                else
                {
                    if (!silent)
                    {
                        MainViewModel.ShowNotification(ConfigResources.UpdateCheckLatestVersion, NotificationType.Success);
                    }
                }
            }
        }

        private void Feedback_Click(object sender, RoutedEventArgs e)
        {
            var d = new Windows.Feedback("", false) { Owner = Window.GetWindow(this) };
            d.ShowDialog();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e) => PopulateAppInfo();

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            e.Handled = true;
            if (e.Uri != null && e.Uri.IsAbsoluteUri)
            {
                Utils.Helpers.LaunchBrowser(e.Uri.ToString());
            }
        }
    }
}
