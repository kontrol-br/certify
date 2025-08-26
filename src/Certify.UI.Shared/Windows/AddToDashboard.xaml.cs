using System;
using System.Windows;
using System.Windows.Input;

namespace Certify.UI.Windows
{
    /// <summary>
    /// Interaction logic for AddToDashboard.xaml 
    /// </summary>
    public partial class AddToDashboard
    {
        protected Certify.UI.ViewModel.AppViewModel AppViewModel => UI.ViewModel.AppViewModel.Current;

        public AddToDashboard()
        {
            InitializeComponent();

            DataContext = AppViewModel;

            Width *= AppViewModel.UIScaleFactor;
            Height *= AppViewModel.UIScaleFactor;
        }

        private async void ValidateKey_Click(object sender, RoutedEventArgs e)
        {
            var email = EmailAddress.Text?.Trim().ToLower();
            var pwd = Password.Password.Trim();

            if (string.IsNullOrEmpty(email))
            {
                MessageBox.Show(Certify.Locales.SR.Registration_NeedEmail);
                return;
            }

            if (string.IsNullOrEmpty(pwd))
            {
                // MessageBox.Show(Certify.Locales.SR.Registration_NeedKey);
                return;
            }

            ValidateKey.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;

            var dashboardClient = ViewModel.AppViewModel.Current.DashboardClient;

            if (dashboardClient != null)
            {
                try
                {
                    var instance = new Models.Shared.RegisteredInstance
                    {
                        InstanceId = ViewModel.AppViewModel.Current.Preferences.InstanceId,
                        AppVersion = Management.Util.GetAppVersion().ToString(),
                        OS = Environment.OSVersion.ToString(),
                        MachineName = Environment.MachineName
                    };

                    var resultOK = await dashboardClient.RegisterInstance(instance, email, pwd, createAccount: false);
                    Mouse.OverrideCursor = Cursors.Arrow;

                    if (resultOK)
                    {
                        await ViewModel.AppViewModel.Current.SetInstanceRegisteredOnDashboard();
                        MessageBox.Show("Registro do servidor concluído.");
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("O registro do servidor não pôde ser concluído. Verifique se seu nome de usuário e senha estão corretos e se conexões HTTPS de saída são permitidas nesta máquina.");
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show(Certify.Locales.SR.Registration_KeyValidationError);
                }
            }
            else
            {
                MessageBox.Show(Certify.Locales.SR.Registration_UnableToVerify);
            }

            ValidateKey.IsEnabled = true;
            Mouse.OverrideCursor = Cursors.Arrow;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}
