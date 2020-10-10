using Dopamine.Data.Entities;
using Dopamine.Data.UnitOfWorks;
using SQLite;

namespace Dopamine.Data
{
    public class SQLiteUnitOfWorksFactory: IUnitOfWorksFactory
    {
        ISQLiteConnectionFactory sQLiteConnectionFactory;
        public SQLiteUnitOfWorksFactory(ISQLiteConnectionFactory sQLiteConnectionFactory)
        {
            this.sQLiteConnectionFactory = sQLiteConnectionFactory;
        }

        public IAddFolderUnitOfWork getAddFolderUnitOfWork()
        {
            return new SQLiteAddFolderUnitOfWork(sQLiteConnectionFactory.GetConnection());
        }

        public ICleanUpImagesUnitOfWork getCleanUpAlbumImages()
        {
            return new SQLiteCleanUpImagesUnitOfWork(sQLiteConnectionFactory.GetConnection());
        }

        public IRemoveFolderUnitOfWork getRemoveFolderUnitOfWork()
        {
            return new SQLiteRemoveFolderUnitOfWork(sQLiteConnectionFactory.GetConnection());
        }

        public IUpdateCollectionUnitOfWork getUpdateCollectionUnitOfWork()
        {
            return new SQLiteUpdateCollectionUnitOfWork(sQLiteConnectionFactory.GetConnection(), true);
        }

        public IUpdateFolderUnitOfWork getUpdateFolderUnitOfWork()
        {
            return new SQLiteUpdateFolderUnitOfWork(sQLiteConnectionFactory.GetConnection());
        }

    }
}
