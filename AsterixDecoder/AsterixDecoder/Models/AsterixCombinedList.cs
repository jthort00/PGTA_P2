using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Text;
using AsterixDecoder.Models.CAT048;
using AsterixDecoder.Models.CAT021;
using AsterixDecoder.IO;
using MultiCAT6.Utils;

namespace AsterixDecoder.Models
{
    /// <summary>
    /// Lista combinada que permite almacenar conjuntamente registros CAT048 y CAT021.
    /// Pensada para flujos donde un mismo fichero contiene ambas categorías.
    /// </summary>
    public class AsterixCombinedList
    {
        public enum AsterixCategory { CAT048, CAT021 }

        /// <summary>
        /// Registro discriminado que envuelve a un Cat048 o un Cat021.
        /// </summary>
        public class AsterixRecord
        {
            public AsterixCategory Category { get; }
            public Cat048? Cat048 { get; }
            public Cat021? Cat021 { get; }

            private AsterixRecord(AsterixCategory category, Cat048? cat048, Cat021? cat021)
            {
                Category = category;
                Cat048 = cat048;
                Cat021 = cat021;
            }

            public static AsterixRecord From(Cat048 value) => new AsterixRecord(AsterixCategory.CAT048, value, null);
            public static AsterixRecord From(Cat021 value) => new AsterixRecord(AsterixCategory.CAT021, null, value);
        }

        private readonly List<AsterixRecord> _records = new List<AsterixRecord>();

        /// <summary>
        /// Añade un registro CAT048.
        /// </summary>
        public void Add(Cat048 cat048) => _records.Add(AsterixRecord.From(cat048));

        /// <summary>
        /// Añade un registro CAT021.
        /// </summary>
        public void Add(Cat021 cat021) => _records.Add(AsterixRecord.From(cat021));

        /// <summary>
        /// Añade un registro genérico ya envuelto.
        /// </summary>
        public void Add(AsterixRecord record)
        {
            if (record != null) _records.Add(record);
        }

        /// <summary>
        /// Añade múltiples registros ya envueltos.
        /// </summary>
        public void AddRange(IEnumerable<AsterixRecord> records)
        {
            if (records == null) return;
            _records.AddRange(records.Where(r => r != null));
        }

        /// <summary>
        /// Número total de registros (sumando 048 y 021).
        /// </summary>
        public int Count => _records.Count;

        /// <summary>
        /// Obtiene todos los registros en el orden de inserción.
        /// </summary>
        public IReadOnlyList<AsterixRecord> GetAll() => _records;

        /// <summary>
        /// Obtiene solo los registros CAT048.
        /// </summary>
        public List<Cat048> GetCat048() => _records.Where(r => r.Category == AsterixCategory.CAT048 && r.Cat048 != null)
                                                   .Select(r => r.Cat048!)
                                                   .ToList();

        /// <summary>
        /// Obtiene solo los registros CAT021.
        /// </summary>
        public List<Cat021> GetCat021() => _records.Where(r => r.Category == AsterixCategory.CAT021 && r.Cat021 != null)
                                                   .Select(r => r.Cat021!)
                                                   .ToList();

        /// <summary>
        /// Cuenta por categoría.
        /// </summary>
        public (int cat048, int cat021) GetCountsByCategory()
        {
            int c48 = _records.Count(r => r.Category == AsterixCategory.CAT048);
            int c21 = _records.Count(r => r.Category == AsterixCategory.CAT021);
            return (c48, c21);
        }

        /// <summary>
        /// Devuelve una nueva lista combinada que contiene únicamente registros CAT021.
        /// No modifica la lista actual.
        /// </summary>
        public AsterixCombinedList FilterCat021()
        {
            var filtered = new AsterixCombinedList();
            foreach (var r in _records)
            {
                if (r.Category == AsterixCategory.CAT021 && r.Cat021 != null)
                {
                    filtered.Add(r.Cat021);
                }
            }
            return filtered;
        }

