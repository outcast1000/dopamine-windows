using Dopamine.Core.Alex;
using Dopamine.Core.IO;
using NLog;
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
        private static NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public FileStorage()
        {
            StorageImagePath = Path.Combine(SettingsClient.ApplicationFolder(), ApplicationPaths.CacheFolder);
            string typeFolderPath = Path.Combine(SettingsClient.ApplicationFolder(), ApplicationPaths.CacheFolder);
            if (!Directory.Exists(typeFolderPath))
            {
                Directory.CreateDirectory(typeFolderPath);
            }
        }

        public string GetRealPath(string location)
        {
            if (location.ToLower().StartsWith("cache://")){
                return Path.Combine(StorageImagePath, location.Substring(8) + ".jpg");
            }
            return location;
        }

        public string SaveImageToCache(byte[] bytes, FileStorageItemType fileStorageItemType = FileStorageItemType.Unknown)
        {
            Debug.Assert(bytes != null && bytes.Length > 0);
            string sha1 = CalculateSHA1(bytes);
            string location = Path.Combine("cache://", fileStorageItemType.ToString().ToLower() + "-" + sha1);// "cache://" + type + "//" + sha1;
            string realPath = GetRealPath(location);
            try
            {
                Logger.Debug($"SaveImageToCache: {bytes.Length} - {location}");
                File.WriteAllBytes(realPath, bytes);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"SaveImageToCache: {ex.Message} - {location}");
                if (!File.Exists(realPath))
                    return null; // It seems that even if it could not write the file actually exists
            }
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


        public string StorageImagePath { get; }

    }
}
