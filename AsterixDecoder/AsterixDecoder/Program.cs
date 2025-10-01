using System;
using AsterixDecoder.IO;

namespace AsterixDecoder
{
    class Program
    {
        static void Main(string[] args)
        {
            var reader = new BinaryFileReader("datos_asterix_adsb.ast");
            var messages = reader.ReadMessages();

            Console.WriteLine($"Total missatges trobats: {messages.Count}");
        }
    }
}
