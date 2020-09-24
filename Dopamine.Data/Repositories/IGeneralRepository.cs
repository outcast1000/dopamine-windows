using Dopamine.Data.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dopamine.Data.Repositories
{
    public enum GeneralRepositoryKeys
    {
        DBVersion,
        PlayListPosition,
        PlayListPositionInTrack

    };
    public interface IGeneralRepository
    {

        /*
        string GetValue(string key);

        void SetValue(string key, string value);
        */

        string GetValue(GeneralRepositoryKeys key, string def = null);

        void SetValue(GeneralRepositoryKeys key, string value);


    }
}