        /// <summary>
        /// Devuelve una nueva lista combinada que contiene únicamente registros CAT048.
        /// No modifica la lista actual.
        /// </summary>
        public AsterixCombinedList FilterCat048()
        {
            var filtered = new AsterixCombinedList();
            foreach (var r in _records)
            {
                if (r.Category == AsterixCategory.CAT048 && r.Cat048 != null)
                {
                    filtered.Add(r.Cat048);
                }
            }
            return filtered;
        }

        /// <summary>
        /// Imprime un pequeño resumen por consola (opcional).
        /// </summary>
        public void PrintSummary()
        {
            var (c48, c21) = GetCountsByCategory();
            Console.WriteLine($"AsterixCombinedList -> Total: {Count} (CAT048: {c48}, CAT021: {c21})");
        }

        /// <summary>
        /// Crea una lista combinada leyendo y decodificando un archivo ASTERIX (.ast) que puede contener
        /// mensajes CAT048 y/o CAT021. Para cada mensaje se invoca el decodificador pertinente y se agregan
        /// los registros resultantes en el orden de lectura.
        /// </summary>
        /// <param name="filePath">Ruta del archivo .ast</param>
        /// <returns>AsterixCombinedList con todos los registros CAT048 y CAT021.</returns>
        public static AsterixCombinedList FromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Ruta de archivo no válida", nameof(filePath));

            var combined = new AsterixCombinedList();

            // Leer mensajes binarios del archivo
            var reader = new BinaryFileReader(filePath);
            var messages = reader.ReadMessages();

            // Inicializar utilidades geodésicas (Radar BCN)
            var radarBcn = new CoordinatesWGS84(
                GeoUtils.LatLon2Radians(41, 18, 2.5284, 0),
                GeoUtils.LatLon2Radians(2, 6, 7.4095, 0),
                27.257
            );
            GeoUtils geoUtils = new GeoUtils(0.081819190843, 6378137.0, radarBcn);
            double qnh = 1013.25; // QNH por defecto

            foreach (var message in messages)
            {
                if (message == null || message.Length < 1) continue;
                byte cat = message[0];

                if (cat == 48)
                {
                    var decoder = new CAT048.Cat048Decoder(message);
                    var rawRecords = decoder.Decode();
                    foreach (var raw in rawRecords)
                    {
                        var obj = new CAT048.Cat048(raw, geoUtils, radarBcn);
                        combined.Add(obj);
                    }
                }
                else if (cat == 21)
                {
                    var decoder = new Cat021Decoder(message, qnh);
                    var rawRecords = decoder.Decode();
                    foreach (var raw in rawRecords)
                    {
                        var obj = new Cat021(raw, qnh);
                        combined.Add(obj);
                    }
                }
            }

            return combined;
        }

        // ===  CSV combinado CAT048 + CAT021  ===
        private class UnifiedRecord
        {
            public int CAT { get; set; }
            public int? SAC { get; set; }
            public int? SIC { get; set; }
            public string Time { get; set; }
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

        private static string FormatNullableInt(int? v) => v.HasValue ? v.Value.ToString(CultureInfo.InvariantCulture) : "";
        private static string FormatNullableDouble(double? v)
        {
            if (!v.HasValue) return "";
            return v.Value.ToString("G", CultureInfo.InvariantCulture);
        }
        private static string FormatStringField(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            // Escapar comillas si aparecieran (aunque usamos ';' como separador)
            return s.Replace("\"", "''");
        }

