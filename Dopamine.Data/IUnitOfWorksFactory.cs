using Dopamine.Data.UnitOfWorks;
using SQLite;

namespace Dopamine.Data
{
    public interface IUnitOfWorksFactory
    {
        IUpdateCollectionUnitOfWork getUpdateCollectionUnitOfWork();
        IAddFolderUnitOfWork getAddFolderUnitOfWork();
        IUpdateFolderUnitOfWork getUpdateFolderUnitOfWork();
        IRemoveFolderUnitOfWork getRemoveFolderUnitOfWork();
        ICleanUpImagesUnitOfWork getCleanUpAlbumImages();
    }
}
