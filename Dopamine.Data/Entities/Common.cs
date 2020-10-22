using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dopamine.Data.Entities
{
    public enum OriginType
    {
        Unknown = 1,
        File,
        ExternalFile,
        Internet,
        User
    }

    public enum ArtistRoleType
    {
        General = 1,
        Composer,
        Producer,
        Mixing
    }

    public enum HistoryActionType
    {
        Executed = 1,
        Played,
        Skipped,
        Loved,
        Rated
    }


}