        private static DateTime? ParseTime(string t)
        {
            if (string.IsNullOrWhiteSpace(t)) return null;
            string[] formats = new[] { "HH:mm:ss:fff", "HH:mm:ss.fff", "HH:mm:ss" };
            if (DateTime.TryParseExact(t, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt;
            // Intento fallback general
            if (DateTime.TryParse(t, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                return dt;
            return null;
        }

        private static string BuildModeSField(Cat048 r)
        {
            // Intento componer un resumen si hay algo disponible
            var parts = new List<string>();
            if (r.COM.HasValue) parts.Add($"COM={r.COM}");
            if (r.SI.HasValue) parts.Add($"SI={(r.SI.Value ? 1 : 0)}");
            if (r.MSSC.HasValue) parts.Add($"MSSC={(r.MSSC.Value ? 1 : 0)}");
            if (r.ARC.HasValue) parts.Add($"ARC={(r.ARC.Value ? 1 : 0)}");
            if (r.AIC.HasValue) parts.Add($"AIC={(r.AIC.Value ? 1 : 0)}");
            return parts.Count > 0 ? string.Join(",", parts) : "NV";
        }

        private static string BuildSTAT230Field(Cat048 r)
        {
            if (!string.IsNullOrWhiteSpace(r.Stat)) return r.Stat;
            if (r.STAT_230.HasValue) return r.STAT_230.Value.ToString(CultureInfo.InvariantCulture);
            return "NV";
        }

        /// <summary>
        /// Genera un CSV combinado de CAT048 y CAT021 con el mismo layout que el ejemplo proporcionado.
        /// </summary>
        /// <param name="outputPath">Ruta de salida opcional. Si es null, se genera en el directorio actual con nombre por fecha.</param>
        /// <returns>Ruta completa del fichero CSV generado.</returns>
        public string ExportCombinedToCSV(string outputPath = null)
        {
            // 1) Proyección a registros unificados
            var combined = new List<UnifiedRecord>();

            foreach (var r in _records)
            {
                if (r.Category == AsterixCategory.CAT048 && r.Cat048 != null)
                {
                    var c = r.Cat048;
                    combined.Add(new UnifiedRecord
                    {
                        CAT = 48,
                        SAC = c.SAC,
                        SIC = c.SIC,
                        Time = c.Time,
                        LAT = c.LAT,
                        LON = c.LON,
                        H_WGS = c.H_m,
                        H_FT = c.H,
                        RHO = c.RHO,
                        THETA = c.THETA,
                        Mode3A = c.Mode3A,
                        FlightLevel = c.FL,
                        ModeC_Corrected = c.H,
                        TA = c.TA,
                        TI = c.TI,
                        Mode_S = BuildModeSField(c),
                        BP = c.BP,
                        RA = c.RA,
                        TTA = c.TTA,
                        GS = c.GS,
                        TAR = c.TAR,
                        TAS = c.TAS,
                        HDG = c.HDG,
                        IAS = c.IAS,
                        MACH = c.MACH,
                        BAR = c.BAR,
                        IVV = c.IVV,
                        TrackNumber = c.TN,
                        GroundSpeedKT = c.GSSD,
                        Heading = c.HDG2,
                        STAT230 = BuildSTAT230Field(c)
                    });
                }
                else if (r.Category == AsterixCategory.CAT021 && r.Cat021 != null)
                {
                    var c = r.Cat021;
                    combined.Add(new UnifiedRecord
                    {
                        CAT = 21,
                        SAC = c.SAC,
                        SIC = c.SIC,
                        Time = c.Time,
                        LAT = c.LAT,
                        LON = c.LON,
                        H_WGS = null,
                        H_FT = null,
                        RHO = null,
                        THETA = null,
                        Mode3A = c.Mode3A,
                        FlightLevel = c.FL,
                        ModeC_Corrected = c.ModeC_Corrected,
                        TA = c.TA,
                        TI = c.TI,
                        Mode_S = "NV",
                        BP = c.BP,
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
            }

            // 2) Orden por tiempo ascendente
            var sorted = combined.OrderBy(c => ParseTime(c.Time)).ThenBy(c => c.Time).ToList();

            // 3) Escritura CSV
            string filename = outputPath ?? $"Combined_CAT_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            using (var writer = new StreamWriter(filename, false, Encoding.UTF8))
            {
                writer.WriteLine("CAT;SAC;SIC;Time;Latitude;Longitude;h_wgs84;h_ft;RHO;THETA;Mode3A;Flight_Level;ModeC_Corrected;TA;TI;Mode_S;BPS;RA;TTA;GS;TAR;TAS;HDG;IAS;MACH;BAR;IVV;Track_number;Ground_Speedkt;Heading;STAT230");
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
            var (c48, c21) = GetCountsByCategory();
            Console.WriteLine($"  CAT048: {c48}  |  CAT021: {c21}");
            Console.WriteLine($"  Total combined: {sorted.Count}");

            return filename;
        }
    }
}
