using Dopamine.Data.Entities;
using Dopamine.Data.Metadata;
using System;
using System.Threading.Tasks;

namespace Dopamine.Services.Indexing
{

    public delegate void AlbumInfoDownloaded(AlbumV requestedAlbum, bool success);
    public delegate void ArtistInfoDownloaded(ArtistV requestedArtist, bool success);


    public interface IIndexingService
    {
        void OnFoldersChanged();

        bool IsIndexing { get; }

        Task RefreshCollectionAsync(bool bForce, bool bReadTags);
        bool UpdateFile(FileMetadata fileMetadata);

        Task RetrieveInfoAsync(bool rescanFailed, bool rescanAll);

        event EventHandler IndexingStarted;

        event EventHandler IndexingStopped;


        event Action<IndexingStatusEventArgs> IndexingStatusChanged;

        event EventHandler RefreshLists;



        event AlbumInfoDownloaded AlbumInfoDownloaded;
        event ArtistInfoDownloaded ArtistInfoDownloaded;


        Task<bool> RequestArtistInfoAsync(ArtistV artist, bool bIgnorePreviousFailures, bool bForce);
        Task<bool> RequestAlbumInfoAsync(AlbumV album, bool bIgnorePreviousFailures, bool bForce);
            

        void TriggerRefreshLists();
    }
}
