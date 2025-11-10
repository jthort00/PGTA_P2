using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using AsterixDecoder.Models.CAT048;
using AsterixDecoder.Models.CAT021;

namespace AsterixDecoder.Models
{
    public static class CombinedCSVFileGenerator
    {
        public static void ExportUnified(Cat048List cat048List, Cat021List cat021List)
        {
            if (cat048List == null || cat021List == null)
            {
                Console.WriteLine("❌ Cannot export: one of the lists is null.");
                return;
            }

            // --- 1️⃣ CAT021 inferred BP logic ---
            var inferredBPS = new Dictionary<string, double>();
            foreach (var r in cat021List.GetAll())
            {
                if (!string.IsNullOrEmpty(r.TI) && r.BP.HasValue)
                {
                    double bps = r.BP.Value;
                    if (bps >= 1000.0 && bps <= 1030.0)
                        if (!inferredBPS.ContainsKey(r.TI))
                            inferredBPS[r.TI] = bps;
                }
            }

            foreach (var r in cat021List.GetAll())
            {
                if (!r.BP.HasValue && !string.IsNullOrEmpty(r.TI) && inferredBPS.TryGetValue(r.TI, out double inferred))
                    r.BP = inferred;
            }

            // --- 2️⃣ Combine both lists into unified records ---
            var combined = new List<UnifiedRecord>();

            foreach (var r in cat048List.GetAll())
            {
                combined.Add(new UnifiedRecord
                {
                    CAT = 48,
                    SAC = r.SAC,
                    SIC = r.SIC,
                    Time = r.Time,
                    LAT = r.LAT,
                    LON = r.LON,
                    H_WGS = r.H_m,
                    H_FT = r.H,
                    RHO = r.RHO,
                    THETA = r.THETA,
                    Mode3A = r.Mode3A,
                    FlightLevel = r.FL,
                    ModeC_Corrected = r.H,
                    TA = r.TA,
                    TI = r.TI,
                    Mode_S = BuildModeSField(r),
                    BP = r.BP,
                    RA = r.RA,
                    TTA = r.TTA,
                    GS = r.GS,
                    TAR = r.TAR,
                    TAS = r.TAS,
                    HDG = r.HDG,
                    IAS = r.IAS,
                    MACH = r.MACH,
                    BAR = r.BAR,
                    IVV = r.IVV,
                    TrackNumber = r.TN,
                    GroundSpeedKT = r.GSSD,
                    Heading = r.HDG2,
                    STAT230 = BuildSAT230Field(r)
                });
            }

            foreach (var r in cat021List.GetAll())
            {
                combined.Add(new UnifiedRecord
                {
                    CAT = 21,
                    SAC = r.SAC,
                    SIC = r.SIC,
                    Time = r.Time,
                    LAT = r.LAT,
                    LON = r.LON,
                    H_WGS = null,
                    H_FT = null,
                    RHO = null,
                    THETA = null,
                    Mode3A = r.Mode3A,
                    FlightLevel = r.FL,
                    ModeC_Corrected = r.ModeC_Corrected,
                    TA = r.TA,
                    TI = r.TI,
                    Mode_S = "NV",
                    BP = r.BP,
                    RA = null,
                    TTA = null,
                    GS = null,
                    TAR = null,
                    TAS = null,
                    HDG = null,
                    IAS = null,
                    MACH = null,
                    BAR = null,
                    IVV = null,
                    TrackNumber = null,
                    GroundSpeedKT = null,
                    Heading = null,
                    STAT230 = "NV"
                });
            }

            // --- 3️⃣ Sort by time ascending ---
            var sorted = combined.OrderBy(c => c.Time).ToList();

            // --- 4️⃣ Write CSV file ---
            string filename = $"Combined_CAT_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            using (var writer = new StreamWriter(filename, false, Encoding.UTF8))
            {
                // Header
                writer.WriteLine(
                    "CAT;SAC;SIC;Time;Latitude;Longitude;h_wgs84;h_ft;RHO;THETA;Mode3A;Flight_Level;ModeC_Corrected;TA;TI;Mode_S;BPS;RA;TTA;GS;TAR;TAS;HDG;IAS;MACH;BAR;IVV;Track_number;Ground_Speedkt;Heading;STAT230");

                // Data rows
                foreach (var r in sorted)
                {
                    writer.WriteLine(
                        $"{r.CAT};{FormatNullableInt(r.SAC)};{FormatNullableInt(r.SIC)};{FormatStringField(r.Time)};" +
                        $"{FormatNullableDouble(r.LAT)};{FormatNullableDouble(r.LON)};" +
                        $"{FormatNullableDouble(r.H_WGS)};{FormatNullableDouble(r.H_FT)};" +
                        $"{FormatNullableDouble(r.RHO)};{FormatNullableDouble(r.THETA)};" +
                        $"{FormatStringField(r.Mode3A)};{FormatNullableDouble(r.FlightLevel)};{FormatNullableDouble(r.ModeC_Corrected)};" +
                        $"{FormatStringField(r.TA)};{FormatStringField(r.TI)};{FormatStringField(r.Mode_S)};{FormatNullableDouble(r.BP)};" +
                        $"{FormatNullableDouble(r.RA)};{FormatNullableDouble(r.TTA)};{FormatNullableDouble(r.GS)};{FormatNullableDouble(r.TAR)};{FormatNullableDouble(r.TAS)};" +
                        $"{FormatNullableDouble(r.HDG)};{FormatNullableDouble(r.IAS)};{FormatNullableDouble(r.MACH)};{FormatNullableDouble(r.BAR)};" +
                        $"{FormatNullableDouble(r.IVV)};{FormatNullableInt(r.TrackNumber)};{FormatNullableDouble(r.GroundSpeedKT)};{FormatNullableDouble(r.Heading)};{FormatStringField(r.STAT230)}"
                    );
                }
            }
            
            Console.WriteLine($"\n✓ Combined unified CSV exported: {filename}");
            Console.WriteLine($"  CAT048: {cat048List.Count}  |  CAT021: {cat021List.Count}");
            Console.WriteLine($"  Total combined: {sorted.Count}");
        }

