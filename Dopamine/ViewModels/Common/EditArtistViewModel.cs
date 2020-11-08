using Digimezzo.Foundation.Core.Logging;
using Digimezzo.Foundation.Core.Utils;
using Dopamine.Core.Base;
using Dopamine.Core.Enums;
using Dopamine.Data;
using Dopamine.Data.Entities;
using Dopamine.Data.Metadata;
using Dopamine.Data.Repositories;
using Dopamine.Services.Cache;
using Dopamine.Services.Dialog;
using Dopamine.Services.Entities;
using Dopamine.Services.Indexing;
using Dopamine.Services.InfoDownload;
using Dopamine.Services.Metadata;
using Dopamine.Utils;
using Dopamine.ViewModels.Common.Base;
using Dopamine.Views.Common;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Dopamine.ViewModels.Common
{
    public class EditArtistViewModel : BindableBase
    {
        private ArtistViewModel artistViewModel;
        private IMetadataService metadataService;
        private IDialogService dialogService;
        private IIndexingService _indexingService;
        private IFileStorage _fileStorage;
        private IInfoRepository _infoRepository;
        private bool isBusy;
        private string artworkSize;
        private BitmapImage artworkThumbnail;
        private MetadataArtworkValue _artwork;
        private MetadataValue _artistBiography;

        private int slideInFrom;
        private UserControl editContent;

        private EditArtistPage previousSelectedEditPage;
        private EditArtistPage selectedEditPage;
        public DelegateCommand LoadedCommand { get; set; }
        public DelegateCommand ChangeArtworkCommand { get; set; }
        public DelegateCommand RemoveArtworkCommand { get; set; }
        public DelegateCommand DownloadArtworkCommand { get; set; }

        public ArtistViewModel ArtistViewModel
        {
            get { return this.artistViewModel; }
            set { SetProperty<ArtistViewModel>(ref this.artistViewModel, value); }
        }

        /// <summary>
        /// <summary>
        /// Controls the From (Initial Animation)
        /// </summary>
        public int SlideInFrom
        {
            get { return this.slideInFrom; }
            set { SetProperty<int>(ref this.slideInFrom, value); }
        }

        /// <summary>
        /// Controls the Visible Tab Page
        /// </summary>
		public EditArtistPage SelectedEditPage
        {
            get { return selectedEditPage; }
            set
            {
                SetProperty<EditArtistPage>(ref this.selectedEditPage, value);
                this.NagivateToSelectedPage();
            }
        }
        public UserControl EditContent
        {
            get { return this.editContent; }
            set { SetProperty<UserControl>(ref this.editContent, value); }
        }

        public MetadataArtworkValue Artwork
        {
            get { return this._artwork; }
            set { SetProperty<MetadataArtworkValue>(ref this._artwork, value); }
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
            get { return _artwork?.Value != null; }
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



        public MetadataValue ArtistBiography
        {
            get { return this._artistBiography; }
            set { SetProperty<MetadataValue>(ref this._artistBiography, value); }
        }
        public DelegateCommand ExportArtworkCommand { get; set; }

        public EditArtistViewModel(ArtistViewModel artistViewModel, IMetadataService metadataService,
            IDialogService dialogService, IFileStorage fileStorage, IIndexingService indexingService, IInfoRepository infoRepository)// : base(infoDownloadService)
        {
            this.artistViewModel = artistViewModel;
            this.metadataService = metadataService;
            this.dialogService = dialogService;
            _fileStorage = fileStorage;
            _indexingService = indexingService;
            _infoRepository = infoRepository;
            _artwork = new MetadataArtworkValue();
            _artistBiography = new MetadataValue();
            this.LoadedCommand = new DelegateCommand(async () =>
            {
                this.NagivateToSelectedPage();
                await LoadExistingDataAsync();
            });

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


        private void NagivateToSelectedPage()
        {
            this.SlideInFrom = this.selectedEditPage <= this.previousSelectedEditPage ? -Constants.SlideDistance : Constants.SlideDistance;
            this.previousSelectedEditPage = this.selectedEditPage;

            switch (this.selectedEditPage)
            {
                case EditArtistPage.Image:
                    {
                        var content = new EditArtistImageControl();
                        content.DataContext = this;
                        this.EditContent = content;
                    }
                    break;
                case EditArtistPage.Biography:
                    {
                        var content = new EditArtistBiographyControl();
                        content.DataContext = this;
                        this.EditContent = content;
                    }
                    break;
                default:
                    break;
            }
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
                _indexingService.ArtistInfoDownloaded += _indexingService_ArtistInfoDownloaded;
                _indexingService.RequestArtistInfoAsync(artistViewModel.Data, true, true);
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not download artwork. Exception: {0}", ex.Message);
            }
        }

        private void _indexingService_ArtistInfoDownloaded(ArtistV requestedArtist, bool success)
        {
            if (success && requestedArtist.Thumbnail != null)
            {
                byte[] img = ImageUtils.Image2ByteArray(_fileStorage.GetRealPath(requestedArtist.Thumbnail), 0, 0);
                if (img != null)
                    UpdateArtwork(img);
            }
            this.IsBusy = false;
        }


        private bool CanDownloadArtwork()
        {
            if (this.artistViewModel == null)
            {
                return false;
            }
            return string.IsNullOrEmpty(this.artistViewModel.Name);
        }

        private void UpdateArtwork(byte[] imageData)
        {
            this.IsBusy = true;
            this.Artwork.Value = imageData; // Update existing artwork data, so IsValueChanged is triggered.
            this.VisualizeArtwork(imageData); // Visualize the artwork
            this.IsBusy = false;
        }

        private async Task LoadExistingDataAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    ArtistBiography artistBiography = _infoRepository.GetArtistBiography(artistViewModel.Id);
                    if (artistBiography != null)
                        ArtistBiography = new MetadataValue(artistBiography.Biography);
                    if (string.IsNullOrEmpty(artistViewModel.Thumbnail))
                    {
                        VisualizeArtwork(null);
                    }
                    else
                    {
                        byte[] img = ImageUtils.Image2ByteArray(_fileStorage.GetRealPath(artistViewModel.Thumbnail), 0, 0);
                        if (img != null)
                        {
                            this.Artwork = new MetadataArtworkValue(img);
                            this.VisualizeArtwork(img);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogClient.Error("An error occurred while getting the artwork for artist name='{0}'. Exception: {2}", (string)this.artistViewModel.Name, ex.Message);
                }
            });

        }

        public async Task<bool> SaveAsync()
        {
            this.IsBusy = true;

            try
            {
                if (_artwork.IsValueChanged)
                {
                    await this.metadataService.UpdateArtistAsync(this.artistViewModel, _artwork);
                }
                if (_artistBiography.IsValueChanged)
                {
                    _infoRepository.SetArtistBiography(new Data.Entities.ArtistBiography() {ArtistId = artistViewModel.Id, OriginType = OriginType.User, Biography = _artistBiography.Value, DateAdded = DateTime.Now.Ticks });
                }
            }
            catch (Exception ex)
            {
                LogClient.Error("An error occurred while saving the artist with name='{0}'. Exception: {2}", (string)this.artistViewModel.Name, ex.Message);
            }

            this.IsBusy = false;
            return true;
        }

    }
}