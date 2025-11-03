using System;
using System.Globalization;
using System.IO;
using System.Linq;
using AsterixDecoder.Models; // make sure this matches your namespace for Cat021Decoder

namespace AsterixDecoder
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("\n=== Running CAT021 Decoder ===\n");

            // Path to your ASTERIX CAT021 file
            string filePath = "/Users/marcrodulfo/Documents/datos_asterix_adsb.ast"; // change if needed

            // Verify file exists
            if (!File.Exists(filePath))
            {
                Console.WriteLine("❗ File not found. Please check the path.");
                return;
            }

            try
            {
                // Read the ASTERIX binary data
                byte[] data = File.ReadAllBytes(filePath);

                // Actual QNH value (you can modify this)
                double qnhActual = 1016.0;

                // Decode CAT021 messages
                var decoder = new Cat021Decoder(data, qnhActual);
                var records = decoder.Decode();

                Console.WriteLine($"✅ Successfully decoded {records.Count} airborne CAT021 records.\n");

                // Print the first 10 decoded records
                foreach (var r in records)
                {
                    Console.WriteLine(
                        $"[{r.Target_Identification,-8}] " +
                        $"Addr={r.Target_Address} | " +
                        $"Lat={r.WGS84_Latitude.ToString("F5", CultureInfo.InvariantCulture)}, " +
                        $"Lon={r.WGS84_Longitude.ToString("F5", CultureInfo.InvariantCulture)} | " +
                        $"FL={r.Flight_Level} | " +
                        $"Alt(ft)={r.Real_Altitude_ft:F1}"
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ ERROR during CAT021 decoding: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("\n=== End of CAT021 decoding ===");
        }
    }
}
