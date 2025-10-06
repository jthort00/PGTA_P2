using System;
using System.Collections.Generic;
using System.Text;

namespace AsterixDecoder.Models
{
    /// <summary>
    /// Decodificador ASTERIX CAT048 
    /// </summary>
    public class Cat048Decoder
    {
        private byte[] data;
        private int currentByte;

        public class Cat048Record
        {
            // FRN 1 - I048/010
            public string DataSourceIdentifier { get; set; }

            // FRN 2 - I048/140
            public TimeSpan? TimeOfDay { get; set; }

            // FRN 3 - I048/020
            public string TargetReportDescriptor { get; set; }

            // FRN 4 - I048/040
            public double RhoSlantRange { get; set; } // NM
            public double ThetaAzimuth { get; set; } // degrees

            // FRN 5 - I048/070
            public string Mode3ACode { get; set; }

            // FRN 6 - I048/090
            public int FlightLevel { get; set; } // quarters of FL

            // FRN 7 - I048/130
            public string RadarPlotCharacteristics { get; set; }

            // FRN 8 - I048/220
            public string AircraftAddress { get; set; }

            // FRN 9 - I048/240
            public string AircraftIdentification { get; set; }

            // FRN 10 - I048/250
            public ModeSMBData ModeSData { get; set; }

            // FRN 11 - I048/161
            public int TrackNumber { get; set; }

            // FRN 13 - I048/200
            public double GroundSpeedKnots { get; set; }
            public double HeadingDegrees { get; set; }

            // FRN 14 - I048/170
            public string TrackStatus { get; set; }

            // FRN 15 - I048/210
            public int TrackQuality { get; set; }

            // FRN 16 - I048/030
            public string WarningErrorConditions { get; set; }

            // FRN 17 - I048/080
            public string Mode3AConfidence { get; set; }

            // FRN 18 - I048/100
            public string ModeCConfidence { get; set; }

            // FRN 19 - I048/110
            public double Height3DRadar { get; set; } // feet

            // FRN 20 - I048/120
            public double RadialDopplerSpeed { get; set; } // m/s

            // FRN 21 - I048/230
            public string CommunicationsACASCapability { get; set; }

            // FRN 22 - I048/260
            public string ACASResolutionAdvisory { get; set; }

            // FRN 27 - SP-Data
            public byte[] SpecialPurposeField { get; set; }

            // FRN 28 - RE-Data
            public byte[] ReservedExpansionField { get; set; }
        }

        public class ModeSMBData
        {
            public List<MBDataBlock> MBDataBlocks { get; set; } = new List<MBDataBlock>();
        }

        public class MBDataBlock
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

        public Cat048Decoder(byte[] asterixData)
        {
            data = asterixData;
            currentByte = 0;
        }

        public List<Cat048Record> Decode()
        {
            var records = new List<Cat048Record>();

            while (currentByte < data.Length)
            {
                // Leer CAT (1 byte)
                if (currentByte >= data.Length) break;
                byte cat = data[currentByte++];
                if (cat != 48) continue;

                // Leer longitud (2 bytes)
                if (currentByte + 1 >= data.Length) break;
                int length = data[currentByte] << 8 | data[currentByte + 1];
                currentByte += 2;

                int recordEnd = currentByte + length - 3;

                // Validar que no excedamos el buffer
                if (recordEnd > data.Length) break;

                // Leer FSPEC
                var fspec = ReadFSPEC();

                // Decodificar record
                var record = new Cat048Record();
                DecodeRecord(record, fspec, recordEnd);
                records.Add(record);

                currentByte = recordEnd;
            }

            return records;
        }

        private List<bool> ReadFSPEC()
        {
            var fspec = new List<bool>();
            bool hasExtension = true;

            while (hasExtension && currentByte < data.Length)
            {
                byte octet = data[currentByte++];

                for (int i = 7; i >= 1; i--)
                {
                    fspec.Add((octet & 1 << i) != 0);
                }

                hasExtension = (octet & 1) != 0;
            }

            return fspec;
        }

