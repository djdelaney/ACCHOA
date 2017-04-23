using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HOA.Services;

namespace Tests
{
    class MockFileStorage : IFileStore
    {
        public Task<string> StoreFile(string code, Stream data)
        {
            return Task.FromResult(Guid.NewGuid().ToString());
        }

        public Task<Stream> RetriveFile(string id)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write("filecontent");
            writer.Flush();
            stream.Position = 0;
            System.IO.Stream result = stream;

            return Task.FromResult(result);
        }

        public Task DeleteFile(string id)
        {
            return Task.FromResult<object>(null);
        }
    }
}
