using Dopamine.Core.Alex;
using Dopamine.Core.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Dopamine.Data
{
    public class FileStorage : IFileStorage
    {

        private string _cacheFolderPath;

        public FileStorage()
        {
            _cacheFolderPath = Path.Combine(SettingsClient.ApplicationFolder(), ApplicationPaths.CacheFolder);
        }

        public string GetRealPath(string location)
        {
            if (location.ToLower().StartsWith("cache://")){
                return Path.Combine(_cacheFolderPath, location.Substring(8) + ".jpg");
            }
            return location;
        }

        public string SaveImage(byte[] bytes)
        {
            string sha1 = CalculateSHA1(bytes);
            string location = "cache://" + sha1 + ".jpg";
            string realPath = GetRealPath(location);
            File.WriteAllBytes(realPath, bytes);
            return location;
        }

        private string CalculateSHA1(byte[] bytes)
        {
            using (var cryptoProvider = new SHA1CryptoServiceProvider())
            {
                return BitConverter.ToString(cryptoProvider.ComputeHash(bytes));
            }
        }


        public string StorageImagePath { get { return _cacheFolderPath; } }

    }
}
