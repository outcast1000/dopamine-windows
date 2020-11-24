﻿using Dopamine.Core.Alex;  //Digimezzo.Foundation.Core.Settings
using Dopamine.Core.Api.Lyrics;
using Dopamine.Data.Entities;
using Dopamine.Data.Metadata;
using Dopamine.Services.Metadata;
using Dopamine.Services.Provider;
using Dopamine.Services.Lyrics;
using Dopamine.ViewModels.Common.Base;
using Prism.Commands;
using Prism.Ioc;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using Dopamine.Services.Entities;
using Dopamine.Data.Repositories;

namespace Dopamine.ViewModels.Common
{
    public class LyricsViewModel : ContextMenuViewModelBase
    {
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private TrackViewModel track;
        private Lyrics lyrics;
        private Lyrics uneditedLyrics;
        private ObservableCollection<LyricsLineViewModel> lyricsLines;
        private double fontSize;
        private bool automaticScrolling;
        private bool centerLyrics;
        private bool isEditing;
        private IMetadataService metadataService;
        private IProviderService providerService;
        private ILyricsService lyricsService;
        private IInfoRepository infoRepository;

        public DelegateCommand DecreaseFontSizeCommand { get; set; }
        public DelegateCommand IncreaseFontSizeCommand { get; set; }
        public DelegateCommand EditCommand { get; set; }
        public DelegateCommand CancelEditCommand { get; set; }
        public DelegateCommand SaveCommand { get; set; }
        public DelegateCommand SaveIfNotEmptyCommand { get; set; }

        public bool HasSource
        {
            get { return this.lyrics != null ? this.lyrics.HasSource : false; }
        }

        public SourceTypeEnum? SourceType => this.lyrics?.SourceType;

        public bool ShowSource
        {
            get { return this.lyrics != null ? this.lyrics.HasText : false; }
        }

        public bool IsNoLyricsTextVisible
        {
            get { return (this.lyrics == null || string.IsNullOrEmpty(this.lyrics.Text)) & !this.IsEditing; }
        }

        public bool IsEditing
        {
            get { return this.isEditing; }
            set
            {
                SetProperty<bool>(ref this.isEditing, value);
                RaisePropertyChanged(nameof(this.IsNoLyricsTextVisible));
            }
        }

        public double FontSize
        {
            get { return this.fontSize; }
            set
            {
                SetProperty<double>(ref this.fontSize, value);
                SettingsClient.Set<int>("Lyrics", "FontSize", (int)value);
                RaisePropertyChanged(nameof(this.FontSizePixels));
            }
        }

        public bool AutomaticScrolling
        {
            get { return this.automaticScrolling; }
            set
            {
                SetProperty<bool>(ref this.automaticScrolling, value);
                SettingsClient.Set<bool>("Lyrics", "AutomaticScrolling", value);
            }
        }

        public bool CenterLyrics
        {
            get { return this.centerLyrics; }
            set
            {
                SetProperty<bool>(ref this.centerLyrics, value);
                SettingsClient.Set<bool>("Lyrics", "CenterLyrics", value);
            }
        }

        public string FontSizePixels
        {
            get { return this.fontSize.ToString() + " px"; }
        }

        public Lyrics Lyrics
        {
            get { return this.lyrics; }
            set
            {
                SetProperty<Lyrics>(ref this.lyrics, value);
                RaisePropertyChanged(nameof(this.IsNoLyricsTextVisible));
                RaisePropertyChanged(nameof(this.ShowSource));
                RaisePropertyChanged(nameof(this.HasSource));
                RaisePropertyChanged(nameof(this.SourceType));
            }
        }

        public ObservableCollection<LyricsLineViewModel> LyricsLines
        {
            get { return this.lyricsLines; }
        }

        private async Task SaveLyricsInAudioFileAsync()
        {
            this.IsEditing = false;
            this.ParseLyrics(this.lyrics);

            if (this.track == null)
            {
                return;
            }

            // Save to the file
            var fmd = await this.metadataService.GetFileMetadataAsync(this.track.Path);
            var lyricsMetaDataValue = new MetadataValue(this.lyrics.Text);
            lyricsMetaDataValue.Value = this.lyrics.Text;
            fmd.Lyrics = lyricsMetaDataValue;
            infoRepository.SetTrackLyrics(new TrackLyrics() { TrackId = track.Id, Lyrics = this.lyrics.Text, OriginType = OriginType.User }, true);
            await this.metadataService.UpdateTracksAsync(new List<FileMetadata> { fmd }, false);
        }

        public LyricsViewModel(IContainerProvider container, TrackViewModel track) : base(container)
        {
            this.metadataService = container.Resolve<IMetadataService>();
            this.providerService = container.Resolve<IProviderService>();
            this.lyricsService = container.Resolve<ILyricsService>();
            this.infoRepository = container.Resolve<IInfoRepository>();

            this.track = track;

            this.lyrics = new Lyrics();
            this.uneditedLyrics = new Lyrics();

            this.FontSize = SettingsClient.Get<double>("Lyrics", "FontSize");
            this.AutomaticScrolling = SettingsClient.Get<bool>("Lyrics", "AutomaticScrolling");
            this.CenterLyrics = SettingsClient.Get<bool>("Lyrics", "CenterLyrics");

            this.DecreaseFontSizeCommand = new DelegateCommand(() => { if (this.FontSize > 11) this.FontSize--; });
            this.IncreaseFontSizeCommand = new DelegateCommand(() => { if (this.FontSize < 50) this.FontSize++; });
            this.EditCommand = new DelegateCommand(() => { this.IsEditing = true; });
            this.CancelEditCommand = new DelegateCommand(() =>
            {
                this.Lyrics = this.uneditedLyrics.Clone();
                this.IsEditing = false;
            });

            this.SaveCommand = new DelegateCommand(async () => await this.SaveLyricsInAudioFileAsync());
            this.SaveIfNotEmptyCommand = new DelegateCommand(async () => await this.SaveLyricsInAudioFileAsync(), () => !string.IsNullOrWhiteSpace(this.lyrics.Text));

            this.SearchOnlineCommand = new DelegateCommand<string>((id) => this.SearchOnline(id));
        }

        public void SetLyrics(Lyrics lyrics)
        {
            this.Lyrics = lyrics.Clone();
            this.uneditedLyrics = lyrics.Clone();
            this.ParseLyrics(lyrics);
        }

        private void ParseLyrics(Lyrics lyrics)
        {
             Application.Current.Dispatcher.Invoke(() =>
            {
                this.lyricsLines = new ObservableCollection<LyricsLineViewModel>(this.lyricsService.ParseLyrics(lyrics));
                RaisePropertyChanged(nameof(this.LyricsLines));
            });
            ;
        }

        protected override void SearchOnline(string id)
        {
            if (this.track == null)
            {
                return;
            }

            this.providerService.SearchOnline(id, new string[] { this.track.ArtistName, this.track.TrackTitle });
        }
    }
}