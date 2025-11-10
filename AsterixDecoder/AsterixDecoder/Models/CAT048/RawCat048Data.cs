using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsterixDecoder.Models.CAT048
{
    /// <summary>
    /// Estructura de datos en crudo decodificados de ASTERIX CAT048
    /// Solo contiene los campos que SÍ se deben decodificar
    /// </summary>
    public class RawCat048Data
    {
        // FRN 1 - I048/010 - Data Source Identifier
        public int SAC { get; set; }
        public int SIC { get; set; }

        // FRN 2 - I048/140 - Time of Day
        public TimeSpan? TimeOfDay { get; set; }

        // NUEVO: valor numérico del TIP/Descriptor de I048/020
        public int TypDesc { get; set; }

        // FRN 3 - I048/020 - Target Report Descriptor
        public string TargetReportDescriptor { get; set; }
        public int TargetType { get; set; }
        public bool SPIPresent { get; set; }
        public bool RABPresent { get; set; }

        // FRN 4 - I048/040 - Measured Position in Slant Polar Co-ordinates
        public double RhoSlantRange { get; set; } // NM
        public double ThetaAzimuth { get; set; } // degrees

        // FRN 5 - I048/070 - Mode-3/A Code in Octal Representation
        public string Mode3ACode { get; set; }

        // FRN 6 - I048/090 - Flight Level in Binary Representation
        public int FlightLevel { get; set; } // quarters of FL (con signo)

        // FRN 7 - I048/130 - Radar Plot Characteristics
        // (se lee pero no se almacena)

        // FRN 8 - I048/220 - Aircraft Address
        public string AircraftAddress { get; set; }

        // FRN 9 - I048/240 - Aircraft Identification
        public string AircraftIdentification { get; set; }

        // FRN 10 - I048/250 - Mode S MB Data
        public List<ModeSMBDataBlock> ModeSDataBlocks { get; set; } = new List<ModeSMBDataBlock>();

        // FRN 11 - I048/161 - Track Number
        public int TrackNumber { get; set; }

        // FRN 12 - I048/042 - Calculated Position in Cartesian Co-ordinates
        // NO SE DECODIFICA (se salta)

        // FRN 13 - I048/200 - Calculated Track Velocity in Polar Representation
        public double VxRaw { get; set; } // Groundspeed raw
        public double VyRaw { get; set; } // Heading raw

        // FRN 14 - I048/170 - Track Status
        public string TrackStatus { get; set; }

        // FRN 21 - I048/230 - Communications/ACAS Capability and Flight Status
        public int COM { get; set; }     // Communications capability (3 bits)
        public int STAT { get; set; }    // Flight Status (3 bits)
        public bool SI { get; set; }     // SI/II Transponder Capability
        public bool MSSC { get; set; }   // Mode-S Specific Service Capability
        public bool ARC { get; set; }    // Altitude reporting capability
        public bool AIC { get; set; }    // Aircraft identification capability
        public int B1A { get; set; }     // BDS 1,0 bit 16
        public int B1B { get; set; }     // BDS 1,0 bits 37/40

        // CAMPOS NO DECODIFICADOS (marcados en rojo):
        // FRN 12 - I048/042 - Calculated Position in Cartesian Co-ordinates - NO
        // FRN 15 - I048/210 - Track Quality - NO
        // FRN 16 - I048/030 - Warning/Error - NO
        // FRN 17 - I048/080 - Mode 3/A Confidence - NO
        // FRN 18 - I048/100 - Mode C Confidence - NO
        // FRN 19 - I048/110 - Height 3D Radar - NO
        // FRN 20 - I048/120 - Radial Doppler Speed - NO
        // FRN 22 - I048/260 - ACAS Resolution Advisory - NO
        // FRN 23 - I048/055 - Mode-1 Code - NO
        // FRN 24 - I048/050 - Mode-2 Code - NO
        // FRN 25 - I048/065 - Mode-1 Confidence - NO
        // FRN 26 - I048/060 - Mode-2 Confidence - NO
        // FRN 27 - SP-Data - Special Purpose - NO
        // FRN 28 - RE-Data - Reserved Expansion - NO
    }
}
