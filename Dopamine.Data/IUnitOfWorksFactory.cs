using Dopamine.Data.UnitOfWorks;
using SQLite;

namespace Dopamine.Data
{
    public interface IUnitOfWorksFactory
    {
        IDeleteMediaFileUnitOfWork getDeleteMediaFileUnitOfWork();
        IIgnoreMediaFileUnitOfWork getIgnoreMediaFileUnitOfWork();
        IUpdateCollectionUnitOfWork getUpdateCollectionUnitOfWork();
    }
}
