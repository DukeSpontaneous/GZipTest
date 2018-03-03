using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

namespace GZipTest.Helpers.Accessors
{
    class FileChunk
    {
        public int Number { get; private set; }
        public byte[] Data { get; private set; }

        public FileChunk(byte[] data, int number)
        {
            Number = number;
            Data = data;
        }
    }
}
