using Digimezzo.Foundation.Core.Logging;
using Digimezzo.Foundation.Core.Utils;
using Dopamine.Data;
using Dopamine.Data.Entities;
using Dopamine.Services.Cache;
using Dopamine.Services.Dialog;
using Dopamine.Services.Entities;
using Dopamine.Services.Indexing;
using Dopamine.Services.InfoDownload;
using Dopamine.Services.Metadata;
using Dopamine.Utils;
using Dopamine.ViewModels.Common.Base;
using Prism.Commands;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.ViewModels.Common
{
    public class EditAlbumViewModel : EditMetadataBase
    {
        private AlbumViewModel albumViewModel;
        private IMetadataService metadataService;
        private IDialogService dialogService;
        private IInfoDownloadService infoDownloadService;
        private IIndexingService _indexingService;
        private IFileStorage _fileStorage;
        private bool updateFileArtwork;

        public AlbumViewModel AlbumViewModel
        {
            get { return this.albumViewModel; }
            set
            {
                SetProperty(ref this.albumViewModel, value);
                this.DownloadArtworkCommand.RaiseCanExecuteChanged();
            }
        }

        public bool UpdateFileArtwork
        {
            get { return this.updateFileArtwork; }
            set { SetProperty<bool>(ref this.updateFileArtwork, value); }
        }

        public DelegateCommand LoadedCommand { get; set; }

        public DelegateCommand ChangeArtworkCommand { get; set; }

        public DelegateCommand RemoveArtworkCommand { get; set; }

        public EditAlbumViewModel(AlbumViewModel albumViewModel, IMetadataService metadataService,
            IDialogService dialogService, IInfoDownloadService infoDownloadService, IFileStorage fileStorage, IIndexingService indexingService) : base(infoDownloadService)
        {
            this.albumViewModel = albumViewModel;
            this.metadataService = metadataService;
            this.dialogService = dialogService;
            this.infoDownloadService = infoDownloadService;
            _fileStorage = fileStorage;
            _indexingService = indexingService;

            this.LoadedCommand = new DelegateCommand(async () => await this.GetAlbumArtworkAsync());

            this.ChangeArtworkCommand = new DelegateCommand(async () =>
           {
               if (!await OpenFileUtils.OpenImageFileAsync(new Action<byte[]>(this.UpdateArtwork)))
               {
                   this.dialogService.ShowNotification(0xe711, 16, ResourceUtils.GetString("Language_Error"), ResourceUtils.GetString("Language_Error_Changing_Image"), ResourceUtils.GetString("Language_Ok"), true, ResourceUtils.GetString("Language_Log_File"));
               }
           });


            this.RemoveArtworkCommand = new DelegateCommand(() => this.UpdateArtwork(null));
            this.DownloadArtworkCommand = new DelegateCommand(async () => await this.DownloadArtworkAsync(), () => this.CanDownloadArtwork());

            //DisplayArtwork(albumViewModel);
        }

        private void DisplayArtwork(AlbumViewModel albumViewModel)
        {
            if (albumViewModel.Thumbnail != null)
            {
                byte[] img = ImageUtils.Image2ByteArray(_fileStorage.GetRealPath(this.albumViewModel.Thumbnail), 0, 0);
                if (img != null)
                    this.ShowArtwork(img);
            }
        }

        private async Task DownloadArtworkAsync()
        {
            try
            {
                _indexingService.AlbumInfoDownloaded += _indexingService_AlbumInfoDownloaded;
                _indexingService.RequestAlbumInfoAsync(albumViewModel.Data, true, true);
                //== Start Waiting Cursor
                await albumViewModel.RequestImageDownload(true, true);
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not download artwork. Exception: {0}", ex.Message);
            }
        }

        private void _indexingService_AlbumInfoDownloaded(AlbumV requestedAlbum, bool success)
        {
            //== Stop Waiting Cursor
            if (success && requestedAlbum.Thumbnail != null)
            {
                byte[] img = ImageUtils.Image2ByteArray(_fileStorage.GetRealPath(requestedAlbum.Thumbnail), 0, 0);
                if (img != null)
                    this.ShowArtwork(img);
            }
        }


        private bool CanDownloadArtwork()
        {
            if (this.albumViewModel == null)
            {
                return false;
            }

            return !string.IsNullOrEmpty(this.albumViewModel.AlbumArtists) && !string.IsNullOrEmpty(this.albumViewModel.Name);
        }

        protected override void UpdateArtwork(byte[] imageData)
        {
            base.UpdateArtwork(imageData);
        }

        private async Task GetAlbumArtworkAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    if (!string.IsNullOrEmpty(this.albumViewModel.Thumbnail))
                    {
                        DisplayArtwork(albumViewModel);
                    }
                    else
                    {
                        this.ShowArtwork(null);
                    }
                }
                catch (Exception ex)
                {
                    LogClient.Error("An error occurred while getting the artwork for album title='{0}' and artist='{1}'. Exception: {2}", (string)this.albumViewModel.Name, (string)this.albumViewModel.AlbumArtists, ex.Message);
                }
            });
        }

        /*
        private AlbumInfoProviderData RetrieveInfo(AlbumV album)
        {
            if (string.IsNullOrEmpty(album.Name))
                return null;
            Logger.Debug($"RetrieveInfo: Getting {album.Name} - {album.AlbumArtists}");
            return _infoProvider.Get(album.Name, string.IsNullOrEmpty(album.AlbumArtists) ? null : DataUtils.SplitAndTrimColumnMultiValue(album.AlbumArtists).ToArray());
        }
        */

        public async Task<bool> SaveAlbumAsync()
        {
            this.IsBusy = true;

            try
            {
                if (this.Artwork.IsValueChanged)
                {
                    await this.metadataService.UpdateAlbumAsync(this.albumViewModel, this.Artwork, this.UpdateFileArtwork);
                }
            }
            catch (Exception ex)
            {
                LogClient.Error("An error occurred while saving the album with title='{0}' and artist='{1}'. Exception: {2}", (string)this.albumViewModel.Name, (string)this.albumViewModel.AlbumArtists, ex.Message);
            }

            this.IsBusy = false;
            return true;
        }

    }
}