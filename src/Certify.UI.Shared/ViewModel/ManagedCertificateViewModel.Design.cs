using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Certify.Models;
using Certify.Locales;

namespace Certify.UI
{
    /// <summary>
    /// Mock data item view model for use in the XAML designer in Visual Studio 
    /// </summary>
    public class ManagedCertificateViewModelDesign : ViewModel.ManagedCertificateViewModel
    {
        private AppViewModelDesign _appViewModel => (AppViewModelDesign)AppViewModelDesign.Current;

        public ManagedCertificateViewModelDesign()
        {
            // auto-load data if in WPF designer

            SelectedItem = _appViewModel.ManagedCertificates.First();

            SelectedItem.RenewalFailureCount = 3;
            SelectedItem.RenewalFailureMessage = SR.Design_RenewalFailureMessageExample;
            SelectedItem.DateLastRenewalAttempt = DateTimeOffset.UtcNow.AddMinutes(-30);

            ConfigCheckResults = new System.Collections.ObjectModel.ObservableCollection<StatusMessage> {
                    new StatusMessage{
                        IsOK =true,
                        Message = SR.Design_ConfigTestResultExample
                    },
                     new StatusMessage{
                        IsOK =false,
                        Message = SR.Design_FailureMessageExample
                    }
                };

        }

        private void _appViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_appViewModel.SelectedItem))
            {
                RaisePropertyChangedEvent(nameof(SelectedItem));
            }
        }

        protected async override Task<IEnumerable<DomainOption>> GetDomainOptionsFromSite(string siteId)
        {
            return await Task.Run(() =>
                    {
                        return Enumerable.Range(1, 50).Select(i => new DomainOption()
                        {
                            Domain = $"www{i}.domain.example.org",
                            IsPrimaryDomain = i == 1,
                            IsSelected = true
                        });
                    });
        }
    }
}
