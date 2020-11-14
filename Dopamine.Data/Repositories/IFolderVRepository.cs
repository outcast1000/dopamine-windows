using Dopamine.Data;
using Dopamine.Data.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public interface IFolderVRepository
    {
        List<FolderV> GetFolders(QueryOptions qo = null);

        List<FolderV> GetAllFolders(DataRichnessEnum dataRichness = DataRichnessEnum.Normal);

        List<FolderV> GetShownFolders(DataRichnessEnum dataRichness = DataRichnessEnum.Normal);

        bool SetFolderIndexing(FolderIndexing folderIndexing);
    }
}