        private void DecodeRecord(Cat048Record record, List<bool> fspec, int recordEnd)
        {
            int fspecIndex = 0;

            // FRN 1 - I048/010 - Data Source Identifier
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
            {
                int sac = data[currentByte++];
                int sic = data[currentByte++];
                record.DataSourceIdentifier = $"SAC:{sac} SIC:{sic}";
            }

            // FRN 2 - I048/140 - Time of Day
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(3, recordEnd))
            {
                int tod = data[currentByte] << 16 | data[currentByte + 1] << 8 | data[currentByte + 2];
                currentByte += 3;
                record.TimeOfDay = TimeSpan.FromSeconds(tod / 128.0);
            }

            // FRN 3 - I048/020 - Target Report Descriptor
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
            {
                byte octet1 = data[currentByte++];
                StringBuilder trd = new StringBuilder();

                int typ = octet1 >> 5 & 0x07;
                trd.Append($"TYP:{typ} ");

                bool sim = (octet1 & 0x10) != 0;
                trd.Append($"SIM:{(sim ? 1 : 0)} ");

                bool rdp = (octet1 & 0x08) != 0;
                trd.Append($"RDP:{(rdp ? 1 : 0)} ");

                bool spi = (octet1 & 0x04) != 0;
                trd.Append($"SPI:{(spi ? 1 : 0)} ");

                bool rab = (octet1 & 0x02) != 0;
                trd.Append($"RAB:{(rab ? 1 : 0)}");

                if ((octet1 & 0x01) != 0 && CheckBytes(1, recordEnd))
                {
                    byte octet2 = data[currentByte++];
                }

                record.TargetReportDescriptor = trd.ToString();
            }

            // FRN 4 - I048/040 - Measured Position
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(4, recordEnd))
            {
                int rho = data[currentByte] << 8 | data[currentByte + 1];
                currentByte += 2;
                record.RhoSlantRange = rho * (1.0 / 256.0); // NM

                int theta = data[currentByte] << 8 | data[currentByte + 1];
                currentByte += 2;
                record.ThetaAzimuth = theta * (360.0 / 65536.0); // degrees
            }

            // FRN 5 - I048/070 - Mode-3/A Code
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
            {
                int code = (data[currentByte] & 0x0F) << 8 | data[currentByte + 1];
                currentByte += 2;
                record.Mode3ACode = Convert.ToString(code, 8).PadLeft(4, '0');
            }

            // FRN 6 - I048/090 - Flight Level
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
            {
                int fl = (data[currentByte] & 0x3F) << 8 | data[currentByte + 1];
                currentByte += 2;
                if ((fl & 0x2000) != 0) fl |= unchecked((int)0xFFFFC000);
                record.FlightLevel = fl;
            }

            // FRN 7 - I048/130 - Radar Plot Characteristics
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
            {
                byte firstOctet = data[currentByte++];
                StringBuilder rpc = new StringBuilder();

                while ((firstOctet & 0x01) != 0 && CheckBytes(1, recordEnd))
                {
                    firstOctet = data[currentByte++];
                }

                record.RadarPlotCharacteristics = rpc.ToString();
            }

            // FX - Skip if present
            if (fspecIndex < fspec.Count && fspec[fspecIndex++])
            {
                // Segundo octeto del FSPEC
            }

