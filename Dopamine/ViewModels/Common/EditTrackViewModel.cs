﻿using Digimezzo.Foundation.Core.Logging;
using Digimezzo.Foundation.Core.Utils;
using Dopamine.Core.Base;
using Dopamine.Core.Enums;
using Dopamine.Data;
using Dopamine.Data.Entities;
using Dopamine.Data.Metadata;
using Dopamine.Services.Cache;
using Dopamine.Services.Dialog;
using Dopamine.Services.Indexing;
using Dopamine.Services.InfoDownload;
using Dopamine.Services.Metadata;
using Dopamine.Utils;
using Dopamine.ViewModels.Common.Base;
using Dopamine.Views.Common;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Dopamine.ViewModels.Common
{
    public class EditTrackViewModel : BindableBase
    {
        private IList<string> paths;
        private IMetadataService metadataService;
        private IDialogService dialogService;
        private IInfoDownloadService infoDownloadService;
        private IIndexingService _indexingService;
        private bool updateFileArtwork;
        private bool isBusy;
        private string artworkSize;
        private BitmapImage artworkThumbnail;

        private string multipleValuesText;
        private bool hasMultipleArtwork;

        private bool updateAlbumArtwork;
        private MetadataValue artists;
        private MetadataValue title;
        private MetadataValue album;
        private MetadataValue albumArtists;
        private MetadataValue year;
        private MetadataValue trackNumber;
        private MetadataValue trackCount;
        private MetadataValue discNumber;
        private MetadataValue discCount;
        private MetadataValue genres;
        private MetadataValue grouping;
        private MetadataValue comment;
        private MetadataValue lyrics;
        private MetadataArtworkValue artwork;

        private int slideInFrom;
        private UserControl editTrackContent;

        private EditTrackPage previousSelectedEditTrackPage;
        private EditTrackPage selectedEditTrackPage;

        public DelegateCommand LoadedCommand { get; set; }
        public DelegateCommand ChangeArtworkCommand { get; set; }
        public DelegateCommand RemoveArtworkCommand { get; set; }
        public DelegateCommand ExportArtworkCommand { get; set; }

        /// <summary>
        /// It would be the Dialog Title but in this implementation does nothing
        /// </summary>
        public string DialogTitle
        {
            get
            {
                string dialogTitle = this.paths.Count > 1 ? ResourceUtils.GetString("Language_Edit_Multiple_Songs") : ResourceUtils.GetString("Language_Edit_Song");
                return dialogTitle.ToLower();
            }
        }
        /// <summary>
        /// It is the warning just under the Title bar
        /// </summary>
        public string MultipleTracksWarningText
        {
            get { return ResourceUtils.GetString("Language_Multiple_Songs_Selected").Replace("{trackcount}", this.paths.Count.ToString()); }
        }
        /// <summary>
        /// Controls the visibility of the warning
        /// </summary>
        public bool ShowMultipleTracksWarning
        {
            get { return this.paths.Count > 1; }
        }
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
        public EditTrackPage SelectedEditTrackPage
        {
            get { return selectedEditTrackPage; }
            set
            {
                SetProperty<EditTrackPage>(ref this.selectedEditTrackPage, value);
                this.NagivateToSelectedPage();
            }
        }

        public UserControl EditTrackContent
        {
            get { return this.editTrackContent; }
            set { SetProperty<UserControl>(ref this.editTrackContent, value); }
        }

        public bool HasMultipleArtwork
        {
            get { return this.hasMultipleArtwork; }
            set { SetProperty<bool>(ref this.hasMultipleArtwork, value); }
        }

        public MetadataArtworkValue Artwork
        {
            get { return this.artwork; }
            set { SetProperty<MetadataArtworkValue>(ref this.artwork, value); }
        }

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

        public MetadataValue Artists
        {
            get { return this.artists; }
            set
            {
                SetProperty<MetadataValue>(ref this.artists, value);
            }
        }

        public MetadataValue Title
        {
            get { return this.title; }
            set { SetProperty<MetadataValue>(ref this.title, value); }
        }

        public MetadataValue Album
        {
            get { return this.album; }
            set
            {
                SetProperty<MetadataValue>(ref this.album, value);
            }
        }

        public MetadataValue AlbumArtists
        {
            get { return this.albumArtists; }
            set
            {
                SetProperty<MetadataValue>(ref this.albumArtists, value);
            }
        }

        public MetadataValue Year
        {
            get { return this.year; }
            set { SetProperty<MetadataValue>(ref this.year, value); }
        }

        public MetadataValue TrackNumber
        {
            get { return this.trackNumber; }
            set { SetProperty<MetadataValue>(ref this.trackNumber, value); }
        }

        public MetadataValue TrackCount
        {
            get { return this.trackCount; }
            set { SetProperty<MetadataValue>(ref this.trackCount, value); }
        }

        public MetadataValue DiscNumber
        {
            get { return this.discNumber; }
            set { SetProperty<MetadataValue>(ref this.discNumber, value); }
        }

        public MetadataValue DiscCount
        {
            get { return this.discCount; }
            set { SetProperty<MetadataValue>(ref this.discCount, value); }
        }

        public MetadataValue Genres
        {
            get { return this.genres; }
            set { SetProperty<MetadataValue>(ref this.genres, value); }
        }

        public MetadataValue Grouping
        {
            get { return this.grouping; }
            set { SetProperty<MetadataValue>(ref this.grouping, value); }
        }

        public MetadataValue Comment
        {
            get { return this.comment; }
            set { SetProperty<MetadataValue>(ref this.comment, value); }
        }

        public MetadataValue Lyrics
        {
            get { return this.lyrics; }
            set { SetProperty<MetadataValue>(ref this.lyrics, value); }
        }

        public bool UpdateAlbumArtwork
        {
            get { return this.updateAlbumArtwork; }
            set { SetProperty<bool>(ref this.updateAlbumArtwork, value); }
        }

        public EditTrackViewModel(IList<string> paths, IMetadataService metadataService,
            IDialogService dialogService, IInfoDownloadService infoDownloadService, IIndexingService indexingService) //: base(infoDownloadService)
        {
            this.multipleValuesText = "<" + ResourceUtils.GetString("Language_Multiple_Values") + ">";

            this.metadataService = metadataService;
            this.dialogService = dialogService;
            this.infoDownloadService = infoDownloadService;
            _indexingService = indexingService;

            this.paths = paths;

            this.HasMultipleArtwork = false;
            this.UpdateAlbumArtwork = false;

            this.LoadedCommand = new DelegateCommand(async () =>
            {
                this.NagivateToSelectedPage();
                await this.GetFilesMetadataAsync();
            });

            this.ChangeArtworkCommand = new DelegateCommand(async () =>
            {
                if (!await OpenFileUtils.OpenImageFileAsync(new Action<byte[]>(this.UpdateArtwork)))
                {
                    this.dialogService.ShowNotification(
                        0xe711,
                        16,
                        ResourceUtils.GetString("Language_Error"),
                        ResourceUtils.GetString("Language_Error_Changing_Image"),
                        ResourceUtils.GetString("Language_Ok"),
                        true,
                        ResourceUtils.GetString("Language_Log_File"));
                }
            });

            this.RemoveArtworkCommand = new DelegateCommand(() => this.UpdateArtwork(null));
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
            this.SlideInFrom = this.selectedEditTrackPage <= this.previousSelectedEditTrackPage ? -Constants.SlideDistance : Constants.SlideDistance;
            this.previousSelectedEditTrackPage = this.selectedEditTrackPage;

            switch (this.selectedEditTrackPage)
            {
                case EditTrackPage.Tags:
                    var tagsContent = new EditTrackTagsControl();
                    tagsContent.DataContext = this;
                    this.EditTrackContent = tagsContent;
                    break;
                case EditTrackPage.Lyrics:
                    var lyricsContent = new EditTrackLyricsControl();
                    lyricsContent.DataContext = this;
                    this.EditTrackContent = lyricsContent;
                    break;
                default:
                    break;
            }
        }

        private bool CanDownloadArtwork()
        {
            if (this.albumArtists == null || this.albumArtists.Value == null ||
                this.Artists == null || this.Artists.Value == null ||
                this.Album == null || this.Album.Value == null)
            {
                return false;
            }

            return (!string.IsNullOrEmpty(this.albumArtists.Value) || !string.IsNullOrEmpty(this.Artists.Value) &&
                !string.IsNullOrEmpty(this.Album.Value));
        }

        private async Task GetFilesMetadataAsync()
        {
            var fileMetadatas = new List<FileMetadata>();

            try
            {
                foreach (string path in this.paths)
                {
                    fileMetadatas.Add(await this.metadataService.GetFileMetadataAsync(path));
                }
            }
            catch (Exception ex)
            {
                LogClient.Error("An error occurred while getting the metadata from the files. Exception: {0}", ex.Message);
            }

            if (fileMetadatas.Count == 0) return;

            await Task.Run(() =>
            {
                try
                {
                    // Artists
                    List<string> distinctArtists = fileMetadatas.Select((f) => f.Artists.Value).Distinct().ToList();
                    this.Artists = new MetadataValue(distinctArtists.Count == 1 ? distinctArtists.First() : this.multipleValuesText);

                    // Title
                    List<string> distinctTitles = fileMetadatas.Select((f) => f.Title.Value).Distinct().ToList();
                    this.Title = new MetadataValue(distinctTitles.Count == 1 ? distinctTitles.First() : this.multipleValuesText);

                    // Album
                    List<string> distinctAlbums = fileMetadatas.Select((f) => f.Album.Value).Distinct().ToList();
                    this.Album = new MetadataValue(distinctAlbums.Count == 1 ? distinctAlbums.First() : this.multipleValuesText);

                    // AlbumArtists
                    List<string> distinctAlbumArtists = fileMetadatas.Select((f) => f.AlbumArtists.Value).Distinct().ToList();
                    this.AlbumArtists = new MetadataValue(distinctAlbumArtists.Count == 1 ? distinctAlbumArtists.First() : this.multipleValuesText);

                    // Year
                    List<string> distinctYears = fileMetadatas.Select((f) => f.Year.Value).Distinct().ToList();
                    this.Year = new MetadataValue(distinctYears.Count == 1 ? distinctYears.First().ToString() : this.multipleValuesText);

                    // TrackNumber
                    List<string> distinctTrackNumbers = fileMetadatas.Select((f) => f.TrackNumber.Value).Distinct().ToList();
                    this.TrackNumber = new MetadataValue(distinctTrackNumbers.Count == 1 ? distinctTrackNumbers.First().ToString() : this.multipleValuesText);

                    // TrackCount
                    List<string> distinctTrackCounts = fileMetadatas.Select((f) => f.TrackCount.Value).Distinct().ToList();
                    this.TrackCount = new MetadataValue(distinctTrackCounts.Count == 1 ? distinctTrackCounts.First().ToString() : this.multipleValuesText);

                    // DiscNumber
                    List<string> distinctDiscNumbers = fileMetadatas.Select((f) => f.DiscNumber.Value).Distinct().ToList();
                    this.DiscNumber = new MetadataValue(distinctDiscNumbers.Count == 1 ? distinctDiscNumbers.First().ToString() : this.multipleValuesText);

                    // DiscCount
                    List<string> distinctDiscCounts = fileMetadatas.Select((f) => f.DiscCount.Value).Distinct().ToList();
                    this.DiscCount = new MetadataValue(distinctDiscCounts.Count == 1 ? distinctDiscCounts.First().ToString() : this.multipleValuesText);

                    // Genres
                    List<string> distinctGenres = fileMetadatas.Select((f) => f.Genres.Value).Distinct().ToList();
                    this.Genres = new MetadataValue(distinctGenres.Count == 1 ? distinctGenres.First() : this.multipleValuesText);

                    // Grouping
                    List<string> distinctGroupings = fileMetadatas.Select((f) => f.Grouping.Value).Distinct().ToList();
                    this.Grouping = new MetadataValue(distinctGroupings.Count == 1 ? distinctGroupings.First() : this.multipleValuesText);

                    // Comment
                    List<string> distinctComments = fileMetadatas.Select((f) => f.Comment.Value).Distinct().ToList();
                    this.Comment = new MetadataValue(distinctComments.Count == 1 ? distinctComments.First() : this.multipleValuesText);

                    // Lyrics
                    List<string> distinctLyrics = fileMetadatas.Select((f) => f.Lyrics.Value).Distinct().ToList();
                    this.lyrics = new MetadataValue(distinctLyrics.Count == 1 ? distinctLyrics.First() : this.multipleValuesText);

                    // Artwork 
                    this.GetArtwork(fileMetadatas);
                }
                catch (Exception ex)
                {
                    LogClient.Error("An error occurred while parsing the metadata. Exception: {0}", ex.Message);
                }
            });
        }

        private void GetArtwork(List<FileMetadata> fileMetadatas)
        {
            byte[] foundArtwork = null;

            List<byte[]> artworks = fileMetadatas.Select((f) => f.ArtworkData.Value).ToList();
            List<int> artworksSizes = new List<int>();

            foreach (byte[] eaw in artworks)
            {
                if (eaw != null)
                {
                    artworksSizes.Add(eaw.Length);
                    foundArtwork = eaw;
                }
                else
                {
                    artworksSizes.Add(0);
                }
            }

            int distinctArtworkCount = artworksSizes.Select((l) => l).Distinct().Count();

            if (distinctArtworkCount > 1)
            {
                foundArtwork = null;
                this.HasMultipleArtwork = true;
            }
            else
            {
                this.HasMultipleArtwork = false;
            }

            this.ShowArtwork(foundArtwork);
        }

        private void ShowArtwork(byte[] imageData)
        {
            this.Artwork = new MetadataArtworkValue(imageData); // Create new artwork data, so IsValueChanged is not triggered.
            this.VisualizeArtwork(imageData); // Visualize the artwork
            this.ExportArtworkCommand.RaiseCanExecuteChanged();
        }

        private bool AllEntriesValid()
        {
            return this.Year.IsNumeric &
                   this.TrackNumber.IsNumeric &
                   this.TrackCount.IsNumeric &
                   this.DiscNumber.IsNumeric &
                   this.DiscCount.IsNumeric;
        }

        private void VisualizeArtwork(byte[] imageData)
        {
            this.ArtworkThumbnail = ImageUtils.ByteToBitmapImage(imageData, 0, 0, Convert.ToInt32(Constants.CoverLargeSize));

            // Size of the artwork
            if (imageData != null)
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

        private void UpdateArtwork(byte[] imageData)
        {
            this.Artwork.Value = imageData; // Update existing artwork data, so IsValueChanged is triggered.
            this.VisualizeArtwork(imageData); // Visualize the artwork
            this.ExportArtworkCommand.RaiseCanExecuteChanged();
            // Artwork is updated. Multiple artwork is now impossible.
            this.HasMultipleArtwork = false;
        }

        public async Task<bool> SaveTracksAsync()
        {
            if (!this.AllEntriesValid()) return false;

            var fmdList = new List<FileMetadata>();

            this.IsBusy = true;

            await Task.Run(() =>
            {
                try
                {
                    foreach (string path in this.paths)
                    {
                        FileMetadata fmd = this.metadataService.GetFileMetadata(path);

                        fmd.Artists = this.artists;
                        fmd.Title = this.title;
                        fmd.Album = this.album;
                        fmd.AlbumArtists = this.albumArtists;
                        fmd.Year = this.year;
                        fmd.TrackNumber = this.trackNumber;
                        fmd.TrackCount = this.trackCount;
                        fmd.DiscNumber = this.discNumber;
                        fmd.DiscCount = this.discCount;
                        fmd.Genres = this.genres;
                        fmd.Grouping = this.grouping;
                        fmd.Comment = this.comment;
                        fmd.Lyrics = this.lyrics;
                        fmd.ArtworkData = this.Artwork;

                        fmdList.Add(fmd);
                    }
                }
                catch (Exception ex)
                {
                    LogClient.Error("An error occurred while setting the metadata. Exception: {0}", ex.Message);
                }
            });

            if (fmdList.Count > 0)
            {
                _indexingService.SuspendFileSystemWatcher = true;
                await this.metadataService.UpdateTracksAsync(fmdList, artwork.IsValueChanged);
                await Task.Delay(500);
                _indexingService.SuspendFileSystemWatcher = false;
            }

            this.IsBusy = false;

            return true;
        }
    }
}