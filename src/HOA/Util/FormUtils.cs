using Microsoft.AspNet.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HOA.Util
{
    public static class FormUtils
    {
        private static string[] allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".gif", ".png" };

        public static bool IsValidFileType(string fileName)
        {
            string extension = System.IO.Path.GetExtension(fileName);
            return (allowedExtensions.Any(ext => ext.Equals(extension, StringComparison.Ordinal)));
        }

        public static string GetUploadedFilename(IFormFile file)
        {
            var chunks = file.ContentDisposition.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            var nameChunk = chunks.FirstOrDefault(c => c.Contains("filename"));
            var fileName = nameChunk.Split('=')[1].Trim(new char[] { '"' });
            return  System.IO.Path.GetFileName(fileName);
        }
    }
}
