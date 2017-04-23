using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HOA.Services
{
    public class MockFileStore : IFileStore
    {
        public Task DeleteFile(string id)
        {
            return Task.FromResult<object>(null);
        }

        Task<Stream> IFileStore.RetriveFile(string id)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write("filecontent");
            writer.Flush();
            stream.Position = 0;
            System.IO.Stream result = stream;

            return Task.FromResult(result);
        }

        Task<string> IFileStore.StoreFile(string code, Stream data)
        {
            return Task.FromResult(Guid.NewGuid().ToString());
        }
    }
}
