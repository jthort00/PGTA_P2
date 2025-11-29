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
        // Indica si el campo Flight Level (I048/090) estaba presente en el mensaje.
        public bool HasFlightLevel { get; set; }

        // FRN 1 - I048/010 - Data Source Identifier
        public int SAC { get; set; }
        public int SIC { get; set; }

        // FRN 2 - I048/140 - Time of Day
        public TimeSpan? TimeOfDay { get; set; }

        public int TypDesc { get; set; }

        // FRN 3 - I048/020 - Target Report Descriptor
        public string TargetReportDescriptor { get; set; }
        public int TargetType { get; set; }
        public bool SPIPresent { get; set; }
        public bool RABPresent { get; set; }

        // FRN 4 - I048/040 - Posición medida en coordenadas polares inclinadas
        public double RhoSlantRange { get; set; } // NM
        public double ThetaAzimuth { get; set; } // grados

        // FRN 5 - I048/070 - Mode-3/A Code in Octal Representation
        public string Mode3ACode { get; set; }

        // FRN 6 - I048/090 - Flight Level in Binary Representation
        public int FlightLevel { get; set; } // quarters of FL (con signo)
        

        // FRN 8 - I048/220 - Aircraft Address
        public string AircraftAddress { get; set; }

        // FRN 9 - I048/240 - Aircraft Identification
        public string AircraftIdentification { get; set; }

        // FRN 10 - I048/250 - Mode S MB Data
        public List<ModeSMBDataBlock> ModeSDataBlocks { get; set; } = new List<ModeSMBDataBlock>();

        // FRN 11 - I048/161 - Track Number
        public int TrackNumber { get; set; }
        

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
    }
}