            // FRN 8 - I048/220 - Aircraft Address
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(3, recordEnd))
            {
                int addr = data[currentByte] << 16 | data[currentByte + 1] << 8 | data[currentByte + 2];
                currentByte += 3;
                record.AircraftAddress = addr.ToString("X6");
            }

            // FRN 9 - I048/240 - Aircraft Identification
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(6, recordEnd))
            {
                StringBuilder callsign = new StringBuilder();
                for (int i = 0; i < 6; i++)
                {
                    byte b = data[currentByte++];
                    char c = DecodeIA5Character(b);
                    if (c != ' ') callsign.Append(c);
                }
                record.AircraftIdentification = callsign.ToString().Trim();
            }

            // FRN 10 - I048/250 - Mode S MB Data
            if (fspecIndex < fspec.Count && fspec[fspecIndex++])
            {
                record.ModeSData = DecodeModeSMBData(recordEnd);
            }

            // FRN 11 - I048/161 - Track Number
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
            {
                int tn = (data[currentByte] & 0x0F) << 8 | data[currentByte + 1];
                currentByte += 2;
                record.TrackNumber = tn;
            }

            // FRN 12 - I048/042 - Calculated Position (NO DECODIFICAR)
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(4, recordEnd))
            {
                currentByte += 4; // Skip sin decodificar
            }

            // FRN 13 - I048/200 - Calculated Track Velocity
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(4, recordEnd))
            {
                int vx = data[currentByte] << 8 | data[currentByte + 1];
                if ((vx & 0x8000) != 0) vx |= unchecked((int)0xFFFF0000);
                currentByte += 2;

                int vy = data[currentByte] << 8 | data[currentByte + 1];
                if ((vy & 0x8000) != 0) vy |= unchecked((int)0xFFFF0000);
                currentByte += 2;

                double vxKt = vx * (1.0 / 16384.0) * 3600.0; // NM/s to kt
                double vyKt = vy * (1.0 / 16384.0) * 3600.0;

                record.GroundSpeedKnots = Math.Sqrt(vxKt * vxKt + vyKt * vyKt);
                record.HeadingDegrees = Math.Atan2(vxKt, vyKt) * 180.0 / Math.PI;
                if (record.HeadingDegrees < 0) record.HeadingDegrees += 360.0;
            }

            // FRN 14 - I048/170 - Track Status
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
            {
                byte octet = data[currentByte++];
                bool hasExtension = (octet & 0x01) != 0;
                while (hasExtension && CheckBytes(1, recordEnd))
                {
                    octet = data[currentByte++];
                    hasExtension = (octet & 0x01) != 0;
                }
            }

            // Continuar con el resto de los campos según FSPEC...
            DecodeRemainingFields(record, fspec, ref fspecIndex, recordEnd);
        }

        private bool CheckBytes(int needed, int recordEnd)
        {
            return currentByte + needed <= recordEnd && currentByte + needed <= data.Length;
        }

        private void DecodeRemainingFields(Cat048Record record, List<bool> fspec, ref int fspecIndex, int recordEnd)
        {
            // FRN 15 - I048/210 - Track Quality
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(4, recordEnd))
            {
                currentByte += 4; // 4 octetos
            }

            // FRN 16 - I048/030 - Warning/Error
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
            {
                byte octet = data[currentByte++];
                bool hasExtension = (octet & 0x01) != 0;
                while (hasExtension && CheckBytes(1, recordEnd))
                {
                    octet = data[currentByte++];
                    hasExtension = (octet & 0x01) != 0;
                }
            }

            // FRN 17 - I048/080 - Mode 3/A Confidence
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
            {
                currentByte += 2;
            }

            // FRN 18 - I048/100 - Mode C Confidence
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(4, recordEnd))
            {
                currentByte += 4;
            }

            // FRN 19 - I048/110 - Height 3D Radar
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
            {
                currentByte += 2;
            }

            // FRN 20 - I048/120 - Radial Doppler Speed
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
            {
                currentByte += 2;
            }

            // FX
            if (fspecIndex < fspec.Count && fspec[fspecIndex++])
            {
                // Tercer octeto FSPEC
            }

            // FRN 21 - I048/230 - Communications/ACAS
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
            {
                currentByte += 2;
            }

            // FRN 22 - I048/260 - ACAS Resolution Advisory
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(7, recordEnd))
            {
                currentByte += 7;
            }

            // FRN 23-26 - NO DECODIFICAR según comentarios
            for (int i = 0; i < 4; i++)
            {
                if (fspecIndex < fspec.Count && fspec[fspecIndex++])
                {
                    // Skip estos campos
                    if (CheckBytes(1, recordEnd))
                    {
                        byte firstByte = data[currentByte++];
                        // Si tiene extension, leer más bytes
                        while ((firstByte & 0x01) != 0 && CheckBytes(1, recordEnd))
                        {
                            firstByte = data[currentByte++];
                        }
                    }
                }
            }

            // FX
            if (fspecIndex < fspec.Count && fspec[fspecIndex++])
            {
                // Cuarto octeto FSPEC
            }

            // FRN 27 - SP Special Purpose
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
            {
                byte len = data[currentByte++];
                if (CheckBytes(len, recordEnd))
                {
                    record.SpecialPurposeField = new byte[len];
                    Array.Copy(data, currentByte, record.SpecialPurposeField, 0, len);
                    currentByte += len;
                }
            }

            // FRN 28 - RE Reserved Expansion
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
            {
                byte len = data[currentByte++];
                if (CheckBytes(len, recordEnd))
                {
                    record.ReservedExpansionField = new byte[len];
                    Array.Copy(data, currentByte, record.ReservedExpansionField, 0, len);
                    currentByte += len;
                }
            }
        }

        private ModeSMBData DecodeModeSMBData(int recordEnd)
        {
            var mbData = new ModeSMBData();

            // Verificar que hay al menos 1 byte para el repetition factor
            if (!CheckBytes(1, recordEnd)) return mbData;

            // Repetition factor
            byte rep = data[currentByte++];

            for (int i = 0; i < rep; i++)
            {
                // Verificar que hay 8 bytes disponibles para este bloque
                if (!CheckBytes(8, recordEnd))
                {
                    //Console.WriteLine($"Advertencia: No hay suficientes bytes para el bloque MB {i + 1}/{rep}");
                    break;
                }

                var block = new MBDataBlock();
                block.RawData = new byte[8];
                Array.Copy(data, currentByte, block.RawData, 0, 8);
                currentByte += 8;

                // Determinar BDS register
                int bds1 = block.RawData[0] >> 4 & 0x0F;
                int bds2 = block.RawData[0] & 0x0F;
                block.BDSRegister = bds1 << 4 | bds2;

                // Decodificar según BDS
                if (block.BDSRegister == 0x40) DecodeBDS40(block);
                else if (block.BDSRegister == 0x50) DecodeBDS50(block);
                else if (block.BDSRegister == 0x60) DecodeBDS60(block);

                mbData.MBDataBlocks.Add(block);
            }

            return mbData;
        }

        private void DecodeBDS40(MBDataBlock block)
        {
            // BDS 4.0 - Selected Vertical Intention
            // MCP/FCU Selected Altitude (bits 14-25)
            int mcpAlt = (block.RawData[1] & 0x0F) << 8 | block.RawData[2];
            if (mcpAlt != 0) block.MCP_FCU_SelectedAltitude = mcpAlt * 16;

            // FMS Selected Altitude (bits 27-38)
            int fmsAlt = (block.RawData[3] & 0x0F) << 8 | block.RawData[4];
            if (fmsAlt != 0) block.FMS_SelectedAltitude = fmsAlt * 16;
        }

        private void DecodeBDS50(MBDataBlock block)
        {
            // BDS 5.0 - Track and Turn Report
            // Roll Angle (bits 2-11)
            int roll = (block.RawData[0] & 0x3F) << 4 | block.RawData[1] >> 4 & 0x0F;
            if (roll != 0)
            {
                if ((roll & 0x200) != 0) roll |= unchecked((int)0xFFFFFC00);
                block.RollAngle = roll * 45.0 / 256.0;
            }

            // True Track Angle (bits 13-23)
            int track = (block.RawData[1] & 0x07) << 8 | block.RawData[2];
            if (track != 0) block.TrueTrackAngle = track * 90.0 / 512.0;

            // Ground Speed (bits 25-34)
            int gs = (block.RawData[3] & 0x7F) << 3 | block.RawData[4] >> 5 & 0x07;
            if (gs != 0) block.GroundSpeed = gs * 2.0; // knots
        }

        private void DecodeBDS60(MBDataBlock block)
        {
            // BDS 6.0 - Heading and Speed Report
            // Magnetic Heading (bits 2-12)
            int heading = (block.RawData[0] & 0x3F) << 5 | block.RawData[1] >> 3 & 0x1F;
            if (heading != 0) block.MagneticHeading = heading * 90.0 / 512.0;

            // Indicated Airspeed (bits 14-23)
            int ias = (block.RawData[1] & 0x03) << 8 | block.RawData[2];
            if (ias != 0) block.IndicatedAirspeed = ias; // knots
        }

        private char DecodeIA5Character(byte b)
        {
            b &= 0x3F;
            if (b == 32) return ' ';
            if (b >= 1 && b <= 26) return (char)('A' + b - 1);
            if (b >= 48 && b <= 57) return (char)b;
            return ' ';
        }
    }
}
