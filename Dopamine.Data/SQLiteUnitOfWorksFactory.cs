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
            throw new System.NotImplementedException();
        }

        public IDeleteMediaFileUnitOfWork getDeleteMediaFileUnitOfWork()
        {
            return new SQLiteDeleteMediaFileUnitOfWork(sQLiteConnectionFactory.GetConnection());
        }
        public IIgnoreMediaFileUnitOfWork getIgnoreMediaFileUnitOfWork()
        {
            return new SQLiteIgnoreMediaFileUnitOfWork(sQLiteConnectionFactory.GetConnection());
        }

        public IRemoveFolderUnitOfWork getRemoveFolderUnitOfWork()
        {
            throw new System.NotImplementedException();
        }

        public IUpdateCollectionUnitOfWork getUpdateCollectionUnitOfWork()
        {
            return new SQLiteUpdateCollectionUnitOfWork(sQLiteConnectionFactory.GetConnection());
        }

        public IUpdateFolderUnitOfWork getUpdateFolderUnitOfWork()
        {
            throw new System.NotImplementedException();
        }
    }
}
