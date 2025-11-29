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
        public double? ModeC_Corrected { get; set; }
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
                Time = $"{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}:{time.Milliseconds:D3}";
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
            BP = rawData.BarometricPressureSetting;
            //Console.WriteLine($"Debug: BP={BP}"); 

            // Determinar si está en tierra (ATP: 0=unknown, 1=ADS-B, 2=ADS-B+ES, 3=ADS-B+ES+extended)
            // Típicamente ATP=1,2,3 son airborne, pero también se puede inferir de otros campos
            IsOnGround = DetermineGroundStatus(rawData);
        }

        /// <summary>
        /// Procesa el Flight Level y calcula la altitud real con corrección QNH
        /// </summary>
        // Añadir esto a nivel de clase (en tu decodificador/procesador)
        public double? lastKnownBP; // recordar el último BP no estándar hasta 6000 ft

        // Implementación actualizada de ProcessFlightLevel
        

        public void ProcessFlightLevel(RawCat021Data rawData, double actualQNH)
        {
            if (rawData.FlightLevel_Raw <= 0)
            {
                FL = 0;
                ModeC_Corrected = 0;
                if (!lastKnownBP.HasValue)
                    BP = actualQNH; // valor por defecto (alternativo)
                else
                    BP = lastKnownBP; // mantener el anterior
                return;
            }

            // Convertir cuentas crudas a altitud Mode-C
            double modeC_alt_ft = rawData.FlightLevel_Raw * 0.25;
            
            if (rawData.GBS || rawData.ATP == 2)
            {
                if (modeC_alt_ft > 150.0)
                    modeC_alt_ft = 0.0;
            }
            
            if (modeC_alt_ft < -2.0 || modeC_alt_ft > 200.0)
                modeC_alt_ft = 0.0;

            FL = modeC_alt_ft;
            
            // Procesar el ajuste de presión barométrica (BP)
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

            if (FL < 60.0)
            {
                // Por debajo de la TA -> aplicar corrección QNH
                ModeC_Corrected = (100.0 * FL) + (qnhToUse - 1013.25) * 30.0;
            }
            else
            {
                // Por encima de la TA -> presión estándar
                ModeC_Corrected = 100.0 * FL;
            }
        }

        /// <summary>
        /// Determina si el target está en tierra basándose en los datos disponibles
        /// </summary>
        private bool DetermineGroundStatus(RawCat021Data rawData)
        {
            // 1. Confiar en el Ground Bit si está presente
            if (rawData.GBS)
                return true;

            // 2. ATP = 2 significa vehículo en superficie
            if (rawData.ATP == 2)
                return true;

            // 3. El Flight Level indica claramente que está en tierra
            double fl = rawData.FlightLevel_Raw * 0.25;
            if (rawData.FlightLevel_Raw == 0 || fl <= 14.0)
                return true;

            // 4. En otro caso, asumir en vuelo (airborne)
            return false;
        }


        // <summary>
        // Verifica si el registro cumple con los filtros del FIR de Barcelona
        // </summary>
        public bool IsWithinBarcelonaFIR()
        {
            return LAT > 40.9 && LAT < 41.7 && LON > 1.5 && LON < 2.6;
        }

        /// <summary>
        /// Verifica si el registro es válido (airborne y dentro del FIR)
        /// </summary>
        public bool IsValid()
        {
            return !IsOnGround;
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
