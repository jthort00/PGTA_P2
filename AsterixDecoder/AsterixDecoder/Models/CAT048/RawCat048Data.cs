using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsterixDecoder.Models.CAT048
{
    /// <summary>
    /// Estructura de datos en crudo decodificados de ASTERIX CAT048
    /// </summary>
    public class RawCat048Data
    {
        public int SAC { get; set; }
        public int SIC { get; set; }
        public TimeSpan? TimeOfDay { get; set; }
        public string TargetReportDescriptor { get; set; }
        public int TargetType { get; set; }
        public bool SPIPresent { get; set; }
        public bool RABPresent { get; set; }
        public double RhoSlantRange { get; set; } // NM
        public double ThetaAzimuth { get; set; } // degrees
        public string Mode3ACode { get; set; }
        public int FlightLevel { get; set; } // quarters of FL
        public string AircraftAddress { get; set; }
        public string AircraftIdentification { get; set; }
        public List<ModeSMBDataBlock> ModeSDataBlocks { get; set; } = new List<ModeSMBDataBlock>();
        public int TrackNumber { get; set; }
        public int VxRaw { get; set; }
        public int VyRaw { get; set; }
        public double Height3DRadar { get; set; } // feet
        public double RadialDopplerSpeed { get; set; } // m/s
    }
}
