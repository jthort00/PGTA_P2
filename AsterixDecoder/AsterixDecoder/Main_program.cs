using System;
using System.Collections.Generic;
using System.Linq;
using AsterixDecoder.IO;
using AsterixDecoder.Models;
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
                // Inicializar GeoUtils con el centro de proyección en el radar BCN
                GeoUtils geoUtils = new GeoUtils(0.081819190843, 6378137.0, RadarBCN);

                // Leer el archivo binario
                var reader = new BinaryFileReader("datos_asterix_radar.ast");
                var messages = reader.ReadMessages();

                Console.WriteLine("Procesando archivo ASTERIX CAT048...\n");

                var allCat048Records = new List<Cat048Decoder.Cat048Record>();

                // Procesar todos los mensajes CAT048
                foreach (var message in messages)
                {
                    byte cat = message[0];

                    if (cat == 48)
                    {
                        var decoder = new Cat048Decoder(message);
                        var records = decoder.Decode();
                        allCat048Records.AddRange(records);
                    }
                }

                // Mostrar CAT048 en formato tabular
                if (allCat048Records.Count > 0)
                {
                    Console.WriteLine("=== ASTERIX CAT048 RECORDS ===\n");
                    PrintCat048Table(allCat048Records, geoUtils);

                    Console.WriteLine($"\n\nTotal CAT048 records: {allCat048Records.Count}");
                }
                else
                {
                    Console.WriteLine("No se encontraron registros CAT048 en el archivo.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nERROR: {ex.Message}");
                Console.WriteLine($"Stack Trace:\n{ex.StackTrace}");
            }


            static void PrintCat048Table(List<Cat048Decoder.Cat048Record> records, GeoUtils geoUtils)
            {
                // Encabezados basados en la imagen
                var headers = new[]
                {
                "CAT", "SAC", "SIC", "Time", "LAT", "LON", "H", "H(m)",
                "RHO", "THETA", "Mode3/A", "FL", "TA", "TI", "BP", "RA",
                "ITA", "GS", "TAR", "TAS", "HDG", "IAS", "MACH", "BAR",
                "IVV", "TN", "GSSD", "HDG2", "Stat", "AD"
            };

                // Imprimir encabezados
                Console.Write("| ");
                foreach (var h in headers)
                {
                    Console.Write($"{h,-10} | ");
                }
                Console.WriteLine();

                // Línea separadora
                Console.Write("|-");
                foreach (var h in headers)
                {
                    Console.Write(new string('-', 10) + "-|-");
                }
                Console.WriteLine();

                // Imprimir cada registro
                int rowNum = 1;
                foreach (var record in records)
                {
                    Console.Write("| ");

                    // CAT
                    Console.Write($"{"CAT048",-10} | ");

                    // SAC, SIC
                    var sac = "N/A";
                    var sic = "N/A";
                    if (!string.IsNullOrEmpty(record.DataSourceIdentifier))
                    {
                        var parts = record.DataSourceIdentifier.Split(' ');
                        if (parts.Length >= 2)
                        {
                            sac = parts[0].Replace("SAC:", "");
                            sic = parts[1].Replace("SIC:", "");
                        }
                    }
                    Console.Write($"{sac,-10} | ");
                    Console.Write($"{sic,-10} | ");

                    // Time
                    var time = "N/A";
                    if (record.TimeOfDay.HasValue)
                    {
                        var totalHours = (int)record.TimeOfDay.Value.TotalHours;
                        var minutes = record.TimeOfDay.Value.Minutes;
                        var seconds = record.TimeOfDay.Value.Seconds;
                        var millis = record.TimeOfDay.Value.Milliseconds;
                        time = $"{totalHours:D2}:{minutes:D2}:{seconds:D2}.{millis:D3}";
                    }
                    Console.Write($"{time,-10} | ");

                    // Convertir coordenadas polares a WGS84
                    string lat = "N/A", lon = "N/A", alt = "N/A";

                    if (record.RhoSlantRange > 0 && record.ThetaAzimuth >= 0)
                    {
                        try
                        {
                            // Convertir de NM a metros
                            double rhoMeters = record.RhoSlantRange * GeoUtils.NM2METERS;
                            double thetaRadians = record.ThetaAzimuth * GeoUtils.DEGS2RADS;

                            // Calcular elevación si tenemos altura 3D
                            double elevation = 0;
                            //if (record.Height3DRadar > 0)
                            //{
                              //  double heightMeters = record.Height3DRadar * GeoUtils.FEET2METERS;
                                //elevation = GeoUtils.CalculateElevation(RadarBCN, geoUtils.R_S, rhoMeters, heightMeters);
                            //}

                            // Crear coordenadas polares
                            CoordinatesPolar polar = new CoordinatesPolar(rhoMeters, thetaRadians, elevation);

                            // Convertir: Polar -> Cartesiano Radar -> Geocéntrico -> Geodésico (WGS84)
                            CoordinatesXYZ radarCart = GeoUtils.change_radar_spherical2radar_cartesian(polar);
                            CoordinatesXYZ geocentric = geoUtils.change_radar_cartesian2geocentric(RadarBCN, radarCart);
                            CoordinatesWGS84 wgs84 = geoUtils.change_geocentric2geodesic(geocentric);

                            if (wgs84 != null)
                            {
                                // Convertir radianes a grados
                                lat = $"{(wgs84.Lat * GeoUtils.RADS2DEGS):F6}";
                                lon = $"{(wgs84.Lon * GeoUtils.RADS2DEGS):F6}";
                                alt = $"{wgs84.Height:F3}";
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }

                    // LAT, LON, ALT
                    Console.Write($"{lat,-10} | ");
                    Console.Write($"{lon,-10} | ");
                    Console.Write($"{alt,-10} | ");

                    // H(m) (Height en metros derivados de Height3DRadar)
                    //var heightM = record.Height3DRadar > 0 ? $"{(record.Height3DRadar * 0.3048):F0}" : "N/A";
                    //Console.Write($"{heightM,-10} | ");

                    // RHO (Slant Range)
                    var rho = record.RhoSlantRange > 0 ? $"{record.RhoSlantRange:F3}" : "N/A";
                    Console.Write($"{rho,-10} | ");

                    // THETA (Azimuth)
                    var theta = record.ThetaAzimuth >= 0 ? $"{record.ThetaAzimuth:F2}" : "N/A";
                    Console.Write($"{theta,-10} | ");

                    // Mode 3/A
                    Console.Write($"{record.Mode3ACode ?? "N/A",-10} | ");

                    // FL (Flight Level)
                    var fl = record.FlightLevel != 0 ? $"{(record.FlightLevel / 4.0):F0}" : "N/A";
                    Console.Write($"{fl,-10} | ");

                    // TA, TI (de Mode S si disponible)
                    var ta = "N/A";
                    var ti = "N/A";
                    if (record.ModeSData?.MBDataBlocks != null)
                    {
                        var bds50 = record.ModeSData.MBDataBlocks.FirstOrDefault(b => b.BDSRegister == 0x50);
                        if (bds50 != null && bds50.TrueTrackAngle > 0)
                            ta = $"{bds50.TrueTrackAngle:F5}";
                    }
                    Console.Write($"{ta,-10} | ");
                    Console.Write($"{ti,-10} | ");

                    // BP (Barometric Pressure - no disponible)
                    Console.Write($"{"N/A",-10} | ");

                    // RA (Roll Angle de Mode S BDS 5.0)
                    var ra = "N/A";
                    if (record.ModeSData?.MBDataBlocks != null)
                    {
                        var bds50 = record.ModeSData.MBDataBlocks.FirstOrDefault(b => b.BDSRegister == 0x50);
                        if (bds50 != null && bds50.RollAngle != 0)
                            ra = $"{bds50.RollAngle:F2}";
                    }
                    Console.Write($"{ra,-10} | ");

                    // ITA (no disponible)
                    Console.Write($"{"N/A",-10} | ");

                    // GS (Ground Speed)
                    var gs = record.GroundSpeedKnots > 0 ? $"{record.GroundSpeedKnots:F1}" : "N/A";
                    Console.Write($"{gs,-10} | ");

                    // TAR (Track Angle Rate de Mode S BDS 5.0)
                    var tar = "N/A";
                    if (record.ModeSData?.MBDataBlocks != null)
                    {
                        var bds50 = record.ModeSData.MBDataBlocks.FirstOrDefault(b => b.BDSRegister == 0x50);
                        if (bds50 != null && bds50.TrackAngleRate != 0)
                            tar = $"{bds50.TrackAngleRate:F2}";
                    }
                    Console.Write($"{tar,-10} | ");

                    // TAS (True Airspeed de Mode S BDS 5.0)
                    var tas = "N/A";
                    if (record.ModeSData?.MBDataBlocks != null)
                    {
                        var bds50 = record.ModeSData.MBDataBlocks.FirstOrDefault(b => b.BDSRegister == 0x50);
                        if (bds50 != null && bds50.TrueAirspeed > 0)
                            tas = $"{bds50.TrueAirspeed:F0}";
                    }
                    Console.Write($"{tas,-10} | ");

                    // HDG (Heading)
                    var hdg = record.HeadingDegrees > 0 ? $"{record.HeadingDegrees:F1}" : "N/A";
                    Console.Write($"{hdg,-10} | ");

                    // IAS (Indicated Airspeed de Mode S BDS 6.0)
                    var ias = "N/A";
                    if (record.ModeSData?.MBDataBlocks != null)
                    {
                        var bds60 = record.ModeSData.MBDataBlocks.FirstOrDefault(b => b.BDSRegister == 0x60);
                        if (bds60 != null && bds60.IndicatedAirspeed > 0)
                            ias = $"{bds60.IndicatedAirspeed:F0}";
                    }
                    Console.Write($"{ias,-10} | ");

                    // MACH (de Mode S BDS 6.0)
                    var mach = "N/A";
                    if (record.ModeSData?.MBDataBlocks != null)
                    {
                        var bds60 = record.ModeSData.MBDataBlocks.FirstOrDefault(b => b.BDSRegister == 0x60);
                        if (bds60 != null && bds60.MachNumber > 0)
                            mach = $"{bds60.MachNumber:F3}";
                    }
                    Console.Write($"{mach,-10} | ");

                    // BAR (Barometric Altitude Rate de Mode S BDS 6.0 con fallback a Radial Doppler)
                    var bar = "N/A";
                    if (record.ModeSData?.MBDataBlocks != null)
                    {
                        var bds60 = record.ModeSData.MBDataBlocks.FirstOrDefault(b => b.BDSRegister == 0x60);
                        if (bds60 != null && bds60.BarometricAltitudeRate != 0)
                            bar = $"{bds60.BarometricAltitudeRate:F1}";
                    }
                    //if (bar == "N/A" && record.RadialDopplerSpeed != 0)
                    //bar = $"{record.RadialDopplerSpeed:F1}";
                    Console.Write($"{bar,-10} | ");

                    // IVV (Inertial Vertical Velocity de Mode S BDS 6.0)
                    var ivv = "N/A";
                    if (record.ModeSData?.MBDataBlocks != null)
                    {
                        var bds60 = record.ModeSData.MBDataBlocks.FirstOrDefault(b => b.BDSRegister == 0x60);
                        if (bds60 != null && bds60.InertialVerticalVelocity != 0)
                            ivv = $"{bds60.InertialVerticalVelocity:F0}";
                    }
                    Console.Write($"{ivv,-10} | ");

                    // TN (Track Number)
                    var tn = record.TrackNumber > 0 ? $"{record.TrackNumber}" : "N/A";
                    Console.Write($"{tn,-10} | ");

                    // GSSD (Ground Speed from Mode S)
                    var gssd = "N/A";
                    if (record.ModeSData?.MBDataBlocks != null)
                    {
                        var bds50 = record.ModeSData.MBDataBlocks.FirstOrDefault(b => b.BDSRegister == 0x50);
                        if (bds50 != null && bds50.GroundSpeed > 0)
                            gssd = $"{bds50.GroundSpeed:F0}";
                    }
                    Console.Write($"{gssd,-10} | ");

                    // HDG2 (Magnetic Heading de Mode S BDS 6.0)
                    var hdg2 = "N/A";
                    if (record.ModeSData?.MBDataBlocks != null)
                    {
                        var bds60 = record.ModeSData.MBDataBlocks.FirstOrDefault(b => b.BDSRegister == 0x60);
                        if (bds60 != null && bds60.MagneticHeading > 0)
                            hdg2 = $"{bds60.MagneticHeading:F1}";
                    }
                    Console.Write($"{hdg2,-10} | ");

                    // Stat (Status/Type de la imagen)
                    var stat = record.TargetReportDescriptor ?? "N/A";
                    if (stat.Length > 10) stat = stat.Substring(0, 10);
                    Console.Write($"{stat,-10} | ");

                    // AD (Aircraft Address)
                    Console.Write($"{record.AircraftAddress ?? "N/A",-10} | ");

                    Console.WriteLine();
                }
            }
        }
    }
}

    //string filePath = "datos_asterix_combinado.ast"; // Canvia-ho pel teu fitxer ASTERIX
    //var reader = new BinaryFileReader(filePath);
    //var messages = reader.ReadMessages();

    //Console.WriteLine($"S'han llegit {messages.Count} missatges del fitxer.\n");

    //        for (int i = 0; i<Math.Min(20, messages.Count); i++)
    //        {
    //            var msg = messages[i];
    //byte category = msg[0];
    //int length = (msg[1] << 8) | msg[2];

    //var decoder = new Cat048Decoder(msg);
    //var records = decoder.Decode();

    //            foreach (var record in records)
    //            {
    //                Console.WriteLine(
    //                    $"Msg {i + 1}: " +
    //                    $"Cat={category}, Longitud={length}, " +
    //                    $"{record.DataSourceIdentifier}, " +
    //                    $"TimeOfDay={record.TimeOfDay}, " +
    //                    $"Rho={record.RhoSlantRange}, " +
    //                    $"Theta={record.ThetaAzimuth}, " +
    //                    $"Mode3A={record.Mode3ACode}, " +
    //                    $"Id={record.AircraftIdentification} " 

    //                );
    //            }
    //        }
