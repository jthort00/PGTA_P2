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

            // Input ASTERIX file
            string filePath = "/Users/marcrodulfo/Documents/datos_asterix_adsb.ast";

            // CSV output path
            string csvOutputPath = "/Users/marcrodulfo/Documents/decoded_cat021.csv";

            if (!File.Exists(filePath))
            {
                Console.WriteLine("❗ File not found. Please check the path.");
                return;
            }

            try
            {
                byte[] data = File.ReadAllBytes(filePath);
                double qnhActual = 1013.25; // adjust as needed

                var decoder = new Cat021Decoder(data, qnhActual);
                var records = decoder.Decode();

                Console.WriteLine($"✅ Successfully decoded {records.Count} airborne CAT021 records.\n");

                // Print table header
                // Print table header
                Console.WriteLine("{0,-3} {1,-3} {2,-8} {3,-9} {4,-9} {5,-6} {6,-6} {7,-7} {8,-8} {9,-10} {10,-2}",
                    "CAT", "SAC", "SIC", "Time", "LAT", "LON", "Mode3/A", "FL", "TA", "TI", "BP");

                // Print each record
                foreach (var r in records)
                {
                    string timeStr = r.Time_Reception_Position.HasValue
                        ? r.Time_Reception_Position.Value.ToString(@"hh\:mm\:ss")
                        : "--:--:--";

                    string ta = r.Target_Address ?? "------";
                    string ti = r.Target_Identification ?? "--------";
                    string bp = r.TargetReportDescriptor ?? "";

                    string sac = "--";
                    string sic = "--";
                    if (!string.IsNullOrEmpty(r.DataSourceIdentifier) && r.DataSourceIdentifier.Contains("SAC:"))
                    {
                        var parts = r.DataSourceIdentifier.Split(' ');
                        if (parts.Length == 2)
                        {
                            sac = parts[0].Replace("SAC:", "");
                            sic = parts[1].Replace("SIC:", "");
                        }
                    }

                    Console.WriteLine("{0,-6} {1,-6} {2,-3} {3,-8} {4,-9:F5} {5,-9:F5} {6,-6} {7,-6} {8,-7} {9,-10} {10,-2}",
                        "CAT021", sac, sic, timeStr, r.WGS84_Latitude, r.WGS84_Longitude,
                        r.Mode3A_Code, r.Flight_Level, ta, ti, bp);
                }


                // Write CSV
                Cat021Decoder.WriteCsv(csvOutputPath, records);
                Console.WriteLine($"\n✅ CSV file written to: {csvOutputPath}");
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
