using HOA.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace Tests.Helpers
{
    public class FileMock : IFileStore
    {
        public Task DeleteFile(string id)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> RetriveFile(string id)
        {
            throw new NotImplementedException();
        }

        public Task<string> StoreFile(string code, Stream data)
        {
            return Task.FromResult(Guid.NewGuid().ToString());
        }
    }
}
