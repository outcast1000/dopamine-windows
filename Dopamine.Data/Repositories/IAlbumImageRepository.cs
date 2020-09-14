using Dopamine.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public interface IAlbumImageRepository
    {
        IList<AlbumImage> GetAlbumImages();

        AlbumImage GetAlbumImage(long albumId);

        AlbumImage GetAlbumImageForTrackWithPath(string path);


        //AlbumImage GetAlbumArtworkForPath(string path);
        /*
        Task DeleteAlbumArtworkAsync(string albumKey);

        Task<long> DeleteUnusedAlbumArtworkAsync();

        Task UpdateAlbumArtworkAsync(string albumKey, string artworkId);
        */
    }
}
