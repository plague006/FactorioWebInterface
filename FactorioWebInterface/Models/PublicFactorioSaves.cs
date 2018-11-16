using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FactorioWebInterface.Models
{
    public class PublicFactorioSaves
    {
        private static HashSet<string> ValidPublicDirectories { get; } = new HashSet<string>()
        {
            Constants.PublicStartSavesDirectoryName,
            Constants.PublicFinalSavesDirectoryName,
            Constants.PublicOldSavesDirectoryName,
            Constants.WindowsPublicStartSavesDirectoryName,
            Constants.WindowsPublicFinalSavesDirectoryName,
            Constants.WindowsPublicOldSavesDirectoryName,
        };

        private static DirectoryInfo GetDirectory(string dirName)
        {
            try
            {
                if (ValidPublicDirectories.Contains(dirName))
                {
                    var dirPath = Path.Combine(FactorioServerData.baseDirectoryPath, dirName);
                    dirPath = Path.GetFullPath(dirPath);

                    if (!dirPath.StartsWith(FactorioServerData.baseDirectoryPath))
                        return null;

                    var dir = new DirectoryInfo(dirPath);
                    if (!dir.Exists)
                    {
                        dir.Create();
                    }

                    return dir;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static FileMetaData[] GetFiles(string directoryName)
        {
            var dir = GetDirectory(directoryName);

            if (dir == null)
            {
                return null;
            }

            try
            {
                var files = dir.EnumerateFiles("*.zip")
                    .Select(f => new FileMetaData()
                    {
                        Name = f.Name,
                        Directory = directoryName,
                        CreatedTime = f.CreationTimeUtc,
                        LastModifiedTime = f.LastWriteTimeUtc,
                        Size = f.Length
                    })
                    .ToArray();

                return files;
            }
            catch (Exception)
            {
                return new FileMetaData[0];
            }
        }

        public static FileInfo GetFile(string directoryName, string fileName)
        {
            var dir = GetDirectory(directoryName);
            if (dir == null)
            {
                return null;
            }

            if (Path.GetExtension(fileName) != ".zip")
            {
                return null;
            }

            fileName = Path.GetFileName(fileName);

            try
            {
                var filePath = Path.Combine(dir.FullName, fileName);
                filePath = Path.GetFullPath(filePath);

                if (!filePath.StartsWith(FactorioServerData.basePublicDirectoryPath))
                {
                    return null;
                }

                var fi = new FileInfo(filePath);
                if (fi.Exists)
                {
                    return fi;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
