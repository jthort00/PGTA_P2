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
        public double ModeC_Corrected { get; set; }
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
            //Console.WriteLine($"Debug: BP={BP}"); 

            // Determinar si está en tierra (ATP: 0=unknown, 1=ADS-B, 2=ADS-B+ES, 3=ADS-B+ES+extended)
            // Típicamente ATP=1,2,3 son airborne, pero también se puede inferir de otros campos
            IsOnGround = DetermineGroundStatus(rawData);
        }

        /// <summary>
        /// Procesa el Flight Level y calcula la altitud real con corrección QNH
        /// </summary>
        // Add this at class scope (in your decoder/processor class)
        public double? lastKnownBP; // remember last non-standard BP until 6000 ft

        // Updated ProcessFlightLevel implementation
        

        public void ProcessFlightLevel(RawCat021Data rawData, double actualQNH)
        {
            if (rawData.FlightLevel_Raw <= 0)
            {
                FL = 0;
                ModeC_Corrected = 0;
                if (!lastKnownBP.HasValue)
                    BP = actualQNH; // fallback
                else
                    BP = lastKnownBP; // keep previous
                return;
            }

            // Convert raw counts to Mode-C altitude
            double modeC_alt_ft = rawData.FlightLevel_Raw * 0.25;
            
            if (modeC_alt_ft < -2.0)
                modeC_alt_ft = 0.0;

            FL = modeC_alt_ft;
            
            // Process Barometric Pressure Setting (BP)
            if (rawData.BarometricPressureSetting.HasValue &&
                rawData.BarometricPressureSetting.Value >= 1000.0 &&
                rawData.BarometricPressureSetting.Value <= 1030.0)
            {
                BP = rawData.BarometricPressureSetting.Value;
                lastKnownBP = BP;
            }
            else
            {
                BP = null;
            }

            double qnhToUse = actualQNH;
            if (BP.HasValue)
                qnhToUse = BP.Value;
            else if (lastKnownBP.HasValue)
                qnhToUse = lastKnownBP.Value;

            // 5️⃣ Compute Mode-C corrected altitude
            if (FL < 60.0)
            {
                // Below TA -> apply QNH correction
                ModeC_Corrected = (100.0 * FL) + (qnhToUse - 1013.25) * 30.0;
            }
            else
            {
                // Above TA -> standard pressure
                ModeC_Corrected = 100.0 * FL;
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
            if ((rawData.FlightLevel_Raw*0.25) <= 25.25)
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
