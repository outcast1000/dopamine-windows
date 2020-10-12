using Dopamine.Data;
using Dopamine.Data.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public interface IFolderVRepository
    {
        List<FolderV> GetFolders();

        bool SetFolderIndexing(FolderIndexing folderIndexing);
    }
}
