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
        public IDeleteMediaFileUnitOfWork getDeleteMediaFileUnitOfWork()
        {
            return new SQLiteDeleteMediaFileUnitOfWork(sQLiteConnectionFactory.GetConnection());
        }
        public IIgnoreMediaFileUnitOfWork getIgnoreMediaFileUnitOfWork()
        {
            return new SQLiteIgnoreMediaFileUnitOfWork(sQLiteConnectionFactory.GetConnection());
        }
        public IUpdateCollectionUnitOfWork getUpdateCollectionUnitOfWork()
        {
            return new SQLiteUpdateCollectionUnitOfWork(sQLiteConnectionFactory.GetConnection());
        }
    }
}
