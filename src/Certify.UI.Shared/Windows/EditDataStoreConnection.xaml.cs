using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Certify.Models;
using Certify.Models.Config;
using Certify.Shared;
using Certify.UI.ViewModel;
using Certify.Locales;
using Newtonsoft.Json;

namespace Certify.UI.Windows
{
    /// <summary>
    /// Edit details for a certify data store connection
    /// </summary>
    public partial class EditDataStoreConnectionDialog
    {
        public class EditModel : BindableBase
        {
            public DataStoreConnection Item { get; set; }
            public List<ProviderDefinition> DataStoreProviders { get; set; }
        }

        public EditModel Model { get; set; }
        public AppViewModel MainViewModel => ViewModel.AppViewModel.Current;
        public EditDataStoreConnectionDialog(DataStoreConnection editItem = null)
        {
            InitializeComponent();

            Model = new EditModel
            {
                Item = editItem != null ? JsonConvert.DeserializeObject<DataStoreConnection>(JsonConvert.SerializeObject(editItem)) :
                new DataStoreConnection { Id = Guid.NewGuid().ToString(), Title = "", TypeId = "postgres", ConnectionConfig = "" }
            };

            if (editItem != null)
            {
                // provider type can't be changed after initial save
                ProviderTypes.IsEnabled = false;
            }

            DataContext = this;

            Width *= MainViewModel.UIScaleFactor;
            Height *= MainViewModel.UIScaleFactor;

        }

        private async void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Model.DataStoreProviders = await MainViewModel.GetDataStoreProviders();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Arrow;
            Close();
        }

        private bool Validate()
        {
            var validationError = "";

            if (string.IsNullOrEmpty(Model.Item.Title))
            {
                validationError = SR.EditDataStoreConnection_TitleRequired;
            }

            if (string.IsNullOrEmpty(Model.Item.TypeId))
            {
                validationError = SR.EditDataStoreConnection_TypeRequired;
            }

            if (Model.Item.TypeId == "sqlite")
            {
                validationError = SR.EditDataStoreConnection_SqliteNotSupported;
            }

            if (string.IsNullOrEmpty(Model.Item.ConnectionConfig))
            {
                validationError = SR.EditDataStoreConnection_ConfigRequired;
            }

            if (!string.IsNullOrEmpty(validationError))
            {
                MessageBox.Show(validationError, SR.EditDataStoreConnection_ValidationFailed);
                return false;
            }
            else
            {
                return true;
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate())
            {
                return;
            }

            Mouse.OverrideCursor = Cursors.Wait;

            var results = await MainViewModel.SaveDataStoreConnection(Model.Item);

            Mouse.OverrideCursor = Cursors.Arrow;

            if (!results.Any(r => r.HasError))
            {
                Close();
            }
            else
            {
                var err = results.First(r => r.HasError);
                MessageBox.Show(err.Description, err.Title);
            }
        }

        private async void Test_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate())
            {
                return;
            }

            Mouse.OverrideCursor = Cursors.Wait;

            var results = await MainViewModel.TestDataStoreConnection(Model.Item);

            Mouse.OverrideCursor = Cursors.Arrow;

            if (!results.Any(r => r.HasError))
            {
                MessageBox.Show(SR.EditDataStoreConnection_TestSuccess, SR.EditDataStoreConnection_TestTitle);
            }
            else
            {
                var err = results.First(r => r.HasError);
                MessageBox.Show(err.Description, err.Title);
            }
        }
    }
}
