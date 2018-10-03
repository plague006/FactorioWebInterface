using System;

namespace FactorioWebInterface.Models
{
    public class FileData
    {
        public string Name { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime LastModifiedTime { get; set; }
        public long Size { get; set; }
    }
}
