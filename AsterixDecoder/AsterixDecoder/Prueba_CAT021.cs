using System;
using System.Globalization;
using System.IO;
using System.Linq;
using AsterixDecoder.Models; // ensure the namespace matches your Cat021Decoder

namespace AsterixDecoder
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("\n=== Running CAT021 Decoder ===\n");

            // Input ASTERIX file
            // *** NOTE: Your path was cut off, make sure this is correct ***
            string filePath = "/Users/marcrodulfo/Documents/datos_asterix_adsb.ast";

            // CSV output path
            string csvOutputPath = "/Users/marcrodulfo/Documents/decoded_cat021.csv";

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"❗ File not found. Please check the path: {filePath}");
                return;
            }

            try
            {
                byte[] data = File.ReadAllBytes(filePath);
                double qnhActual = 1013.25; // Adjust if local QNH differs

                var decoder = new Cat021Decoder(data, qnhActual);
                var records = decoder.Decode();

                Console.WriteLine($"✅ Successfully decoded {records.Count} airborne CAT021 records.\n");

                // Print table header
                Console.WriteLine(
                    "{0,-7} {1,-4} {2,-4} {3,-10} {4,-10} {5,-10} {6,-8} {7,-8} {8,-10} {9,-10} {10,-8}",
                    "CAT", "SAC", "SIC", "Time", "LAT", "LON", "Mode3/A", "FL", "TA", "TI", "BPS(hPa)"
                );

                Console.WriteLine(new string('-', 100));

                // Print each record
                foreach (var r in records)
                {
                    string timeStr = r.Time_Reception_Position.HasValue
                        ? r.Time_Reception_Position.Value.ToString(@"hh\:mm\:ss")
                        : "--:--:--";

                    string ta = r.Target_Address ?? "------";
                    string ti = r.Target_Identification ?? "--------";

                    // ==========================================================
                    //
                    // THIS IS THE LINE THAT WAS BROKEN
                    // It's now fixed to use .HasValue just like you did in WriteCsv
                    //
                    string bpsStr = r.BarometricPressureSetting.HasValue // <-- THIS IS CORRECT
                        ? r.BarometricPressureSetting.Value.ToString("F1", CultureInfo.InvariantCulture)
                        : "--";
                    //
                    // ==========================================================

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

                    Console.WriteLine(
                        "{0,-7} {1,-4} {2,-4} {3,-10} {4,-10:F5} {5,-10:F5} {6,-8} {7,-8} {8,-10} {9,-10} {10,-8}",
                        "CAT021",
                        sac,
                        sic,
                        timeStr,
                        r.WGS84_Latitude,
                        r.WGS84_Longitude,
                        r.Mode3A_Code ?? "---",
                        r.Flight_Level,
                        ta,
                        ti,
                        bpsStr
                    );
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