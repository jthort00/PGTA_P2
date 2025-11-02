using System;
using System.Collections.Generic;
using System.Linq;

namespace AsterixDecoder.Models.CAT048
{
    /// <summary>
    /// Lista de objetos CAT048 con funcionalidades adicionales
    /// </summary>
    public class Cat048List
    {
        private List<Cat048> records;

        public Cat048List()
        {
            records = new List<Cat048>();
        }

        /// <summary>
        /// Añade un registro CAT048 a la lista
        /// </summary>
        public void Add(Cat048 record)
        {
            records.Add(record);
        }

        /// <summary>
        /// Añade múltiples registros a la lista
        /// </summary>
        public void AddRange(IEnumerable<Cat048> recordsToAdd)
        {
            records.AddRange(recordsToAdd);
        }

        /// <summary>
        /// Obtiene el número de registros en la lista
        /// </summary>
        public int Count => records.Count;

        /// <summary>
        /// Obtiene un registro por índice
        /// </summary>
        public Cat048 this[int index] => records[index];

        /// <summary>
        /// Obtiene todos los registros
        /// </summary>
        public List<Cat048> GetAll() => records;

        /// <summary>
        /// Filtra registros por Aircraft Address
        /// </summary>
        public List<Cat048> FilterByAircraftAddress(string address)
        {
            return records.Where(r => r.TA == address).ToList();
        }

        /// <summary>
        /// Filtra registros por Track Number
        /// </summary>
        public List<Cat048> FilterByTrackNumber(int trackNumber)
        {
            return records.Where(r => r.TN == trackNumber).ToList();
        }

        /// <summary>
        /// Obtiene todos los Aircraft Addresses únicos
        /// </summary>
        public List<string> GetUniqueAircraftAddresses()
        {
            return records.Where(r => r.TA != "N/A")
                         .Select(r => r.TA)
                         .Distinct()
                         .ToList();
        }

        /// <summary>
        /// Obtiene todos los Target IDs únicos
        /// </summary>
        public List<string> GetUniqueTargetIds()
        {
            return records.Where(r => r.TI != "N/A")
                         .Select(r => r.TI)
                         .Distinct()
                         .ToList();
        }

