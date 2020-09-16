using Dopamine.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public interface IImageRepository
    {
        IList<AlbumImage> GetAlbumImages();

        AlbumImage GetAlbumImage(long albumId);

        AlbumImage GetAlbumImageForTrackWithPath(string path);

        IList<ArtistImage> GetArtistImages();

        IList<GenreImage> GetGenreImages();

        IList<string> GetAllImagePaths();


        //AlbumImage GetAlbumArtworkForPath(string path);
        /*
        Task DeleteAlbumArtworkAsync(string albumKey);

        Task<long> DeleteUnusedAlbumArtworkAsync();

        Task UpdateAlbumArtworkAsync(string albumKey, string artworkId);
        */
    }
}
