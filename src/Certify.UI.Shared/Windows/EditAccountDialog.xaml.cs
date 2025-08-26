using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Certify.Models;
using Org.BouncyCastle.Crypto.EC;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.IO.Pem;

namespace Certify.UI.Windows
{
    /// <summary>
    /// Edit details for an ACME Account
    /// </summary>
    public partial class EditAccountDialog
    {
        public ContactRegistration Item { get; set; }

        public Certify.UI.ViewModel.AppViewModel MainViewModel => ViewModel.AppViewModel.Current;

        public IEnumerable<CertificateAuthority> CertificateAuthorities => MainViewModel.CertificateAuthorities;

        public EditAccountDialog(ContactRegistration editItem = null)
        {
            InitializeComponent();

            Item = editItem ?? new ContactRegistration();

            DataContext = this;

            Width *= MainViewModel.UIScaleFactor;
            Height *= MainViewModel.UIScaleFactor;

            if (Item.StorageKey != null)
            {

                // edit, disable read only options
                CertificateAuthorityList.IsEnabled = false;
                IsStagingMode.IsEnabled = false;
                AccountKey.IsEnabled = false;
                AccountURI.IsEnabled = false;
                AccountRollover.Visibility = Visibility.Visible;
            }
            else
            {
                CertificateAuthorityList.IsEnabled = true;
                IsStagingMode.IsEnabled = true;
                AccountKey.IsEnabled = true;
                AccountURI.IsEnabled = true;
                AccountRollover.Visibility = Visibility.Collapsed;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Arrow;
            Close();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            //add/update contact

            var ca = MainViewModel.CertificateAuthorities.FirstOrDefault(c => c.Id == Item?.CertificateAuthorityId);

            if (ca == null)
            {
                MessageBox.Show("Autoridade certificadora não selecionada - não é possível continuar. Verifique se o aplicativo foi carregado corretamente e se o serviço em segundo plano do Certify está em execução.");
                return;
            }

            // if ca requires email address, check that first
            if (ca.RequiresEmailAddress)
            {
                var isValidEmail = true;
                if (string.IsNullOrEmpty(Item.EmailAddress))
                {
                    isValidEmail = false;
                }
                else
                {
                    if (!Regex.IsMatch(Item.EmailAddress,
                                @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                                @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
                                RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250)))
                    {
                        isValidEmail = false;
                    }
                }

                if (!isValidEmail)
                {
                    MessageBox.Show(Certify.Locales.SR.New_Contact_EmailError);

                    return;
                }
            }

            // use password fields (account key and eab key) if set
            if (!string.IsNullOrWhiteSpace(EabKey.Password))
            {
                Item.EabKey = EabKey.Password;
            }

            if (!string.IsNullOrWhiteSpace(AccountKey.Password))
            {
                Item.ImportedAccountKey = AccountKey.Password;
            }

            // if EAB is required and not importing an existing account, show CA specific instructions or general prompt for EAB
            if (ca.RequiresExternalAccountBinding && (string.IsNullOrEmpty(Item.ImportedAccountKey) || string.IsNullOrEmpty(Item.ImportedAccountURI)))
            {
                if (string.IsNullOrEmpty(Item.EabKeyId) || string.IsNullOrEmpty(Item.EabKey))
                {
                    MessageBox.Show(string.IsNullOrEmpty(ca.EabInstructions) ? "Um ID da Chave e uma Chave (HMAC) de vinculação de conta externa são necessários e serão fornecidos pela sua Autoridade Certificadora. Você pode inseri-los na guia Avançado." : ca.EabInstructions);
                    return;
                }
            }

            if (Item.IsStaging && string.IsNullOrEmpty(ca.StagingAPIEndpoint))
            {
                MessageBox.Show("Esta autoridade certificadora não possui uma API de homologação (teste), portanto não pode ser usada para solicitações de certificado de Staging.");
                return;
            }

            if (Item.AgreedToTermsAndConditions)
            {
                Mouse.OverrideCursor = Cursors.Wait;

                Models.Config.ActionResult result;
                if (Item.StorageKey == null)
                {

                    result = await MainViewModel.AddContactRegistration(Item);
                }
                else
                {
                    result = await MainViewModel.UpdateContactRegistration(Item);
                }

                Mouse.OverrideCursor = Cursors.Arrow;

                if (result.IsSuccess)
                {
                    await MainViewModel.RefreshAccountsList();

                    Close();
                }
                else
                {
                    MessageBox.Show(result.Message);
                }
            }
            else
            {
                MessageBox.Show(Certify.Locales.SR.Account_Edit_AgreeConditions);
            }
        }

        private void CertificateAuthorityList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }

        private async void AccountKeyChange_Click(object sender, RoutedEventArgs e)
        {
            var result = await MainViewModel.ChangeAccountKey(Item.StorageKey, null);
            if (result.IsSuccess)
            {
                MainViewModel.ShowNotification("Chave da conta alterada");
            }
            else
            {
                MainViewModel.ShowNotification(result.Message, Shared.NotificationType.Error);
            }
        }

        private void AccountKeyGenerate_Click(object sender, RoutedEventArgs e)
        {
            // generate a new EC key
            var generator = GeneratorUtilities.GetKeyPairGenerator("ECDSA");
            var generatorParams = new ECKeyGenerationParameters(CustomNamedCurves.GetOid("P-256"), new SecureRandom());
            generator.Init(generatorParams);
            var keyPair = generator.GenerateKeyPair();
            var keyData = Org.BouncyCastle.Pkcs.PrivateKeyInfoFactory.CreatePrivateKeyInfo(keyPair.Private).ToAsn1Object().GetEncoded();

            // convert to PEM`
            var pem = "";
            using (var sr = new StringWriter())
            {
                var pemObj = new PemObject("PRIVATE KEY", keyData);
                var pemWriter = new PemWriter(sr);
                pemWriter.WriteObject(pemObj);
                pem = sr.ToString();
            }

            Item.ImportedAccountKey = pem;

            AccountKey.Password = pem;
        }
    }
}
