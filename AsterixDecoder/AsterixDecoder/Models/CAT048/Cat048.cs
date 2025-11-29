using MultiCAT6.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AsterixDecoder.Models.CAT048
{
    public class Cat048
    {
        // Atributos principales 
        public string CAT { get; set; }
        public int? SAC { get; set; }
        public int? SIC { get; set; }
        public string Time { get; set; }
        public double? LAT { get; set; }
        public double? LON { get; set; }
        public double? H { get; set; }          
        public double? H_m { get; set; }        
        public double? RHO { get; set; }
        public double? THETA { get; set; }
        public string Mode3A { get; set; }
        public double? FL { get; set; }
        public string TA { get; set; }         
        public string TI { get; set; }
        public string Stat { get; set; }          
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
        public int? TN { get; set; }            
        public double? GSSD { get; set; }       
        public double? HDG2 { get; set; }       
        
        // Atributo adicional: I048/020.typ (0..7)
        public int? TypDesc { get; set; }

        public bool? RABPresent { get; set; }

        // Campos de I048/230 - Communications/ACAS Capability
        public int? COM { get; set; }
        public int? STAT_230 { get; set; }
        public bool? SI { get; set; }
        public bool? MSSC { get; set; }
        public bool? ARC { get; set; }
        public bool? AIC { get; set; }

        // Constantes
        private const double QNH_STANDARD = 1013.25; // hPa
        private const double TRANSITION_ALTITUDE = 6000.0; // feet

        // Estado de QNH/BP para corrección bajo TA
        private double? lastKnownBP; // último QNH válido (1000–1030 hPa) visto por debajo de TA
        private readonly double actualQNH; // QNH de entorno (fallback)

        public Cat048(RawCat048Data rawData, GeoUtils geoUtils, CoordinatesWGS84 radarPosition, double actualQNH = QNH_STANDARD)
        {
            this.actualQNH = actualQNH;
            // Inicializar campos básicos
            CAT = "CAT048";
            SAC = rawData.SAC > 0 ? rawData.SAC : (int?)null;
            SIC = rawData.SIC > 0 ? rawData.SIC : (int?)null;

            // Procesar tiempo
            if (rawData.TimeOfDay.HasValue)
            {
                var totalHours = (int)rawData.TimeOfDay.Value.TotalHours;
                var minutes = rawData.TimeOfDay.Value.Minutes;
                var seconds = rawData.TimeOfDay.Value.Seconds;
                var millis = rawData.TimeOfDay.Value.Milliseconds;
                Time = $"{totalHours:D2}:{minutes:D2}:{seconds:D2}:{millis:D3}";
            }
            else
            {
                Time = null;
            }

            // Coordenadas polares - solo si son válidas
            if (rawData.RhoSlantRange > 0)
            {
                RHO = rawData.RhoSlantRange;
            }
            
            if (rawData.ThetaAzimuth >= 0)
            {
                THETA = rawData.ThetaAzimuth;
            }

            // Convertir coordenadas polares a WGS84
            ConvertPolarToWGS84(rawData, geoUtils, radarPosition);

            // Mode 3/A
            Mode3A = string.IsNullOrWhiteSpace(rawData.Mode3ACode) ? null : rawData.Mode3ACode;

            // Flight Level y altitud corregida
            ProcessFlightLevel(rawData);

            // Procesar Mode S Data
            ProcessModeSData(rawData);

            if (rawData.VxRaw != 0 || rawData.VyRaw != 0)
            {                
                GSSD = rawData.VxRaw != 0 ? rawData.VxRaw : (double?)null;
                HDG2 = rawData.VyRaw != 0 ? rawData.VyRaw : (double?)null;
            }

            // Status
            Stat = string.IsNullOrWhiteSpace(rawData.TargetReportDescriptor) ? null : rawData.TargetReportDescriptor;

            TypDesc = rawData.TargetType;

            // RAB (I048/020) - indicador de transponder fijo
            RABPresent = rawData.RABPresent;

            // Target Address (TA) - Aircraft Address
            TA = string.IsNullOrWhiteSpace(rawData.AircraftAddress) ? null : rawData.AircraftAddress;

            // Target ID (TI) - Aircraft Identification
            TI = string.IsNullOrWhiteSpace(rawData.AircraftIdentification) ? null : rawData.AircraftIdentification;

            // Track Number
            if (rawData.TrackNumber > 0)
            {
                TN = rawData.TrackNumber;
            }

            // Procesar I048/230 - Communications/ACAS Capability
            ProcessCommunicationsCapability(rawData);
        }

        private void ConvertPolarToWGS84(RawCat048Data rawData, GeoUtils geoUtils, CoordinatesWGS84 radarPosition)
        {
            if (rawData.RhoSlantRange <= 0 || rawData.ThetaAzimuth < 0)
                return;

            try
            {
                double rhoMeters = rawData.RhoSlantRange * GeoUtils.NM2METERS;
                double thetaRadians = rawData.ThetaAzimuth * GeoUtils.DEGS2RADS;

                // Usar elevación 0 según especificación actual
                double elevation = 0;

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

        private void ProcessFlightLevel(RawCat048Data rawData)
        {
            // Si el campo I048/090 no está presente en el mensaje, dejar FL/H como null
            if (!rawData.HasFlightLevel)
            {
                FL = null;
                H = null;
                H_m = null;
                // Mantener último BP válido si existiera; si no, usar QNH actual como referencia
                if (!lastKnownBP.HasValue)
                    BP = actualQNH;
                else
                    BP = lastKnownBP;
                return;
            }

            // FL en centenas de pies (raw en cuartos de FL). FL puede ser 0 y es válido (nivel de vuelo 0)
            FL = rawData.FlightLevel / 4.0;
            double fl = FL ?? 0.0;
            double altitudeFeet = fl * 100.0;

            // Obtener QNH desde BDS 4.0 si está en rango válido (1000–1030 hPa)
            var bds40 = rawData.ModeSDataBlocks?.FirstOrDefault(b => b.BDSRegister == 0x40);
            if (bds40 != null)
            {
                if (bds40.BarometricPressureSetting >= 1000.0 && bds40.BarometricPressureSetting <= 1030.0)
                {
                    BP = bds40.BarometricPressureSetting;
                    lastKnownBP = BP; // recordar el último BP válido
                }
                else
                {
                    BP = null; // presente pero fuera de rango
                }
            }
            else
            {
                BP = null; // no vino en este mensaje
            }

            // Seleccionar QNH a usar: BP -> lastKnownBP -> actualQNH
            double qnhToUse = actualQNH;
            if (BP.HasValue)
                qnhToUse = BP.Value;
            else if (lastKnownBP.HasValue)
                qnhToUse = lastKnownBP.Value;

            // Calcular altitud corregida solo por debajo de 6000 ft (FL < 60)
            if (fl < 60.0)
            {
                H = altitudeFeet + (qnhToUse - QNH_STANDARD) * 30.0;
                if (H < 0) H = 0; // evitar negativos por diferencias de QNH
            }
            else
            {
                H = altitudeFeet; // por encima de TA usar presión estándar
            }

            H_m = H.HasValue ? H.Value * GeoUtils.FEET2METERS : (double?)null;
        }

        private void ProcessCommunicationsCapability(RawCat048Data rawData)
        {
            // I048/230 - Communications/ACAS Capability and Flight Status
            if (rawData.COM >= 0)
            {
                COM = rawData.COM;
            }

            if (rawData.STAT >= 0)
            {
                STAT_230 = rawData.STAT;
            }

            SI = rawData.SI;
            MSSC = rawData.MSSC;
            ARC = rawData.ARC;
            AIC = rawData.AIC;
        }

        private void ProcessModeSData(RawCat048Data rawData)
        {
            if (rawData.ModeSDataBlocks == null || rawData.ModeSDataBlocks.Count == 0)
                return;

            // BDS 4.0 - Selected Vertical Intention
            var bds40 = rawData.ModeSDataBlocks.FirstOrDefault(b => b.BDSRegister == 0x40);
            if (bds40 != null && bds40.BarometricPressureSetting > 0)
            {
                BP = bds40.BarometricPressureSetting;
            }

            // BDS 5.0 - Track and Turn Report
            var bds50 = rawData.ModeSDataBlocks.FirstOrDefault(b => b.BDSRegister == 0x50);
            if (bds50 != null)
            {
                // Roll Angle como nullable
                if (bds50.RollAngle != 0)
                {
                    RA = -90 + Math.Abs(bds50.RollAngle);
                }

                if (bds50.TrackAngleRate != 0)
                {
                    TAR = bds50.TrackAngleRate;
                }

                // TAS: 0 no es válido, debe ser > 0
                if (bds50.TrueAirspeed > 0)
                    TAS = bds50.TrueAirspeed;

                // GS desde BDS 5.0: 0 es válido
                if (bds50.GroundSpeed >= 0)
                    GS = bds50.GroundSpeed;

                if (bds50.TrueTrackAngle != 0)
                    TTA = bds50.TrueTrackAngle;
            }

            // BDS 6.0 - Heading and Speed Report
            var bds60 = rawData.ModeSDataBlocks.FirstOrDefault(b => b.BDSRegister == 0x60);
            if (bds60 != null)
            {
                // Magnetic Heading: 0-360° es válido (norte magnético = 0°)
                // LSB = 90/512 = 0.17578125° según BDS 6.0
                if (bds60.MagneticHeading >= 0)
                    HDG = bds60.MagneticHeading;

                // IAS: 0 no es válido, debe ser > 0
                if (bds60.IndicatedAirspeed > 0)
                    IAS = bds60.IndicatedAirspeed;

                // Mach: 0 no es válido
                if (bds60.MachNumber > 0)
                    MACH = bds60.MachNumber;
                
                // BAR e IVV pueden ser negativos (descenso), 0 o positivos
                if (bds60.BarometricAltitudeRate != 0)
                    BAR = bds60.BarometricAltitudeRate;
                
                if (bds60.InertialVerticalVelocity != 0)
                    IVV = bds60.InertialVerticalVelocity;
            }
        }

        /// <summary>
        /// Verifica si el registro CAT048 se encuentra dentro del filtro geográfico de Barcelona FIR
        /// 40.9º N < lat < 41.7º N y 1.5º E < lon < 2.6º E
        /// </summary>
        public bool IsWithinBarcelonaFIR()
        {
            if (!LAT.HasValue || !LON.HasValue) return false;
            return LAT.Value > 40.9 && LAT.Value < 41.7 && LON.Value > 1.5 && LON.Value < 2.6;
        }

        public override string ToString()
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            string sac = SAC.HasValue ? SAC.Value.ToString(inv) : "N/A";
            string sic = SIC.HasValue ? SIC.Value.ToString(inv) : "N/A";
            string lat = LAT.HasValue ? LAT.Value.ToString("F6", inv) : "N/A";
            string lon = LON.HasValue ? LON.Value.ToString("F6", inv) : "N/A";
            string fl = FL.HasValue ? FL.Value.ToString(inv) : "N/A";
            string ta = string.IsNullOrWhiteSpace(TA) ? "N/A" : TA;
            string ti = string.IsNullOrWhiteSpace(TI) ? "N/A" : TI;
            string time = string.IsNullOrWhiteSpace(Time) ? "N/A" : Time;
            return $"CAT048 [SAC:{sac} SIC:{sic}] Time:{time} LAT:{lat} LON:{lon} FL:{fl} TA:{ta} TI:{ti}";
        }
    }
}
