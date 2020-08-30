using Dopamine.Data.Entities;
using Dopamine.Data.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dopamine.Data.UnitOfWorks
{
    public interface IRemoveFolderUnitOfWork : IDisposable
    {
        RemoveFolderResult RemoveFolder(long folderId);
    }
}
