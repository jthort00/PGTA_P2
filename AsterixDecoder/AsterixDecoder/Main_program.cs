using System;
using System.Linq;
using AsterixDecoder.IO;
using AsterixDecoder.Models;

namespace AsterixDecoder
{
    class Main_program
    {
        static void Main(string[] args)
        {
            try
            {
                // Leer el archivo binario
                var reader = new BinaryFileReader("datos_asterix_radar.ast");
                var messages = reader.ReadMessages();

                Console.WriteLine("=".PadRight(80, '='));
                Console.WriteLine($"DECODIFICADOR ASTERIX CAT048");
                Console.WriteLine("=".PadRight(80, '='));
                Console.WriteLine($"Total de mensajes encontrados: {messages.Count}\n");

                int cat048Count = 0;

                // Procesar cada mensaje
                for (int i = 0; i < messages.Count; i++)
                {
                    byte[] data = messages[i];
                    byte cat = data[0];

                    // Solo procesar mensajes CAT048
                    if (cat == 48)
                    {
                        cat048Count++;
                        Console.WriteLine($"\n--- MENSAJE {i + 1} (CAT048 #{cat048Count}) ---");
                        Console.WriteLine($"Longitud: {data.Length} bytes");

                        // Decodificar el mensaje
                        var decoder = new Cat048Decoder(data);
                        var records = decoder.Decode();

                        // Mostrar los registros decodificados
                        foreach (var record in records)
                        {
                            PrintRecord(record);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"\nMensaje {i + 1}: Categoría {cat} (no CAT048) - {data.Length} bytes");
                    }
                }

                Console.WriteLine("\n" + "=".PadRight(80, '='));
                Console.WriteLine($"RESUMEN:");
                Console.WriteLine($"  Total mensajes procesados: {messages.Count}");
                Console.WriteLine($"  Mensajes CAT048: {cat048Count}");
                Console.WriteLine($"  Otros: {messages.Count - cat048Count}");
                Console.WriteLine("=".PadRight(80, '='));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nERROR: {ex.Message}");
                Console.WriteLine($"Stack Trace:\n{ex.StackTrace}");
            }

            Console.WriteLine("\nPresiona cualquier tecla para salir...");
            Console.ReadKey();
        }

        static void PrintRecord(Cat048Decoder.Cat048Record record)
        {
            Console.WriteLine("\n  DATOS DEL REGISTRO:");

            // Data Source Identifier
            if (!string.IsNullOrEmpty(record.DataSourceIdentifier))
                Console.WriteLine($"    Data Source: {record.DataSourceIdentifier}");

            // Time of Day
            if (record.TimeOfDay.HasValue)
                Console.WriteLine($"    Time of Day: {record.TimeOfDay.Value:hh\\:mm\\:ss\\.fff}");

            // Target Report Descriptor
            if (!string.IsNullOrEmpty(record.TargetReportDescriptor))
                Console.WriteLine($"    Target Report: {record.TargetReportDescriptor}");

            // Measured Position
            if (record.RhoSlantRange > 0)
            {
                Console.WriteLine($"    Slant Range (Rho): {record.RhoSlantRange:F3} NM");
                Console.WriteLine($"    Azimuth (Theta): {record.ThetaAzimuth:F2}°");
            }

            // Mode 3/A Code
            if (!string.IsNullOrEmpty(record.Mode3ACode))
                Console.WriteLine($"    Mode 3/A Code: {record.Mode3ACode}");

            // Flight Level
            if (record.FlightLevel != 0)
                Console.WriteLine($"    Flight Level: FL{record.FlightLevel / 4.0:F2} ({record.FlightLevel} quarters)");

            // Aircraft Address
            if (!string.IsNullOrEmpty(record.AircraftAddress))
                Console.WriteLine($"    Aircraft Address (Mode S): {record.AircraftAddress}");

            // Aircraft Identification
            if (!string.IsNullOrEmpty(record.AircraftIdentification))
                Console.WriteLine($"    Callsign: {record.AircraftIdentification}");

            // Track Number
            if (record.TrackNumber > 0)
                Console.WriteLine($"    Track Number: {record.TrackNumber}");

            // Ground Speed and Heading
            if (record.GroundSpeedKnots > 0)
            {
                Console.WriteLine($"    Ground Speed: {record.GroundSpeedKnots:F2} kt");
                Console.WriteLine($"    Heading: {record.HeadingDegrees:F2}°");
            }

            // 3D Radar Height
            if (record.Height3DRadar > 0)
                Console.WriteLine($"    3D Height: {record.Height3DRadar:F0} feet");

            // Radial Doppler Speed
            if (record.RadialDopplerSpeed != 0)
                Console.WriteLine($"    Radial Doppler Speed: {record.RadialDopplerSpeed:F2} m/s");

            // Mode S MB Data
            if (record.ModeSData != null && record.ModeSData.MBDataBlocks.Count > 0)
            {
                Console.WriteLine($"    Mode S MB Data: {record.ModeSData.MBDataBlocks.Count} block(s)");

                foreach (var block in record.ModeSData.MBDataBlocks)
                {
                    Console.WriteLine($"      BDS Register: {block.BDSRegister:X2}");

                    // BDS 4.0 - Selected Vertical Intention
                    if (block.BDSRegister == 0x40)
                    {
                        if (block.MCP_FCU_SelectedAltitude > 0)
                            Console.WriteLine($"        MCP/FCU Altitude: {block.MCP_FCU_SelectedAltitude} feet");
                        if (block.FMS_SelectedAltitude > 0)
                            Console.WriteLine($"        FMS Altitude: {block.FMS_SelectedAltitude} feet");
                    }

                    // BDS 5.0 - Track and Turn Report
                    else if (block.BDSRegister == 0x50)
                    {
                        if (block.RollAngle != 0)
                            Console.WriteLine($"        Roll Angle: {block.RollAngle:F2}°");
                        if (block.TrueTrackAngle != 0)
                            Console.WriteLine($"        True Track: {block.TrueTrackAngle:F2}°");
                        if (block.GroundSpeed != 0)
                            Console.WriteLine($"        Ground Speed (BDS5.0): {block.GroundSpeed:F2} kt");
                    }

                    // BDS 6.0 - Heading and Speed Report
                    else if (block.BDSRegister == 0x60)
                    {
                        if (block.MagneticHeading != 0)
                            Console.WriteLine($"        Magnetic Heading: {block.MagneticHeading:F2}°");
                        if (block.IndicatedAirspeed != 0)
                            Console.WriteLine($"        IAS: {block.IndicatedAirspeed:F2} kt");
                        if (block.MachNumber != 0)
                            Console.WriteLine($"        Mach: {block.MachNumber:F3}");
                    }

                    // Mostrar raw data en hexadecimal
                    Console.WriteLine($"        Raw: {BitConverter.ToString(block.RawData)}");
                }
            }

            // Radar Plot Characteristics
            if (!string.IsNullOrEmpty(record.RadarPlotCharacteristics))
                Console.WriteLine($"    Radar Plot Characteristics: {record.RadarPlotCharacteristics}");

            // Track Status
            if (!string.IsNullOrEmpty(record.TrackStatus))
                Console.WriteLine($"    Track Status: {record.TrackStatus}");

            // Warning/Error Conditions
            if (!string.IsNullOrEmpty(record.WarningErrorConditions))
                Console.WriteLine($"    Warnings/Errors: {record.WarningErrorConditions}");

            // Communications/ACAS Capability
            if (!string.IsNullOrEmpty(record.CommunicationsACASCapability))
                Console.WriteLine($"    Comm/ACAS: {record.CommunicationsACASCapability}");
        }
    }
}
