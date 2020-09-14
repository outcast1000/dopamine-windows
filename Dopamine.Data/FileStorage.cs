using Dopamine.Core.Alex;
using Dopamine.Core.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private string _type;

        public FileStorage(string type)
        {
            _type = type;
            _cacheFolderPath = Path.Combine(SettingsClient.ApplicationFolder(), ApplicationPaths.CacheFolder);
            string typeFolderPath = Path.Combine(SettingsClient.ApplicationFolder(), ApplicationPaths.CacheFolder, type);
            if (!Directory.Exists(typeFolderPath))
            {
                Directory.CreateDirectory(typeFolderPath);
            }
        }

        public string GetRealPath(string location)
        {
            if (location.ToLower().StartsWith("cache://")){
                return Path.Combine(_cacheFolderPath, location.Substring(8) + ".jpg");
            }
            return location;
        }

        public string SaveImageToCache(byte[] bytes)
        {
            Debug.Assert(bytes != null && bytes.Length > 0);
            string sha1 = CalculateSHA1(bytes);
            string location = Path.Combine("cache://", _type, sha1);// "cache://" + type + "//" + sha1;
            string realPath = GetRealPath(location);
            File.WriteAllBytes(realPath, bytes);
            return location;
        }

        private string CalculateSHA1(byte[] bytes)
        {
            Debug.Assert(bytes != null && bytes.Length > 0);
            using (var cryptoProvider = new SHA1CryptoServiceProvider())
            {
                return BitConverter.ToString(cryptoProvider.ComputeHash(bytes));
            }
        }


        public string StorageImagePath { get { return _cacheFolderPath; } }

    }
}