        // ===== Helper formatting methods =====
        private static string FormatNullableDouble(double? value) =>
            value.HasValue ? value.Value.ToString("F1", CultureInfo.InvariantCulture) : "NV";


        private static string FormatNullableInt(int? value) =>
            value.HasValue ? value.Value.ToString() : "NV";

        private static string FormatStringField(string value) =>
            !string.IsNullOrEmpty(value) ? value : "NV";

        private static string FormatNullableBool(bool? value) =>
            value.HasValue ? value.Value.ToString() : "NV";

        // ===== Unified record structure =====
        private class UnifiedRecord
        {
            public int CAT { get; set; }
            public int? SAC { get; set; }
            public int? SIC { get; set; }
            public string Time { get; set; } // now string
            public double? LAT { get; set; }
            public double? LON { get; set; }
            public double? H_WGS { get; set; }
            public double? H_FT { get; set; }
            public double? RHO { get; set; }
            public double? THETA { get; set; }
            public string Mode3A { get; set; }
            public double? FlightLevel { get; set; }
            public double? ModeC_Corrected { get; set; }
            public string TA { get; set; }
            public string TI { get; set; }
            public string Mode_S { get; set; }
            public double? BP { get; set; }
            public double? RA { get; set; }
            public double? TTA { get; set; }
            public double? GS { get; set; }
            public double? TAR { get; set; }
            public double? TAS { get; set; }
            public double? HDG { get; set; }
            public double? IAS { get; set; }
            public double? MACH { get; set; }
            public double? BAR { get; set; }
            public double? IVV { get; set; }
            public int? TrackNumber { get; set; }
            public double? GroundSpeedKT { get; set; }
            public double? Heading { get; set; }
            public string STAT230 { get; set; }
        }

        // ===== Placeholder helpers for CAT048 specifics =====
        private static string BuildModeSField(Cat048 r) => "NV"; 
        private static string BuildSAT230Field(Cat048 r) => "NV"; 
    }
}
