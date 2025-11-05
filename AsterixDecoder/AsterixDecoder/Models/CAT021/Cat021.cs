using System;

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
        public int FL { get; set; }
        public string TA { get; set; }
        public string TI { get; set; }
        public string BP { get; set; }
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
            BP = rawData.TargetReportDescriptor ?? "N/A";

            // Determinar si está en tierra (ATP: 0=unknown, 1=ADS-B, 2=ADS-B+ES, 3=ADS-B+ES+extended)
            // Típicamente ATP=1,2,3 son airborne, pero también se puede inferir de otros campos
            IsOnGround = DetermineGroundStatus(rawData);
        }

        /// <summary>
        /// Procesa el Flight Level y calcula la altitud real con corrección QNH
        /// </summary>
        private void ProcessFlightLevel(RawCat021Data rawData, double actualQNH)
        {
            if (rawData.FlightLevel_Raw > 0)
            {
                // Convertir raw FL a Flight Level (1 FL = 25 ft, raw/4 = FL)
                FL = rawData.FlightLevel_Raw / 4;

                // Altitud indicada en pies
                double indicatedAltitude = FL * 100.0;

                // Determinar si está por debajo de la Transition Altitude (6000 ft)
                bool isBelowTA = indicatedAltitude < 6000;

                // Determinar qué fuente barométrica usar
                bool useQNH = rawData.BarometricPressureSource.HasValue
                    ? rawData.BarometricPressureSource.Value
                    : isBelowTA; // fallback si BPS no está disponible

                // Aplicar corrección QNH si corresponde
                if (useQNH)
                {
                    Real_Altitude_ft = indicatedAltitude + (actualQNH - 1013.25) * 30.0;
                }
                else
                {
                    Real_Altitude_ft = indicatedAltitude;
                }
            }
            else
            {
                FL = 0;
                Real_Altitude_ft = 0;
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