        /// <summary>
        /// Imprime todos los registros en formato de lista
        /// </summary>
        public void PrintAll()
        {
            if (records.Count == 0)
            {
                Console.WriteLine("No hay registros CAT048 en la lista.");
                return;
            }

            Console.WriteLine($"\n=== LISTA DE REGISTROS CAT048 ({records.Count} items) ===\n");

            for (int i = 0; i < records.Count; i++)
            {
                var r = records[i];
                Console.WriteLine($"* Item {i + 1}:");
                Console.WriteLine($"  - CAT: {r.CAT}");
                Console.WriteLine($"  - SAC: {r.SAC}");
                Console.WriteLine($"  - SIC: {r.SIC}");
                Console.WriteLine($"  - Time: {r.Time}");
                Console.WriteLine($"  - LAT: {FormatValue(r.LAT, "F6")}");
                Console.WriteLine($"  - LON: {FormatValue(r.LON, "F6")}");
                Console.WriteLine($"  - H (QNH corr.): {FormatValue(r.H_m, "F2")} m");
                Console.WriteLine($"  - H(m): {FormatValue(r.H, "F0")} ft");
                Console.WriteLine($"  - RHO: {FormatValue(r.RHO, "F3")} NM");
                Console.WriteLine($"  - THETA: {FormatValue(r.THETA, "F2")} deg");
                Console.WriteLine($"  - Mode3/A: {r.Mode3A}");
                Console.WriteLine($"  - FL: {FormatValue(r.FL, "F0")}");
                Console.WriteLine($"  - TA (Target Address): {r.TA}");
                Console.WriteLine($"  - TI (Target ID): {r.TI}");
                Console.WriteLine($"  - BP (Baro Press): {FormatValue(r.BP, "F2")} hPa");
                Console.WriteLine($"  - RA (Antenna): {r.RA}");
                Console.WriteLine($"  - Roll Angle: {FormatValue(r.RollAngle, "F2")} deg");
                Console.WriteLine($"  - ITA: {r.ITA}");
                Console.WriteLine($"  - GS (calc): {FormatValue(r.GS, "F1")} kt");
                Console.WriteLine($"  - TAR (Track Rate): {FormatValue(r.TAR, "F3")} deg/s");
                Console.WriteLine($"  - TAS: {FormatValue(r.TAS, "F0")} kt");
                Console.WriteLine($"  - HDG (-180/180): {FormatValue(r.HDG, "F1")} deg");
                Console.WriteLine($"  - IAS: {FormatValue(r.IAS, "F0")} kt");
                Console.WriteLine($"  - MACH: {FormatValue(r.MACH, "F3")}");
                Console.WriteLine($"  - BAR (Alt Rate): {FormatValue(r.BAR, "F1")} ft/min");
                Console.WriteLine($"  - IVV (Vert Vel): {FormatValue(r.IVV, "F0")} ft/min");
                Console.WriteLine($"  - TN (Track#): {(r.TN.HasValue ? r.TN.Value.ToString() : "N/A")}");
                Console.WriteLine($"  - GSSD (BDS): {FormatValue(r.GSSD, "F0")} kt");
                Console.WriteLine($"  - HDG2 (0-360): {FormatValue(r.HDG2, "F1")} deg");
                Console.WriteLine($"  - Stat: {r.Stat}");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Imprime un resumen compacto
        /// </summary>
        public void PrintSummary()
        {
            if (records.Count == 0)
            {
                Console.WriteLine("No hay registros CAT048 en la lista.");
                return;
            }

            Console.WriteLine($"\n=== RESUMEN CAT048 ({records.Count} registros) ===");
            Console.WriteLine($"Aeronaves únicas: {GetUniqueAircraftAddresses().Count}");
            Console.WriteLine($"Target IDs únicos: {GetUniqueTargetIds().Count}");
            
            var recordsWithQNH = records.Count(r => r.BP.HasValue);
            Console.WriteLine($"Registros con QNH: {recordsWithQNH}");
            
            var recordsWithModeS = records.Count(r => r.TAS.HasValue);
            Console.WriteLine($"Registros con Mode S Data: {recordsWithModeS}");
        }

        /// <summary>
        /// Formatea un valor nullable para impresión
        /// </summary>
        private string FormatValue(double? value, string format)
        {
            return value.HasValue ? value.Value.ToString(format) : "N/A";
        }

        /// <summary>
        /// Limpia la lista
        /// </summary>
        public void Clear()
        {
            records.Clear();
        }

        /// <summary>
        /// Exporta los registros a formato CSV con campos ordenados según especificación
        /// </summary>
        public string ExportToCSV()
        {
            var csv = new System.Text.StringBuilder();
            
            // Header
            csv.AppendLine("CAT,SAC,SIC,Time,LAT,LON,H,H(m),RHO,THETA,Mode3/A,FL,TA,TI,BP,RA,TTA,GS,TAR,TAS,HDG,IAS,MACH,BAR,IVV,TN,GS(kt),HDG2,STAT");
            
            // Data rows
            foreach (var r in records)
            {
                csv.AppendLine(
                    $"{r.CAT}," +
                    $"{r.SAC}," +
                    $"{r.SIC}," +
                    $"{r.Time}," +
                    $"{FormatCSVInvariant(r.LAT, "F8")}," +      // Más precisión para LAT
                    $"{FormatCSVInvariant(r.LON, "F8")}," +      // Más precisión para LON
                    $"{FormatCSVInvariant(r.H)}," +              // H en pies
                    $"{FormatCSVInvariant(r.H_m)}," +            // H en metros
                    $"{FormatDoubleInvariant(r.RHO, "F6")}," +   // RHO con más precisión
                    $"{FormatDoubleInvariant(r.THETA, "F6")}," + // THETA con más precisión
                    $"{r.Mode3A}," +
                    $"{FormatCSVInvariant(r.FL)}," +             // FL puede ser N/A
                    $"{r.TA}," +
                    $"{r.TI}," +
                    $"{FormatCSVInvariant(r.BP, "F0")}," +       // BP sin decimales
                    $"{FormatCSVInvariant(r.RollAngle, "F3")}," +// TTA (Roll Angle)
                    $"{FormatCSVInvariant(r.GS, "F3")}," +       // GS calculado
                    $"{FormatCSVInvariant(r.TAR, "F0")}," +      // TAR sin decimales
                    $"{(r.TAS.HasValue ? r.TAS.Value.ToString("F0", System.Globalization.CultureInfo.InvariantCulture) : "NV")}," + // TAS
                    $"{FormatCSVInvariant(r.HDG, "F6")}," +      // HDG (-180/180)
                    $"{(r.IAS.HasValue ? r.IAS.Value.ToString("F0", System.Globalization.CultureInfo.InvariantCulture) : "NV")}," + // IAS
                    $"{(r.MACH.HasValue ? r.MACH.Value.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) : "NV")}," + // MACH
                    $"{FormatCSVInvariant(r.BAR, "F0")}," +      // BAR
                    $"{FormatCSVInvariant(r.IVV, "F0")}," +      // IVV
                    $"{(r.TN.HasValue ? r.TN.Value.ToString() : "")}," + // TN
                    $"{FormatCSVInvariant(r.GSSD, "F1")}," +     // GS desde BDS 5.0
                    $"{FormatCSVInvariant(r.HDG2, "F4")}," +     // HDG2 (0-360)
                    $"\"{r.Stat}\"");
            }
            
            return csv.ToString();
        }

        private string FormatCSV(double? value)
        {
            return value.HasValue ? value.Value.ToString("F6") : "";
        }

        // Método auxiliar actualizado
        private string FormatCSVInvariant(double? value, string format = "F6", string nullValue = "N/A")
        {
            if (!value.HasValue)
                return nullValue;
            
            return value.Value.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
        }

        private string FormatDoubleInvariant(double value, string format)
        {
            return value.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
