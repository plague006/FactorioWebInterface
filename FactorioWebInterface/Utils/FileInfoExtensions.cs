using System.IO;
using System.Threading.Tasks;

namespace FactorioWebInterface.Utils
{
    public static class FileInfoExtensions
    {
        public static async Task CopyToAsync(this FileInfo source, FileInfo target)
        {
            using (var sourceStream = source.OpenRead())
            using (var targetStream = target.Create())
            {
                await sourceStream.CopyToAsync(targetStream);
            }
        }

        public static Task CopyToAsync(this FileInfo source, string targetPath)
        {
            var target = new FileInfo(targetPath);
            return source.CopyToAsync(target);
        }
    }
}
