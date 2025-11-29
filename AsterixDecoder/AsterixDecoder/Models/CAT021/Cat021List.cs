using AsterixDecoder.Models.CAT048;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace AsterixDecoder.Models.CAT021
{
    /// <summary>
    /// Lista de objetos CAT021
    /// </summary>
    public class Cat021List
    {
        private List<Cat021> records = new List<Cat021>();

        // Exponer la lista como IEnumerable para poder recorrerla
        public IEnumerable<Cat021> Records => records;

        public Cat021List()
        {
            records = new List<Cat021>();
        }

        /// <summary>
        /// Añade un registro CAT021 a la lista
        /// </summary>
        public void Add(Cat021 record)
        {
            records.Add(record);
        }

        /// <summary>
        /// Añade múltiples registros a la lista
        /// </summary>
        public void AddRange(IEnumerable<Cat021> recordsToAdd)
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
        public Cat021 this[int index] => records[index];

        /// <summary>
        /// Obtiene todos los registros
        /// </summary>
        public List<Cat021> GetAll() => records;

        /// <summary>
        /// Filtra registros por Target Address
        /// </summary>
        public List<Cat021> FilterByTargetAddress(string address)
        {
            return records.Where(r => r.TA == address).ToList();
        }

        /// <summary>
        /// Filtra registros por Target Identification
        /// </summary>
        public List<Cat021> FilterByTargetIdentification(string identification)
        {
            return records.Where(r => r.TI == identification).ToList();
        }

        /// <summary>
        /// Filtra solo registros válidos (airborne y dentro del FIR)
        /// </summary>
        public List<Cat021> GetValidRecords()
        {
            return records.Where(r => r.IsValid()).ToList();
        }

        /// <summary>
        /// Obtiene todos los Target Addresses únicos
        /// </summary>
        public List<string> GetUniqueTargetAddresses()
        {
            return records.Where(r => r.TA != "N/A")
                .Select(r => r.TA)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Obtiene todas las identificaciones únicas
        /// </summary>
        public List<string> GetUniqueTargetIdentifications()
        {
            return records.Where(r => r.TI != "N/A")
                .Select(r => r.TI)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Aplica filtros del FIR de Barcelona
        /// </summary>
        public void ApplyBarcelonaFIRFilter()
        {
            records = records.Where(r => !r.IsOnGround && r.IsWithinBarcelonaFIR()).ToList();
        }

        public void InferMissingBP()
        {
            var bpByTI = new Dictionary<string, double>();

            // 1️⃣ Build dictionary (TI → BP) only for FL ≤ 60
            foreach (var r in records)
            {
                if (!string.IsNullOrEmpty(r.TI) &&
                    r.BP.HasValue &&
                    r.FL <= 60)
                {
                    double bps = r.BP.Value;

                    if (bps >= 1000 && bps <= 1030)
                    {
                        if (!bpByTI.ContainsKey(r.TI))
                            bpByTI[r.TI] = bps;
                    }
                }
            }

            // 2️⃣ Infer missing BP
            foreach (var r in records)
            {
                // Skip if BP already present
                if (r.BP.HasValue)
                    continue;

                if (string.IsNullOrEmpty(r.TI))
                    continue;

                if (r.FL <= 60)
                {
                    // Try to infer BP from same TI
                    if (bpByTI.TryGetValue(r.TI, out double inferred))
                    {
                        r.BP = inferred;
                    }
                    // else leave as null → will use fallback QNH later
                }
                else
                {
                    // 3️⃣ NUEVA REGLA: Por encima de FL60 → usar siempre presión estándar
                    r.BP = 1013.25;
                }
            }
        }

        
        /// <summary>
        /// Recalcula ModeC_Corrected después de modificar BP.
        /// </summary>
        public void RecomputeCorrectedAltitudes(double qnhDefault = 1013.25)
        {
            foreach (var r in records)
            {
                // 🛬 On ground → blank altitude
                if (r.IsOnGround)
                {
                    r.FL = 0;
                    r.ModeC_Corrected = null;
                    continue;
                }

                // ✈ Por encima de FL60 → presión estándar + altitud corregida en blanco
                if (r.FL > 60)
                {
                    r.ModeC_Corrected= null;
                    continue;
                }

                // ✈ Por debajo de FL60 → calcular altitud corregida
                double qnhToUse = r.BP.HasValue ? r.BP.Value : qnhDefault;

                r.ModeC_Corrected = (100.0 * r.FL) + (qnhToUse - 1013.25) * 30.0;

                // Evitar negativos pequeños causados por fluctuación de QNH
                if (r.ModeC_Corrected < 0)
                    r.ModeC_Corrected = 0;
            }
        }

        /// <summary>
        /// Imprime todos los registros en formato de lista
        /// </summary>
        public void PrintAll()
        {
            if (records.Count == 0)
            {
                Console.WriteLine("No hay registros CAT021 en la lista.");
                return;
            }

            Console.WriteLine($"\n=== LISTA DE REGISTROS CAT021 ({records.Count} items) ===\n");

            for (int i = 0; i < records.Count; i++)
            {
                var r = records[i];
                Console.WriteLine($"* Item {i + 1}:");
                Console.WriteLine($"  - CAT: {r.CAT}");
                Console.WriteLine($"  - SAC: {r.SAC}");
                Console.WriteLine($"  - SIC: {r.SIC}");
                Console.WriteLine($"  - Time: {r.Time}");
                Console.WriteLine($"  - LAT: {r.LAT:F6}");
                Console.WriteLine($"  - LON: {r.LON:F6}");
                Console.WriteLine($"  - Mode3/A: {r.Mode3A}");
                Console.WriteLine($"  - FL: {r.FL}");
                Console.WriteLine($"  - ModeC_Corrected: {r.ModeC_Corrected:F2}");
                Console.WriteLine($"  - TA: {r.TA}");
                Console.WriteLine($"  - TI: {r.TI}");
                Console.WriteLine($"  - BP: {r.BP:F3}");
                Console.WriteLine($"  - IsOnGround: {r.IsOnGround}");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Exporta la lista a un archivo CSV
        /// </summary>
        public void ExportToCSV(string filePath)
        {
            // Asegurar BP correcto antes de exportar
            InferMissingBP();
            RecomputeCorrectedAltitudes();

            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                writer.WriteLine("CAT\tSAC\tSIC\tTime\tLAT\tLON\tMode3A_Code\tFL\tModeC_Corrected\tTA\tTI\tBP\tOnGround");

                foreach (var r in records.Where(r =>
                             !r.IsOnGround &&
                             r.Mode3A != "7777" &&
                             (string.IsNullOrEmpty(r.TI) ||
                              (!char.IsDigit(r.TI[0]) && r.TI.Length != 3))
                         ))
                {
                    string latStr = FormatDoubleValue(r.LAT, "F8");
                    string lonStr = FormatDoubleValue(r.LON, "F8");
                    string altStr = r.ModeC_Corrected.HasValue ? r.ModeC_Corrected.Value.ToString("F4", CultureInfo.InvariantCulture) : "N/A";

                    string bpStr = "N/A";
                    if (r.BP.HasValue && r.BP.Value >= 1000 && r.BP.Value <= 1030)
                        bpStr = r.BP.Value.ToString("F2", CultureInfo.InvariantCulture);

                    writer.WriteLine(
                        $"{FormatStringValue(r.CAT)}\t{FormatIntValue(r.SAC)}\t{FormatIntValue(r.SIC)}\t{FormatStringValue(r.Time)}\t" +
                        $"{latStr}\t{lonStr}\t{FormatStringValue(r.Mode3A)}\t{FormatDoubleValue(r.FL, "F2")}\t{altStr}\t" +
                        $"{FormatStringValue(r.TA)}\t{FormatStringValue(r.TI)}\t{bpStr}\t{r.IsOnGround}"
                    );
                }
            }
        }

        public void Clear()
        {
            records.Clear();
        }

        public void PrintStatistics()
        {
            Console.WriteLine("\n=== ESTADÍSTICAS CAT021 ===");
            Console.WriteLine($"Total de registros: {records.Count}");

            int onGround = records.Count(r => r.IsOnGround);
            int airborne = records.Count - onGround;
            Console.WriteLine($"En tierra: {onGround}");
            Console.WriteLine($"En vuelo: {airborne}");

            var validRecords = records.Where(r => r.IsValid()).ToList();
            Console.WriteLine($"Registros válidos: {validRecords.Count}");

            var uniqueAddresses = GetUniqueTargetAddresses();
            Console.WriteLine($"Aeronaves únicas: {uniqueAddresses.Count}");

            var uniqueIdentifications = GetUniqueTargetIdentifications();
            Console.WriteLine($"Identificaciones únicas: {uniqueIdentifications.Count}");

            if (validRecords.Count > 0)
            {
                double? avgAlt = validRecords.Average(r => r.ModeC_Corrected);
                double? maxAlt = validRecords.Max(r => r.ModeC_Corrected);
                double? minAlt = validRecords.Min(r => r.ModeC_Corrected);

                Console.WriteLine($"\nAltitudes (válidos):");
                Console.WriteLine($"  - Promedio: {(avgAlt.HasValue ? avgAlt.Value.ToString("F4", CultureInfo.InvariantCulture) : "N/A")} ft");
                Console.WriteLine($"  - Máxima: {(maxAlt.HasValue ? maxAlt.Value.ToString("F4", CultureInfo.InvariantCulture) : "N/A")} ft");
                Console.WriteLine($"  - Mínima: {(minAlt.HasValue ? minAlt.Value.ToString("F4", CultureInfo.InvariantCulture) : "N/A")} ft");
            }
        }

        // Helper formatting methods for CSV/output consistency (aligned with CAT048)
        private string FormatDoubleValue(double? value, string format = "F6")
        {
            return value.HasValue ? value.Value.ToString(format, CultureInfo.InvariantCulture) : "N/A";
        }

        private string FormatIntValue(int? value)
        {
            return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "N/A";
        }

        private string FormatStringValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) || value == "N/A" ? "N/A" : value;
        }
    }
}