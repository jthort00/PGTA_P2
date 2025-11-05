using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsterixDecoder.Models.CAT048
{
    /// <summary>
    /// Bloque de datos Mode S MB (BDS)
    /// </summary>
    public class ModeSMBDataBlock
    {
        public byte[] RawData { get; set; }
        public int BDSRegister { get; set; }

        // BDS 4.0 - Selected Vertical Intention
        public int MCP_FCU_SelectedAltitude { get; set; }
        public int FMS_SelectedAltitude { get; set; }
        public double BarometricPressureSetting { get; set; }

        // BDS 5.0 - Track and Turn Report
        public double RollAngle { get; set; }
        public double TrueTrackAngle { get; set; }
        public double GroundSpeed { get; set; }
        public double TrackAngleRate { get; set; }
        public double TrueAirspeed { get; set; }

        // BDS 6.0 - Heading and Speed Report
        public double MagneticHeading { get; set; }
        public double IndicatedAirspeed { get; set; }
        public double MachNumber { get; set; }
        public double BarometricAltitudeRate { get; set; }
        public double InertialVerticalVelocity { get; set; }
    }
}
