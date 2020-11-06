using Digimezzo.Foundation.Core.Logging;
using Digimezzo.Foundation.Core.Utils;
using Dopamine.Core.Base;
using Dopamine.Data;
using Dopamine.Data.Entities;
using Dopamine.Data.Metadata;
using Dopamine.Services.Cache;
using Dopamine.Services.Dialog;
using Dopamine.Services.Entities;
using Dopamine.Services.Indexing;
using Dopamine.Services.InfoDownload;
using Dopamine.Services.Metadata;
using Dopamine.Utils;
using Dopamine.ViewModels.Common.Base;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Dopamine.ViewModels.Common
{
    public class EditAlbumViewModel : BindableBase
    {
        private AlbumViewModel albumViewModel;
        private IMetadataService metadataService;
        private IDialogService dialogService;
        private IIndexingService _indexingService;
        private IFileStorage _fileStorage;
        private bool updateFileArtwork;
        private bool isBusy;
        private MetadataArtworkValue artwork;
        private string artworkSize;
        private BitmapImage artworkThumbnail;

        /// <summary>
        /// This is the check box
        /// </summary>
        public bool UpdateFileArtwork
        {
            get { return this.updateFileArtwork; }
            set { SetProperty<bool>(ref this.updateFileArtwork, value); }
        }

        /// <summary>
        /// This is the tooltip of the image
        /// </summary>
        public string ArtworkSize
        {
            get { return this.artworkSize; }
            set { SetProperty<string>(ref this.artworkSize, value); }
        }

        /// <summary>
        /// This control the visibility of the image
        /// </summary>
        public bool HasArtwork
        {
            get { return artwork?.Value != null; }
        }

        /// <summary>
        /// This control the visiblity of the waiting cursor
        /// </summary>
        public bool IsBusy
        {
            get { return this.isBusy; }
            set { SetProperty<bool>(ref this.isBusy, value); }
        }

        /// <summary>
        /// This is the source of the image
        /// </summary>
        public BitmapImage ArtworkThumbnail
        {
            get { return this.artworkThumbnail; }
            set { SetProperty<BitmapImage>(ref this.artworkThumbnail, value); }
        }

        public MetadataArtworkValue Artwork
        {
            get { return this.artwork; }
            set { SetProperty<MetadataArtworkValue>(ref this.artwork, value); }
        }
		
        public DelegateCommand LoadedCommand { get; set; }
        public DelegateCommand ChangeArtworkCommand { get; set; }
        public DelegateCommand RemoveArtworkCommand { get; set; }
        public DelegateCommand DownloadArtworkCommand { get; set; }
        public DelegateCommand ExportArtworkCommand { get; set; }

        public EditAlbumViewModel(AlbumViewModel albumViewModel, IMetadataService metadataService,
            IDialogService dialogService, IFileStorage fileStorage, IIndexingService indexingService)// : base(infoDownloadService)
        {
            this.albumViewModel = albumViewModel;
            this.metadataService = metadataService;
            this.dialogService = dialogService;
            _fileStorage = fileStorage;
            _indexingService = indexingService;
			this.artwork = new MetadataArtworkValue();
            this.LoadedCommand = new DelegateCommand(async () => await LoadExistingArtworkAsync());

            this.ChangeArtworkCommand = new DelegateCommand(async () =>
            {
                if (!await OpenFileUtils.OpenImageFileAsync(new Action<byte[]>(this.UpdateArtwork)))
                {
                    this.dialogService.ShowNotification(0xe711, 16, ResourceUtils.GetString("Language_Error"), ResourceUtils.GetString("Language_Error_Changing_Image"), ResourceUtils.GetString("Language_Ok"), true, ResourceUtils.GetString("Language_Log_File"));
                }
            });

            this.RemoveArtworkCommand = new DelegateCommand(() => this.UpdateArtwork(null));
            this.DownloadArtworkCommand = new DelegateCommand(async () => await this.DownloadArtworkAsync(), () => this.CanDownloadArtwork());
            this.ExportArtworkCommand = new DelegateCommand(async () => await this.ExportArtworkAsync(), () => this.CanExportArtwork());
        }

        private async Task ExportArtworkAsync()
        {
            if (this.HasArtwork)
            {
                await SaveFileUtils.SaveImageFileAsync("cover", this.Artwork.Value);
            }
        }

        private bool CanExportArtwork()
        {
            return this.HasArtwork;
        }

        private void VisualizeArtwork(byte[] imageData)
        {
            //this.Artwork = new MetadataArtworkValue(imageData); // Create new artwork data, so IsValueChanged is not triggered
            if (imageData != null)
                this.ExportArtworkCommand.RaiseCanExecuteChanged();

            this.ArtworkThumbnail = ImageUtils.ByteToBitmapImage(imageData, 0, 0, Convert.ToInt32(Constants.CoverLargeSize));

            // Size of the artwork
            if (imageData != null && this.ArtworkThumbnail != null)
            {
                // Use PixelWidth and PixelHeight instead of Width and Height:
                // Width and Height take DPI into account. We don't want that here.
                this.ArtworkSize = this.ArtworkThumbnail.PixelWidth + "x" + this.ArtworkThumbnail.PixelHeight;
            }
            else
            {
                this.ArtworkSize = string.Empty;
            }

            RaisePropertyChanged(nameof(this.HasArtwork));
        }

        private async Task DownloadArtworkAsync()
        {
            this.IsBusy = true;
            try
            {
                _indexingService.AlbumInfoDownloaded += _indexingService_AlbumInfoDownloaded;
                _indexingService.RequestAlbumInfoAsync(albumViewModel.Data, true, true);
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not download artwork. Exception: {0}", ex.Message);
            }
        }

        private void _indexingService_AlbumInfoDownloaded(AlbumV requestedAlbum, bool success)
        {
            if (success && requestedAlbum.Thumbnail != null)
            {
                byte[] img = ImageUtils.Image2ByteArray(_fileStorage.GetRealPath(requestedAlbum.Thumbnail), 0, 0);
                if (img != null)
                    UpdateArtwork(img);
            }
            this.IsBusy = false;
        }


        private bool CanDownloadArtwork()
        {
            if (this.albumViewModel == null)
            {
                return false;
            }

            return !string.IsNullOrEmpty(this.albumViewModel.AlbumArtists) && !string.IsNullOrEmpty(this.albumViewModel.Name);
        }

        private void UpdateArtwork(byte[] imageData)
        {
            this.Artwork.Value = imageData; // Update existing artwork data, so IsValueChanged is triggered.
            this.VisualizeArtwork(imageData); // Visualize the artwork
        }

        private async Task LoadExistingArtworkAsync()
        {
            if (string.IsNullOrEmpty(albumViewModel.Thumbnail))
                VisualizeArtwork(null);
            else
            {
                await Task.Run(() =>
                {
                    try
                    {
                        byte[] img = ImageUtils.Image2ByteArray(_fileStorage.GetRealPath(albumViewModel.Thumbnail), 0, 0);
                        if (img != null)
                            this.VisualizeArtwork(img);
                    }
                    catch (Exception ex)
                    {
                        LogClient.Error("An error occurred while getting the artwork for album title='{0}' and artist='{1}'. Exception: {2}", (string)this.albumViewModel.Name, (string)this.albumViewModel.AlbumArtists, ex.Message);
                    }
                });

            }

        }

        public async Task<bool> SaveAlbumAsync()
        {
            this.IsBusy = true;

            try
            {
                if (artwork.IsValueChanged)
                {
                    await this.metadataService.UpdateAlbumAsync(this.albumViewModel, artwork, this.UpdateFileArtwork);
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