using System;

namespace AsterixDecoder.Models.CAT021
{
    /// <summary>
    /// Estructura de datos en crudo decodificados de ASTERIX CAT021
    /// </summary>
    public class RawCat021Data
    {
        // FRN 1 - I021/010
        public int SAC { get; set; }
        public int SIC { get; set; }

        // FRN 2 - I021/040
        public string TargetReportDescriptor { get; set; }
        public int ATP { get; set; }
        public int ARC { get; set; }
        public int RC { get; set; }
        public int RAB { get; set; }

        // FRN 7 - I021/131
        public double WGS84_Latitude { get; set; }
        public double WGS84_Longitude { get; set; }

        // FRN 11 - I021/080
        public string Target_Address { get; set; }

        // FRN 12 - I021/073
        public TimeSpan? Time_Reception_Position { get; set; }

        // FRN 19 - I021/070
        public string Mode3A_Code { get; set; }

        // FRN 21 - I021/145
        public int FlightLevel_Raw { get; set; }

        // FRN 29 - I021/170
        public string Target_Identification { get; set; }

        // FRN 48 - Reserved Expansion Field
        public bool? BarometricPressureSource { get; set; }
        public double BarometricPressureSetting { get; set; }
    }
}
