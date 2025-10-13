using MultiCAT6.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AsterixDecoder.Models.CAT048
{
    /// <summary>
    /// Clase que representa un registro CAT048 procesado con todos sus campos calculados
    /// </summary>
    public class Cat048
    {
        // Atributos principales
        public string CAT { get; set; }
        public int SAC { get; set; }
        public int SIC { get; set; }
        public string Time { get; set; }
        public double? LAT { get; set; }
        public double? LON { get; set; }
        public double? H { get; set; }          // Altitud corregida con QNH (feet)
        public double? H_m { get; set; }        // Altura en metros
        public double RHO { get; set; }
        public double THETA { get; set; }
        public string Mode3A { get; set; }
        public double? FL { get; set; }
        public string TA { get; set; }         // Target Address (Aircraft Address)
        public string TI { get; set; }          // Target ID (Aircraft Identification)
        public double? BP { get; set; }         // Barometric Pressure (hPa)
        public string RA { get; set; }          // Receiving Antenna / RAB Indicator
        public double? RollAngle { get; set; }  // Roll Angle (BDS 5.0)
        public string ITA { get; set; }         // Reserved for future use
        public double? GS { get; set; }         // Ground Speed from velocity (kt)
        public double? TAR { get; set; }        // Track Angle Rate (deg/s)
        public double? TAS { get; set; }        // True Airspeed (kt)
        public double? HDG { get; set; }        // Heading from velocity -180 to 180
        public double? IAS { get; set; }        // Indicated Airspeed (kt)
        public double? MACH { get; set; }       // Mach Number
        public double? BAR { get; set; }        // Barometric Altitude Rate (ft/min)
        public double? IVV { get; set; }        // Inertial Vertical Velocity (ft/min)
        public int? TN { get; set; }            // Track Number
        public double? GSSD { get; set; }       // Ground Speed from BDS 5.0 (kt)
        public double? HDG2 { get; set; }       // True Heading 0-360 from BDS 6.0
        public string Stat { get; set; }

        // Constantes
        private const double QNH_STANDARD = 1013.25; // hPa
        private const double TRANSITION_ALTITUDE = 6000.0; // feet

        /// <summary>
        /// Constructor que crea un objeto Cat048 a partir de datos decodificados
        /// </summary>
        public Cat048(RawCat048Data rawData, GeoUtils geoUtils, CoordinatesWGS84 radarPosition)
        {
            // Inicializar campos básicos
            CAT = "CAT048";
            SAC = rawData.SAC;
            SIC = rawData.SIC;

            // Procesar tiempo
            if (rawData.TimeOfDay.HasValue)
            {
                var totalHours = (int)rawData.TimeOfDay.Value.TotalHours;
                var minutes = rawData.TimeOfDay.Value.Minutes;
                var seconds = rawData.TimeOfDay.Value.Seconds;
                var millis = rawData.TimeOfDay.Value.Milliseconds;
                Time = $"{totalHours:D2}:{minutes:D2}:{seconds:D2}.{millis:D3}";
            }
            else
            {
                Time = "N/A";
            }

            // Coordenadas polares
            RHO = rawData.RhoSlantRange;
            THETA = rawData.ThetaAzimuth;

            // Convertir coordenadas polares a WGS84
            ConvertPolarToWGS84(rawData, geoUtils, radarPosition);

            // Mode 3/A
            Mode3A = rawData.Mode3ACode ?? "N/A";

            // Flight Level y altitud corregida
            ProcessFlightLevel(rawData);

            // Calcular Ground Speed y Heading desde velocidades cartesianas
            if (rawData.VxRaw != 0 || rawData.VyRaw != 0)
            {
                double vxKt = rawData.VxRaw * (1.0 / 16384.0) * 3600.0;
                double vyKt = rawData.VyRaw * (1.0 / 16384.0) * 3600.0;

                GS = Math.Sqrt(vxKt * vxKt + vyKt * vyKt);
                
                // Heading -180 a 180
                HDG = Math.Atan2(vxKt, vyKt) * 180.0 / Math.PI;
            }


            // Status
            Stat = rawData.TargetReportDescriptor ?? "N/A";

            // Target Address (TA) - Aircraft Address
            TA = rawData.AircraftAddress ?? "N/A";

            // Target ID (TI) - Aircraft Identification
            TI = rawData.AircraftIdentification ?? "N/A";

            // Receiving Antenna (RA) - RAB indicator
            RA = rawData.RABPresent ? "RAB:1" : "RAB:0";

            // Procesar Mode S Data
            ProcessModeSData(rawData);

            // BAR fallback a Radial Doppler si no hay Mode S
            if (!BAR.HasValue && rawData.RadialDopplerSpeed != 0)
            {
                BAR = rawData.RadialDopplerSpeed * 196.85; // m/s to ft/min
            }

            // ITA - Reserved
            ITA = "N/A";
        }

        /// <summary>
        /// Convierte coordenadas polares del radar a WGS84
        /// </summary>
        private void ConvertPolarToWGS84(RawCat048Data rawData, GeoUtils geoUtils, CoordinatesWGS84 radarPosition)
        {
            if (rawData.RhoSlantRange <= 0 || rawData.ThetaAzimuth < 0)
                return;

            try
            {
                double rhoMeters = rawData.RhoSlantRange * GeoUtils.NM2METERS;
                double thetaRadians = rawData.ThetaAzimuth * GeoUtils.DEGS2RADS;

                double elevation = 0;
                if (rawData.Height3DRadar > 0)
                {
                    double heightMeters = rawData.Height3DRadar * GeoUtils.FEET2METERS;
                    H_m = heightMeters;
                    elevation = GeoUtils.CalculateElevation(radarPosition, geoUtils.R_S, rhoMeters, heightMeters);
                }

                CoordinatesPolar polar = new CoordinatesPolar(rhoMeters, thetaRadians, elevation);
                CoordinatesXYZ radarCart = GeoUtils.change_radar_spherical2radar_cartesian(polar);
                CoordinatesXYZ geocentric = geoUtils.change_radar_cartesian2geocentric(radarPosition, radarCart);
                CoordinatesWGS84 wgs84 = geoUtils.change_geocentric2geodesic(geocentric);

                if (wgs84 != null)
                {
                    LAT = wgs84.Lat * GeoUtils.RADS2DEGS;
                    LON = wgs84.Lon * GeoUtils.RADS2DEGS;
                    // H se calculará con corrección QNH más adelante
                }
            }
            catch (Exception)
            {
                // Si falla la conversión, los campos quedan null
            }
        }

        /// <summary>
        /// Procesa el Flight Level y aplica corrección QNH cuando corresponde
        /// </summary>
        private void ProcessFlightLevel(RawCat048Data rawData)
        {
            if (rawData.FlightLevel == 0)
                return;

            // Calcular Flight Level en formato estándar
            FL = rawData.FlightLevel / 4.0;
            
            // Altitud sin corregir en pies
            double altitudeFeet = FL.Value * 100.0;

            // Obtener QNH de Mode S BDS 4.0 si está disponible
            double? qnhCurrent = null;
            var bds40 = rawData.ModeSDataBlocks?.FirstOrDefault(b => b.BDSRegister == 0x40);
            if (bds40 != null && bds40.BarometricPressureSetting > 0)
            {
                qnhCurrent = bds40.BarometricPressureSetting;
                BP = qnhCurrent; // Guardar BP
            }

            // Aplicar corrección QNH si la aeronave está por debajo de altitud de transición
            if (altitudeFeet < TRANSITION_ALTITUDE && qnhCurrent.HasValue)
            {
                // Fórmula: Altitud real = Altitud indicada + (QNH actual - QNH estándar) × 30 ft
                H = altitudeFeet + (qnhCurrent.Value - QNH_STANDARD) * 30.0;
            }
            else
            {
                // Por encima de altitud de transición o sin QNH, usar altitud sin corregir
                H = altitudeFeet;
            }
        }

        /// <summary>
        /// Procesa datos de Mode S BDS
        /// </summary>
        private void ProcessModeSData(RawCat048Data rawData)
        {
            if (rawData.ModeSDataBlocks == null || rawData.ModeSDataBlocks.Count == 0)
                return;

            // BDS 4.0 - Selected Vertical Intention
            var bds40 = rawData.ModeSDataBlocks.FirstOrDefault(b => b.BDSRegister == 0x40);
            if (bds40 != null)
            {
                if (bds40.BarometricPressureSetting > 0)
                    BP = bds40.BarometricPressureSetting;
            }

            // BDS 5.0 - Track and Turn Report
            var bds50 = rawData.ModeSDataBlocks.FirstOrDefault(b => b.BDSRegister == 0x50);
            if (bds50 != null)
            {
                if (bds50.RollAngle != 0)
                    RollAngle = bds50.RollAngle;

                if (bds50.TrackAngleRate != 0)
                    TAR = bds50.TrackAngleRate;

                if (bds50.TrueAirspeed > 0)
                    TAS = bds50.TrueAirspeed;

                if (bds50.GroundSpeed > 0)
                    GSSD = bds50.GroundSpeed;
            }

            // BDS 6.0 - Heading and Speed Report
            var bds60 = rawData.ModeSDataBlocks.FirstOrDefault(b => b.BDSRegister == 0x60);
            if (bds60 != null)
            {
                if (bds60.MagneticHeading > 0)
                    HDG2 = bds60.MagneticHeading;

                if (bds60.IndicatedAirspeed > 0)
                    IAS = bds60.IndicatedAirspeed;

                if (bds60.MachNumber > 0)
                    MACH = bds60.MachNumber;

                if (bds60.BarometricAltitudeRate != 0)
                    BAR = bds60.BarometricAltitudeRate;

                if (bds60.InertialVerticalVelocity != 0)
                    IVV = bds60.InertialVerticalVelocity;
            }
        }

        /// <summary>
        /// Devuelve una representación en string del objeto
        /// </summary>
        public override string ToString()
        {
            return $"CAT048 [SAC:{SAC} SIC:{SIC}] Time:{Time} LAT:{LAT:F6} LON:{LON:F6} FL:{FL} TA:{TA} TI:{TI}";
        }
    }
}
