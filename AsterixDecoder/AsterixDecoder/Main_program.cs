using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using AsterixDecoder.IO;
using AsterixDecoder.Models.CAT048;
using MultiCAT6.Utils;

namespace AsterixDecoder
{
    class Main_program
    {
        // Coordenadas del Radar BCN según el documento
        private static readonly CoordinatesWGS84 RadarBCN = new CoordinatesWGS84(
            GeoUtils.LatLon2Radians(41, 18, 2.5284, 0),    // Latitud: 41° 18' 02.5284" N
            GeoUtils.LatLon2Radians(2, 6, 7.4095, 0),      // Longitud: 02° 06' 07.4095" E
            27.257                                          // Elevación terreno (2.007m) + altura antena (25.25m)
        );

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("====================================");
                Console.WriteLine("   DECODIFICADOR ASTERIX CAT048");
                Console.WriteLine("====================================\n");

                // Paso 1: Inicializar GeoUtils con el centro de proyección en el radar BCN
                Console.WriteLine("[1/5] Inicializando sistema de coordenadas...");
                GeoUtils geoUtils = new GeoUtils(0.081819190843, 6378137.0, RadarBCN);
                Console.WriteLine("      ✓ GeoUtils inicializado correctamente\n");

                // Paso 2: Abrir y leer el archivo binario
                Console.WriteLine("[2/5] Leyendo archivo ASTERIX binario...");
                var reader = new BinaryFileReader("datos_asterix_radar.ast");
                var messages = reader.ReadMessages();
                Console.WriteLine($"      ✓ Archivo leído: {messages.Count} mensajes encontrados\n");

                // Paso 3: Crear lista de objetos CAT048
                var cat048List = new Cat048List();

                // Paso 4: Procesar cada mensaje CAT048
                Console.WriteLine("[3/5] Decodificando mensajes CAT048...");
                int processedCount = 0;
                int cat048Count = 0;
                
                foreach (var message in messages)
                {
                    byte cat = message[0];

                    if (cat == 48)
                    {
                        cat048Count++;
                        
                        // Decodificar mensaje con Cat048Decoder
                        var decoder = new Cat048Decoder(message);
                        var rawRecords = decoder.Decode();

                        // Convertir cada registro decodificado a objeto Cat048
                        foreach (var rawRecord in rawRecords)
                        {
                            var cat048Object = new Cat048(rawRecord, geoUtils, RadarBCN);
                            cat048List.Add(cat048Object);
                            processedCount++;
                        }
                    }
                }

                Console.WriteLine($"      ✓ Mensajes CAT048 encontrados: {cat048Count}");
                Console.WriteLine($"      ✓ Registros procesados: {processedCount}\n");

                // Paso 5: Mostrar estadísticas y resultados
                Console.WriteLine("[4/5] Generando estadísticas...");
                if (cat048List.Count > 0)
                {
                    cat048List.PrintSummary();

                    var uniqueAddresses = cat048List.GetUniqueAircraftAddresses();
                    var uniqueTargetIds = cat048List.GetUniqueTargetIds();

                    Console.WriteLine("\n--- Detalle por aeronave ---");
                    foreach (var addr in uniqueAddresses.Take(5)) // Mostrar primeras 5
                    {
                        var recordsForAddress = cat048List.FilterByAircraftAddress(addr);
                        var firstRecord = recordsForAddress.First();
                        Console.WriteLine($"  {addr} ({firstRecord.TI}): {recordsForAddress.Count} registros");
                    }
                    
                    if (uniqueAddresses.Count > 5)
                    {
                        Console.WriteLine($"  ... y {uniqueAddresses.Count - 5} aeronaves más");
                    }
                }
                else
                {
                    Console.WriteLine("      ⚠ No se encontraron registros CAT048 en el archivo.");
                }

                // Paso 6: Opciones de exportación
                Console.WriteLine("\n[5/5] Opciones de salida:");
                Console.WriteLine("  1 - Mostrar todos los registros detallados");
                Console.WriteLine("  2 - Exportar a CSV");
                Console.WriteLine("  3 - Mostrar registros de una aeronave específica");
                Console.WriteLine("  4 - Salir");
                Console.Write("\nSelecciona una opción: ");

                string option = Console.ReadLine();

                switch (option)
                {
                    case "1":
                        cat048List.PrintAll();
                        break;
                        
                    case "2":
                        string csvContent = cat048List.ExportToCSV();
                        string filename = $"cat048_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                        File.WriteAllText(filename, csvContent);
                        Console.WriteLine($"\n✓ Archivo CSV exportado: {filename}");
                        Console.WriteLine($"  Total de registros: {cat048List.Count}");
                        break;
                        
                    case "3":
                        var addresses = cat048List.GetUniqueAircraftAddresses();
                        if (addresses.Count > 0)
                        {
                            Console.WriteLine("\nAeronaves disponibles:");
                            for (int i = 0; i < addresses.Count; i++)
                            {
                                var firstRec = cat048List.FilterByAircraftAddress(addresses[i]).First();
                                Console.WriteLine($"  {i + 1}. {addresses[i]} - {firstRec.TI}");
                            }
                            Console.Write("\nSelecciona número: ");
                            if (int.TryParse(Console.ReadLine(), out int idx) && idx > 0 && idx <= addresses.Count)
                            {
                                var selectedRecords = cat048List.FilterByAircraftAddress(addresses[idx - 1]);
                                Console.WriteLine($"\n=== Registros de {addresses[idx - 1]} ===");
                                foreach (var rec in selectedRecords)
                                {
                                    Console.WriteLine($"Time: {rec.Time} | LAT: {rec.LAT:F6} | LON: {rec.LON:F6} | " +
                                        $"FL: {rec.FL:F0} | GS: {rec.GS:F1} kt | HDG: {rec.HDG:F1}°");
                                }
                            }
                        }
                        break;
                        
                    case "4":
                    default:
                        Console.WriteLine("\nSaliendo...");
                        break;
                }

                Console.WriteLine("\n====================================");
                Console.WriteLine("  Decodificación completada con éxito");
                Console.WriteLine("====================================");
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"\n*** ERROR: Archivo no encontrado ***");
                Console.WriteLine($"No se pudo encontrar el archivo: {ex.Message}");
                Console.WriteLine("Asegúrate de que el archivo 'datos_asterix_radar.ast' existe en el directorio de ejecución.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n*** ERROR CRÍTICO ***");
                Console.WriteLine($"Mensaje: {ex.Message}");
                Console.WriteLine($"\nDetalles técnicos:");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("\n\nPresiona cualquier tecla para salir...");
            Console.ReadKey();
        }
    }
}