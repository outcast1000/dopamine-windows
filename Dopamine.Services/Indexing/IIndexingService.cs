using System;
using System.Threading.Tasks;

namespace Dopamine.Services.Indexing
{
    public delegate void AlbumArtworkAddedEventHandler(object sender, AlbumArtworkAddedEventArgs e);

    public interface IIndexingService
    {
        void OnFoldersChanged();

        bool IsIndexing { get; }

        //USED ON WHEN THE User Initially adds the first folder in OOBE)
        Task RefreshCollectionAsync(bool bForce, bool bReadTags);

        //WHEN THE USER CLOSE THE ManageCollections Window Dialog

        void ReScanAlbumArtworkAsync(bool reloadOnlyMissing);

        event EventHandler IndexingStarted;

        event EventHandler IndexingStopped;

        event Action<IndexingStatusEventArgs> IndexingStatusChanged;

        event EventHandler RefreshLists;

        event AlbumArtworkAddedEventHandler AlbumArtworkAdded;
    }
}
