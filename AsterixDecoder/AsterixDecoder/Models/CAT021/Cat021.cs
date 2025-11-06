using System;
using System.Globalization;

namespace AsterixDecoder.Models.CAT021
{
    /// <summary>
    /// Clase que representa un registro CAT021 procesado con todos sus campos calculados
    /// </summary>
    public class Cat021
    {
        // Atributos principales
        public string CAT { get; set; }
        public int SAC { get; set; }
        public int SIC { get; set; }
        public string Time { get; set; }
        public double LAT { get; set; }
        public double LON { get; set; }
        public string Mode3A { get; set; }
        public double FL { get; set; }
        public string TA { get; set; }
        public string TI { get; set; }
        public double? BP { get; set; }
        public double Real_Altitude_ft { get; set; }
        public bool IsOnGround { get; set; }

        // Configuración QNH para corrección de altitud
        private const double DEFAULT_QNH = 1013.25;

        /// <summary>
        /// Constructor que crea un objeto Cat021 a partir de datos decodificados
        /// </summary>
        public Cat021(RawCat021Data rawData, double actualQNH = DEFAULT_QNH)
        {
            CAT = "CAT021";
            SAC = rawData.SAC;
            SIC = rawData.SIC;

            // Procesar tiempo
            if (rawData.Time_Reception_Position.HasValue)
            {
                var time = rawData.Time_Reception_Position.Value;
                Time = $"{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}.{time.Milliseconds:D3}";
            }
            else
            {
                Time = "N/A";
            }

            // Coordenadas WGS84
            LAT = rawData.WGS84_Latitude;
            LON = rawData.WGS84_Longitude;

            // Mode 3/A
            Mode3A = rawData.Mode3A_Code ?? "N/A";

            // Flight Level y altitud corregida
            ProcessFlightLevel(rawData, actualQNH);

            // Target Address
            TA = rawData.Target_Address ?? "N/A";

            // Target Identification
            TI = rawData.Target_Identification ?? "N/A";

            // Barometric Pressure
            BP = BP = rawData.BarometricPressureSetting;
            Console.WriteLine($"Debug: BP={BP}"); 

            // Determinar si está en tierra (ATP: 0=unknown, 1=ADS-B, 2=ADS-B+ES, 3=ADS-B+ES+extended)
            // Típicamente ATP=1,2,3 son airborne, pero también se puede inferir de otros campos
            IsOnGround = DetermineGroundStatus(rawData);
        }

        /// <summary>
        /// Procesa el Flight Level y calcula la altitud real con corrección QNH
        /// </summary>
        // Add this at class scope (in your decoder/processor class)
        private double? lastKnownBP; // remember last non-standard BP until 6000 ft

        // Updated ProcessFlightLevel implementation
        private void ProcessFlightLevel(RawCat021Data rawData, double actualQNH)
        {
            // If no raw FL value, zero out
            if (rawData.FlightLevel_Raw <= 0)
            {
                FL = 0;
                Real_Altitude_ft = 0;
                return;
            }

            // rawData.FlightLevel_Raw is the raw "counts" (rawFl).
            // Mode-C altitude in feet = rawFl * 25 ft
            double modeC_alt_ft = rawData.FlightLevel_Raw * 25.0;

            // Flight Level number (for display when above TA) = rawFl / 4
            double flightLevelNumber = rawData.FlightLevel_Raw / 4.0;

            // Update lastKnownBP when a non-standard BPS is announced
            // (store only non-1013.25 values, because spec says keep previous non-STD until 6000ft)
            if (rawData.BarometricPressureSetting.HasValue && Math.Abs(rawData.BarometricPressureSetting.Value - 1013.25) > 0.001)
            {
                lastKnownBP = rawData.BarometricPressureSetting.Value;
            }

            // Determine whether we are below TA (6000 ft)
            bool belowTA = modeC_alt_ft < 6000.0;

            if (belowTA)
            {
                // Choose QNH to use for correction:
                // - prefer explicit BPS if present and not STD
                // - if BPS is STD but we have lastKnownBP and we are below TA, use lastKnownBP
                // - otherwise fallback to actualQNH (passed into function)
                double qnhToUse;
                if (rawData.BarometricPressureSetting.HasValue && Math.Abs(rawData.BarometricPressureSetting.Value - 1013.25) > 0.001)
                {
                    qnhToUse = rawData.BarometricPressureSetting.Value;
                }
                else if (rawData.BarometricPressureSetting.HasValue && Math.Abs(rawData.BarometricPressureSetting.Value - 1013.25) <= 0.001
                         && lastKnownBP.HasValue)
                {
                    // Aircraft changed to STD prematurely -> keep previous non-STD BP until 6000ft
                    qnhToUse = lastKnownBP.Value;
                }
                else if (lastKnownBP.HasValue)
                {
                    // No new BPS, but we have a remembered one (use it until 6000ft)
                    qnhToUse = lastKnownBP.Value;
                }
                else
                {
                    // No BPS seen -> use provided actualQNH (station QNH)
                    qnhToUse = actualQNH;
                }

                // Correct the mode-C altitude referenced to standard QNH (1013.25 -> Mode C baseline)
                Real_Altitude_ft = modeC_alt_ft + (qnhToUse - 1013.25) * 30.0;

                // For display, when below TA we want the "FL" field to show *feet* (per your requirement)
                // e.g. a raw modeC_alt_ft == 26 -> show FL = 26 (not FL=0 or FL=2600)
                // Use rounded integer feet for FL property
                FL = (int)Math.Round(modeC_alt_ft);
            }
            else
            {
                // At or above TA -> Mode C is a flight level referenced to STD: show FL and real altitude = FL*100
                FL = (int)Math.Round(flightLevelNumber); // e.g., rawFl/4 => FL number (60, 100, 340 ...)
                Real_Altitude_ft = FL * 100.0;
            }
        }



        /// <summary>
        /// Determina si el target está en tierra basándose en los datos disponibles
        /// </summary>
        private bool DetermineGroundStatus(RawCat021Data rawData)
        {
            // ATP (Address Type): valores típicos
            // 0 = unknown
            // 1 = ADS-B with ICAO 24-bit address
            // 2 = ADS-B with surface vehicle address
            // 3 = ADS-B with anonymous address

            // Si ATP indica surface vehicle, está en tierra
            if (rawData.ATP == 2)
                return true;

            // Si la altitud es muy baja (< 100 ft) y velocidad baja, probablemente en tierra
            if (Real_Altitude_ft < 100 && Real_Altitude_ft >= 0)
                return true;

            return false;
        }

        /// <summary>
        /// Verifica si el registro cumple con los filtros del FIR de Barcelona
        /// </summary>
        public bool IsWithinBarcelonaFIR()
        {
            return LAT > 40.9 && LAT < 41.7 && LON > 1.5 && LON < 2.6;
        }

        /// <summary>
        /// Verifica si el registro es válido (airborne y dentro del FIR)
        /// </summary>
        public bool IsValid()
        {
            return !IsOnGround && IsWithinBarcelonaFIR();
        }

        /// <summary>
        /// Devuelve una representación en string del objeto
        /// </summary>
        public override string ToString()
        {
            return $"CAT021 [SAC:{SAC} SIC:{SIC}] Time:{Time} LAT:{LAT:F6} LON:{LON:F6} FL:{FL} TA:{TA} TI:{TI}";
        }
    }
}
