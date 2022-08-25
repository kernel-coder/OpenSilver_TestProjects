#region Usings

using System;

#endregion

namespace Virtuoso.Core
{
    public class FileUpload
    {
        public string DocumentationFileName { get; set; }
        public DateTime UpdatedDate { get; set; }
        public Guid UpdatedBy { get; set; }
        public int DocumentationType { get; set; }

        public string Path { get; set; } = string.Empty;

        public string FileExtension { get; set; } = string.Empty;

        public byte[] Document { get; set; }
    }
}