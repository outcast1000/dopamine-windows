using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dopamine.Data
{
    public interface IFileStorage
    {
        string GetRealPath(string location);

        // Saves the byte array and returns the "location"
        string SaveImageToCache(byte[] bytes);

        string StorageImagePath { get; }

        //List<string> GetAllICachePaths();
        //FileExists
        //DeleteFile
    }
}
