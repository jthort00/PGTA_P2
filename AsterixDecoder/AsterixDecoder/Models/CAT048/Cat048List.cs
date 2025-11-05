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
            return records.Where(r => !string.IsNullOrWhiteSpace(r.TA))
                         .Select(r => r.TA)
                         .Distinct()
                         .ToList();
        }

        /// <summary>
        /// Obtiene todos los Target IDs únicos
        /// </summary>
        public List<string> GetUniqueTargetIds()
        {
            return records.Where(r => !string.IsNullOrWhiteSpace(r.TI))
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
                Console.WriteLine($"  - CAT: {FormatStringValue(r.CAT)}");
                Console.WriteLine($"  - SAC: {FormatIntValue(r.SAC)}");
                Console.WriteLine($"  - SIC: {FormatIntValue(r.SIC)}");
                Console.WriteLine($"  - Time: {FormatStringValue(r.Time)}");
                Console.WriteLine($"  - LAT: {FormatDoubleValue(r.LAT, "F6")}");
                Console.WriteLine($"  - LON: {FormatDoubleValue(r.LON, "F6")}");
                Console.WriteLine($"  - H (QNH corr.): {FormatDoubleValue(r.H_m, "F2")} m");
                Console.WriteLine($"  - H(ft): {FormatDoubleValue(r.H, "F0")} ft");
                Console.WriteLine($"  - RHO: {FormatDoubleValue(r.RHO, "F3")} NM");
                Console.WriteLine($"  - THETA: {FormatDoubleValue(r.THETA, "F2")} deg");
                Console.WriteLine($"  - Mode3/A: {FormatStringValue(r.Mode3A)}");
                Console.WriteLine($"  - FL: {FormatDoubleValue(r.FL, "F0")}");
                Console.WriteLine($"  - TA (Target Address): {FormatStringValue(r.TA)}");
                Console.WriteLine($"  - TI (Target ID): {FormatStringValue(r.TI)}");
                Console.WriteLine($"  - BP (Baro Press): {FormatDoubleValue(r.BP, "F2")} hPa");
                Console.WriteLine($"  - RA (Roll angle): {FormatDoubleValue(r.RA, "F2")}");
                Console.WriteLine($"  - TTA: {FormatDoubleValue(r.TTA, "F3")} deg");
                Console.WriteLine($"  - GS: {FormatDoubleValue(r.GS, "F3")} kt");
                Console.WriteLine($"  - TAR: {FormatDoubleValue(r.TAR, "F3")} deg/s");
                Console.WriteLine($"  - TAS: {FormatDoubleValue(r.TAS, "F3")} kt");
                Console.WriteLine($"  - HDG: {FormatDoubleValue(r.HDG, "F3")} deg");
                Console.WriteLine($"  - IAS: {FormatDoubleValue(r.IAS, "F3")} kt");
                Console.WriteLine($"  - MACH: {FormatDoubleValue(r.MACH, "F3")}" );
                Console.WriteLine($"  - BAR: {FormatDoubleValue(r.BAR, "F3")} ft/min");
                Console.WriteLine($"  - IVV: {FormatDoubleValue(r.IVV, "F3")} ft/min");
                Console.WriteLine($"  - Track Number: {FormatIntValue(r.TN)}");
                Console.WriteLine($"  - Ground Speed (kt): {FormatDoubleValue(r.GSSD, "F3")} kt");
                Console.WriteLine($"  - Heading: {FormatDoubleValue(r.HDG2, "F3")} deg");
                Console.WriteLine($"  - Stat: {FormatStringValue(r.Stat)}");
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
        /// Todos los valores nulos o vacíos se muestran como "N/A"
        /// </summary>
        public string ExportToCSV()
        {
            var csv = new System.Text.StringBuilder();
            
            // Header - Columnas según especificación
            csv.AppendLine("CAT\tSAC\tSIC\tTime\tLat\tLon\tH_wgs\th_ft\tRHO\tTHETA\tMode_3A\tFlight_level\tModeC_corrected\tTarget_address\tTarget_identification\tMode_S\tBP\tRA\tTTA\tGS\tTAR\tTAS\tHDG\tIAS\tMACH\tBAR\tIVV\tTrack_number\tGround_speedkt\tHeading\tSAT230");
            
            // Data rows
            foreach (var r in records)
            {
                // Construir campo Mode_S (BDS registers presentes)
                string modeS = BuildModeSField(r);
                
                // Construir campo SAT230 (Communications capability)
                string sat230 = BuildSAT230Field(r);
                
                csv.AppendLine(
                    $"{FormatStringValue(r.CAT)}\t" +
                    $"{FormatIntValue(r.SAC)}\t" +
                    $"{FormatIntValue(r.SIC)}\t" +
                    $"{FormatStringValue(r.Time)}\t" +
                    $"{FormatDoubleValue(r.LAT, "F8")}\t" +           // Lat
                    $"{FormatDoubleValue(r.LON, "F8")}\t" +           // Lon
                    $"{FormatDoubleValue(r.H_m, "F14")}\t" +          // H_wgs (metros WGS84)
                    $"{FormatDoubleValue(r.H, "F14")}\t" +            // h_ft (pies)
                    $"{FormatDoubleValue(r.RHO, "F6")}\t" +           // RHO
                    $"{FormatDoubleValue(r.THETA, "F6")}\t" +         // THETA
                    $"{FormatStringValue(r.Mode3A)}\t" +               // Mode_3A
                    $"{FormatDoubleValue(r.FL, "F3")}\t" +            // Flight_level
                    $"{FormatDoubleValue(r.H, "F3")}\t" +             // ModeC_corrected
                    $"{FormatStringValue(r.TA)}\t" +                    // Target_address
                    $"{FormatStringValue(r.TI)}\t" +                    // Target_identification
                    $"{FormatStringValue(modeS)}\t" +                   // Mode_S (BDS registers)
                    $"{FormatDoubleValue(r.BP, "F3")}\t" +            // BP
                    $"{FormatDoubleValue(r.RA, "F3")}\t" +            // RA
                    $"{FormatDoubleValue(r.TTA, "F3")}\t" +           // TTA
                    $"{FormatDoubleValue(r.GS, "F3")}\t" +            // GS
                    $"{FormatDoubleValue(r.TAR, "F3")}\t" +           // TAR
                    $"{FormatDoubleValue(r.TAS, "F3")}\t" +           // TAS
                    $"{FormatDoubleValue(r.HDG, "F6")}\t" +           // HDG
                    $"{FormatDoubleValue(r.IAS, "F3")}\t" +           // IAS
                    $"{FormatDoubleValue(r.MACH, "F3")}\t" +          // MACH
                    $"{FormatDoubleValue(r.BAR, "F3")}\t" +           // BAR
                    $"{FormatDoubleValue(r.IVV, "F3")}\t" +           // IVV
                    $"{FormatIntValue(r.TN)}\t" +                       // Track_number
                    $"{FormatDoubleValue(r.GSSD, "F3")}\t" +          // Ground_speedkt
                    $"{FormatDoubleValue(r.HDG2, "F4")}\t" +          // Heading
                    $"{FormatStringValue(sat230)}");                     // SAT230
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

        /// <summary>
        /// Formatea un valor double nullable - devuelve "N/A" si es null
        /// </summary>
        private string FormatDoubleValue(double? value, string format = "F6")
        {
            if (!value.HasValue)
                return "N/A";
            
            return value.Value.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
        }

        // Sobrecarga para valores no-nullable
        private string FormatDoubleValue(double value, string format = "F6")
        {
            return value.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Formatea un valor int nullable - devuelve "N/A" si es null
        /// </summary>
        private string FormatIntValue(int? value)
        {
            if (!value.HasValue)
                return "N/A";
            
            return value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Formatea un valor string - devuelve "N/A" si es null, vacío o "N/A"
        /// </summary>
        private string FormatStringValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "N/A")
                return "N/A";
            
            return value;
        }
    }
}
