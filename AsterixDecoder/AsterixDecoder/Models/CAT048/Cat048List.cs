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
                Console.WriteLine($"  - RA (Roll angle): {FormatValue(r.RA, "F2")}");
                Console.WriteLine($"  - TTA: {r.TTA}");
                Console.WriteLine($"  - GS (calc): {FormatValue(r.GS, "F3")} kt");
                Console.WriteLine($"  - TAR (Track Rate): {FormatValue(r.TAR, "F3")} deg/s");
                Console.WriteLine($"  - TAS: {FormatValue(r.TAS, "F3")} kt");
                Console.WriteLine($"  - HDG: {FormatValue(r.HDG, "F3")} deg");
                Console.WriteLine($"  - IAS: {FormatValue(r.IAS, "F3")} kt");
                Console.WriteLine($"  - MACH: {FormatValue(r.MACH, "F3")}");
                Console.WriteLine($"  - BAR (Alt Rate): {FormatValue(r.BAR, "F3")} ft/min");
                Console.WriteLine($"  - IVV (Vert Vel): {FormatValue(r.IVV, "F3")} ft/min");
                Console.WriteLine($"  - TN (Track#): {(r.TN.HasValue ? r.TN.Value.ToString() : "N/A")}");
                Console.WriteLine($"  - GSSD (BDS): {FormatValue(r.GSSD, "F3")} kt");
                Console.WriteLine($"  - HDG2 (0-360): {FormatValue(r.HDG2, "F3")} deg");
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
            
            // Header - Nuevas columnas según especificación
            csv.AppendLine("CAT\tSAC\tSIC\tTime\tLat\tLon\tH_wgs\th_ft\tRHO\tTHETA\tMode_3A\tFlight_level\tModeC_corrected\tTarget_address\tTarget_identification\tMode_S\tBP\tRA\tTTA\tGS\tTAR\tTAS\tHDG\tIAS\tMACH\tBAR\tIVV\tTrack_number\tGround_speedkt\tHeading\tSAT230");
            
            // Data rows
            foreach (var r in records)
            {
                // Construir campo Mode_S (BDS registers presentes)
                string modeS = BuildModeSField(r);
                
                // Construir campo SAT230 (Communications capability)
                string sat230 = BuildSAT230Field(r);
                
                csv.AppendLine(
                    $"{r.CAT}\t" +
                    $"{r.SAC}\t" +
                    $"{r.SIC}\t" +
                    $"{r.Time}\t" +
                    $"{FormatCSVInvariant(r.LAT, "F8")}\t" +      // Lat
                    $"{FormatCSVInvariant(r.LON, "F8")}\t" +      // Lon
                    $"{FormatCSVInvariant(r.H_m, "F14")}\t" +     // H_wgs (metros WGS84)
                    $"{FormatCSVInvariant(r.H, "F14")}\t" +       // h_ft (pies)
                    $"{FormatDoubleInvariant(r.RHO, "F6")}\t" +   // RHO
                    $"{FormatDoubleInvariant(r.THETA, "F6")}\t" + // THETA
                    $"{r.Mode3A}\t" +                             // Mode_3A
                    $"{FormatCSVInvariant(r.FL, "F3", "")}\t" +   // Flight_level
                    $"{FormatCSVInvariant(r.H, "F3", "")}\t" +    // ModeC_corrected (H corregido con QNH)
                    $"{r.TA}\t" +                                 // Target_address
                    $"{r.TI}\t" +                                 // Target_identification
                    $"{modeS}\t" +                                // Mode_S (BDS registers)
                    $"{FormatCSVInvariant(r.BP, "F3", "")}\t" +   // BP
                    $"{FormatCSVInvariant(r.RA, "F3")}\t" +                                 // RA
                    $"{FormatCSVInvariant(r.TTA, "F3")}\t" +// TTA (Roll Angle)
                    $"{FormatCSVInvariant(r.GS, "F0", "")}\t" +   // GS
                    $"{FormatCSVInvariant(r.TAR, "F0", "")}\t" +  // TAR
                    $"{FormatCSVInvariant(r.TAS, "F0", "")}\t" +  // TAS
                    $"{FormatCSVInvariant(r.HDG, "F6")}\t" +      // HDG
                    $"{FormatCSVInvariant(r.IAS, "F3", "")}\t" +  // IAS
                    $"{FormatCSVInvariant(r.MACH, "F3")}\t" +     // MACH
                    $"{FormatCSVInvariant(r.BAR, "F3", "")}\t" +  // BAR
                    $"{FormatCSVInvariant(r.IVV, "F3", "")}\t" +  // IVV
                    $"{FormatCSVInvariant(r.TN, "F3", "")}\t" + // Track_number
                    $"{FormatCSVInvariant(r.GSSD, "F3", "")}\t" + // Ground_speedkt (BDS 5.0)
                    $"{FormatCSVInvariant(r.HDG2, "F4")}\t" +     // Heading (0-360)
                    $"{sat230}");                                  // SAT230
            }
            
            return csv.ToString();
        }

        /// <summary>
        /// Construye el campo Mode_S con los BDS registers presentes
        /// </summary>
        private string BuildModeSField(Cat048 record)
        {
            var bdsList = new List<string>();
            
            // Verificar qué campos BDS están presentes
            if (record.BP.HasValue) bdsList.Add("BDS:4,0");
            if (record.RA.HasValue || record.TAR.HasValue || record.TAS.HasValue || record.GS.HasValue)
                bdsList.Add("BDS:5,0");
            if (record.HDG.HasValue || record.IAS.HasValue || record.MACH.HasValue || 
                record.BAR.HasValue || record.IVV.HasValue)
                bdsList.Add("BDS:6,0");
            
            return bdsList.Count > 0 ? string.Join(" ", bdsList) : "";
        }

        /// <summary>
        /// Construye el campo SAT230 (Communications/ACAS Capability)
        /// </summary>
        private string BuildSAT230Field(Cat048 record)
        {
            if (!record.COM.HasValue && !record.STAT_230.HasValue)
                return "";
            
            return record.Stat ?? "";
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
