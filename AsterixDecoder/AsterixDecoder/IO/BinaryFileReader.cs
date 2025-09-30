using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace AsterixDecoder.IO
{
    public class BinaryFileReader
    {
        private readonly string _filePath;

        public BinaryFileReader(string filePath)
        {
            _filePath = filePath;
        }
        public List<byte[]> ReadMessages()
        {
            var messages = new List<byte[]>();
            using (var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read))
            using (var br = new BinaryReader(fs))
            {
                while (br.BaseStream.Position<br.BaseStream.Length)
                {
                    try
                    {
                        //  1 byte for category
                        byte category = br.ReadByte();
                        // 2 bytes for length
                        byte[] lengthBytes = br.ReadBytes(2);
                        if (lengthBytes.Length < 2) break;

                        int length = (lengthBytes[0] << 8) | lengthBytes[1];

                        byte[] message = new byte[length];
                        message[0] = category;
                        message[1] = lengthBytes[0];
                        message[2] = lengthBytes[1];

                        int remaining = length - 3;
                        byte[] rest = br.ReadBytes(remaining);

                        if (rest.Length < remaining) break;
                        Array.Copy(rest, 0, message, 3, rest.Length);

                        messages.Add(message);

                        //Console.WriteLine($"Missatge → Categoria: {category}, Longitud: {length}");

                    }

                    catch(EndOfStreamException)
                    {
                        break;
                    }
                }
            }
            return messages;

        }

    }
}
