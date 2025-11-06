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
    /// Lista de objetos CAT021 con funcionalidades adicionales
    /// </summary>
    public class Cat021List
    {
        private List<Cat021> records;

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
                Console.WriteLine($"  - Real Altitude (ft): {r.Real_Altitude_ft:F2}");
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
            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                // Encabezados
                writer.WriteLine("CAT;SAC;SIC;Time;LAT;LON;Mode3A_Code;FL;Real_Alt_ft;TA;TI;BP;OnGround");

                // Datos
                foreach (var r in records)
                {
                    string latStr = r.LAT.ToString("F6", CultureInfo.InvariantCulture);
                    string lonStr = r.LON.ToString("F6", CultureInfo.InvariantCulture);
                    string altStr = r.Real_Altitude_ft.ToString("F0", CultureInfo.InvariantCulture);
                    string bpStr = r.BP.HasValue 
                        ? r.BP.Value.ToString("F2", CultureInfo.InvariantCulture) 
                        : "N/A";

                    writer.WriteLine($"{r.CAT};{r.SAC};{r.SIC};{r.Time};{latStr};{lonStr};" +
                                     $"{r.Mode3A};{r.FL};{altStr};{r.TA};{r.TI};{bpStr};{r.IsOnGround}");
                }

            }

            //Console.WriteLine($"\nArchivo CSV exportado: {filePath}");
        }

        /// <summary>
        /// Limpia la lista
        /// </summary>
        public void Clear()
        {
            records.Clear();
        }

        /// <summary>
        /// Muestra estadísticas de la lista
        /// </summary>
        public void PrintStatistics()
        {
            Console.WriteLine("\n=== ESTADÍSTICAS CAT021 ===");
            Console.WriteLine($"Total de registros: {records.Count}");

            int onGround = records.Count(r => r.IsOnGround);
            int airborne = records.Count - onGround;
            Console.WriteLine($"En tierra: {onGround}");
            Console.WriteLine($"En vuelo: {airborne}");

            var validRecords = records.Where(r => r.IsValid()).ToList();
            Console.WriteLine($"Registros válidos (airborne + FIR BCN): {validRecords.Count}");

            var uniqueAddresses = GetUniqueTargetAddresses();
            Console.WriteLine($"Aeronaves únicas (Target Address): {uniqueAddresses.Count}");

            var uniqueIdentifications = GetUniqueTargetIdentifications();
            Console.WriteLine($"Identificaciones únicas: {uniqueIdentifications.Count}");

            if (validRecords.Count > 0)
            {
                double avgAlt = validRecords.Average(r => r.Real_Altitude_ft);
                double maxAlt = validRecords.Max(r => r.Real_Altitude_ft);
                double minAlt = validRecords.Min(r => r.Real_Altitude_ft);

                Console.WriteLine($"\nAltitudes (registros válidos):");
                Console.WriteLine($"  - Promedio: {avgAlt:F0} ft");
                Console.WriteLine($"  - Máxima: {maxAlt:F0} ft");
                Console.WriteLine($"  - Mínima: {minAlt:F0} ft");
            }
        }
    }
}
