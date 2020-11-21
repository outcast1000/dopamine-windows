﻿using Digimezzo.Foundation.Core.Utils;
using Dopamine.Services.Dialog;
using Dopamine.Services.Provider;
using Prism.Mvvm;
using System.Threading.Tasks;

namespace Dopamine.ViewModels.FullPlayer.Settings
{
    public class SettingsOnlineAddEditSearchProviderViewModel : BindableBase
    {
        private SearchProvider provider;
        private IDialogService dialogService;
        private IProviderService providerService;
        private string name;
        private string url;
        private string separator;
        public SearchProvider Provider
        {
            get { return this.provider; }
            set
            {
                SetProperty<SearchProvider>(ref this.provider, value);
            }
        }

        public string Name
        {
            get { return this.name; }
            set
            {
                SetProperty<string>(ref this.name, value);
            }
        }

        public string Url
        {
            get { return this.url; }
            set
            {
                SetProperty<string>(ref this.url, value);
            }
        }

        public string Separator
        {
            get { return this.separator; }
            set
            {
                SetProperty<string>(ref this.separator, value);
            }
        }

        public SettingsOnlineAddEditSearchProviderViewModel(SearchProvider provider, IDialogService dialogService, IProviderService providerService)
        {
            this.dialogService = dialogService;
            this.providerService = providerService;

            this.provider = provider;
            this.Name = provider.Name;
            this.Url = provider.Url;
            this.Separator = provider.Separator;
        }

#pragma warning disable 1998
        public async Task<bool> AddSearchProviderAsync()
        {
            var provider = new SearchProvider { Id = this.provider.Id, Name = this.Name, Url = this.Url, Separator = this.Separator };
            UpdateSearchProviderResult result = this.providerService.AddSearchProvider(provider);

            switch (result)
            {
                case UpdateSearchProviderResult.MissingFields:
                    this.dialogService.ShowNotification(
                        0xe711,
                        16,
                        ResourceUtils.GetString("Language_Error"),
                        ResourceUtils.GetString("Language_Error_Adding_Online_Search_Provider_Missing_Fields"),
                        ResourceUtils.GetString("Language_Ok"),
                        false,
                        string.Empty);

                    return false;
                case UpdateSearchProviderResult.Failure:
                    this.dialogService.ShowNotification(
                        0xe711,
                        16,
                        ResourceUtils.GetString("Language_Error"),
                        ResourceUtils.GetString("Language_Error_Adding_Online_Search_Provider"),
                        ResourceUtils.GetString("Language_Ok"),
                        true,
                        ResourceUtils.GetString("Language_Log_File"));

                    return false;
                default:
                    return true;
            }
        }

        public async Task<bool> UpdateSearchProviderAsync()
        {
            var provider = new SearchProvider { Id = this.provider.Id, Name = this.Name, Url = this.Url, Separator = this.Separator };
            UpdateSearchProviderResult result = this.providerService.UpdateSearchProvider(provider);

            switch (result)
            {
                case UpdateSearchProviderResult.MissingFields:
                    this.dialogService.ShowNotification(
                        0xe711,
                        16,
                        ResourceUtils.GetString("Language_Error"),
                        ResourceUtils.GetString("Language_Error_Updating_Online_Search_Provider_Missing_Fields"),
                        ResourceUtils.GetString("Language_Ok"),
                        false,
                        string.Empty);

                    return false;
                case UpdateSearchProviderResult.Failure:
                    this.dialogService.ShowNotification(
                        0xe711,
                        16,
                        ResourceUtils.GetString("Language_Error"),
                        ResourceUtils.GetString("Language_Error_Updating_Online_Search_Provider"),
                        ResourceUtils.GetString("Language_Ok"),
                        true,
                        ResourceUtils.GetString("Language_Log_File"));

                    return false;
                default:
                    return true;
            }
        }
#pragma warning restore 1998
    }
}
