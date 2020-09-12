using System;
using System.Threading.Tasks;

namespace Dopamine.Services.Indexing
{
    public delegate void AlbumArtworkAddedEventHandler(object sender, AlbumArtworkAddedEventArgs e);
    public delegate void ArtistImagesAddedEventHandler(object sender, ArtistImagesAddedEventArgs e);

    public interface IIndexingService
    {
        void OnFoldersChanged();

        bool IsIndexing { get; }

        Task RefreshCollectionAsync(bool bForce, bool bReadTags);

        Task ReScanAlbumArtworkAsync(bool reloadOnlyMissing);

        event EventHandler IndexingStarted;

        event EventHandler IndexingStopped;

        event Action<IndexingStatusEventArgs> IndexingStatusChanged;

        event EventHandler RefreshLists;

        event AlbumArtworkAddedEventHandler AlbumArtworkAdded;

        event ArtistImagesAddedEventHandler ArtistImagesAdded;
    }
}
