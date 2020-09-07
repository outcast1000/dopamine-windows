using Dopamine.Services.Appearance;
using Dopamine.Services.Cache;
using Dopamine.Services.Metadata;
using Dopamine.Services.Playback;

namespace Dopamine.ViewModels.Common
{
    public class BackgroundCoverArtControlViewModel : CoverArtControlViewModel
    {
        private IAppearanceService appearanceService;
        private IMetadataService metadataService;
        
        public BackgroundCoverArtControlViewModel(IPlaybackService playbackService,
            IAppearanceService appearanceService, 
            IMetadataService metadataService) : base(playbackService, metadataService)
        {
            this.playbackService = playbackService;
            this.appearanceService = appearanceService;
            this.metadataService = metadataService;
        }
    }
}
