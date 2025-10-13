using System;
using System.Collections.Generic;
using System.Text;

namespace AsterixDecoder.Models.CAT048
{
    /// <summary>
    /// Decodificador ASTERIX CAT048 - Se encarga únicamente de decodificar datos binarios
    /// </summary>
    public class Cat048Decoder
    {
        private byte[] data;
        private int currentByte;

        public Cat048Decoder(byte[] asterixData)
        {
            data = asterixData;
            currentByte = 0;
        }

        public List<RawCat048Data> Decode()
        {
            var records = new List<RawCat048Data>();

            while (currentByte < data.Length)
            {
                if (currentByte >= data.Length) break;
                byte cat = data[currentByte++];
                if (cat != 48) continue;

                if (currentByte + 1 >= data.Length) break;
                int length = data[currentByte] << 8 | data[currentByte + 1];
                currentByte += 2;

                int recordEnd = currentByte + length - 3;
                if (recordEnd > data.Length) break;

                var fspec = ReadFSPEC();
                var record = new RawCat048Data();
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

        private void DecodeRecord(RawCat048Data record, List<bool> fspec, int recordEnd)
        {
            int fspecIndex = 0;

            // FRN 1 - I048/010 - Data Source Identifier
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
            {
                record.SAC = data[currentByte++];
                record.SIC = data[currentByte++];
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

                record.TargetType = typ;
                record.SPIPresent = spi;
                record.RABPresent = rab;
                record.TargetReportDescriptor = trd.ToString();

                if ((octet1 & 0x01) != 0 && CheckBytes(1, recordEnd))
                {
                    byte octet2 = data[currentByte++];
                }
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
                record.FlightLevel = fl; // Se guarda en quarters de FL
            }

            // FRN 7 - I048/130 - Radar Plot Characteristics
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
            {
                bool hasExtension;
                do
                {
                    byte primary = data[currentByte++];

                    // Bits 8..2 indican presencia de subcampos
                    for (int bit = 7; bit >= 1; bit--)
                    {
                        if (((primary >> bit) & 0x01) == 1)
                        {
                            // Subcamp present → read/skip 1 byte
                            if (CheckBytes(1, recordEnd))
                            {
                                byte subfield = data[currentByte++];
                            }
                        }
                    }

                    // Bit 1 (LSB) = FX → indica si hay otro Primary Subfield
                    hasExtension = (primary & 0x01) != 0;

                } while (hasExtension && CheckBytes(1, recordEnd));
            }

            // FX del primer octeto FSPEC
            if (fspecIndex < fspec.Count && fspec[fspecIndex++])
            {
                // Extension marker
            }

            // FRN 8 - I048/220 - Aircraft Address
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(3, recordEnd))
            {
                int addr = data[currentByte] << 16 | data[currentByte + 1] << 8 | data[currentByte + 2];
                currentByte += 3;
                record.AircraftAddress = addr.ToString("X6");
            }

            // FRN 9 - I048/240 - Aircraft Identification (fix: 6 bytes IA-5 packed)
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(6, recordEnd))
            {
                // Leer los 6 bytes empaquetados
                Span<byte> b = stackalloc byte[6];
                for (int i = 0; i < 6; i++) b[i] = data[currentByte++];

                // Extraer 8 valores de 6 bits
                int[] sixBits = new int[8];
                sixBits[0] = (b[0] >> 2) & 0x3F;
                sixBits[1] = ((b[0] & 0x03) << 4 | (b[1] >> 4)) & 0x3F;
                sixBits[2] = ((b[1] & 0x0F) << 2 | (b[2] >> 6)) & 0x3F;
                sixBits[3] = b[2] & 0x3F;
                sixBits[4] = (b[3] >> 2) & 0x3F;
                sixBits[5] = ((b[3] & 0x03) << 4 | (b[4] >> 4)) & 0x3F;
                sixBits[6] = ((b[4] & 0x0F) << 2 | (b[5] >> 6)) & 0x3F;
                sixBits[7] = b[5] & 0x3F;

                // Convertir cada valor IA-5 a carácter
                var sb = new StringBuilder(8);
                for (int i = 0; i < 8; i++)
                {
                    char c = DecodeIA5Character((byte)sixBits[i]);
                    sb.Append(c);
                }

                record.AircraftIdentification = sb.ToString().Trim();
            }

            // FRN 10 - I048/250 - Mode S MB Data
            if (fspecIndex < fspec.Count && fspec[fspecIndex++])
            {
                DecodeModeSMBData(record, recordEnd);
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

                record.VxRaw = vx;
                record.VyRaw = vy;
            }

            // FRN 14 - I048/170 - Track Status
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
            {
                bool hasExtension;
                do
                {
                    byte octet = data[currentByte++];
                    hasExtension = (octet & 0x01) != 0;
                }
                while (hasExtension && CheckBytes(1, recordEnd));
            }

            // Continuar con el resto de los campos según FSPEC...
            DecodeRemainingFields(record, fspec, ref fspecIndex, recordEnd);
        }

        private bool CheckBytes(int needed, int recordEnd)
        {
            return currentByte + needed <= recordEnd && currentByte + needed <= data.Length;
        }

        private void DecodeRemainingFields(RawCat048Data record, List<bool> fspec, ref int fspecIndex, int recordEnd)
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
                int height = data[currentByte] << 8 | data[currentByte + 1];
                if ((height & 0x2000) != 0) height |= unchecked((int)0xFFFFC000);
                record.Height3DRadar = height * 25.0; // 25 ft resolution
                currentByte += 2;
            }

            // FRN 20 - I048/120 - Radial Doppler Speed
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
            {
                int doppler = data[currentByte] << 8 | data[currentByte + 1];
                if ((doppler & 0x8000) != 0) doppler |= unchecked((int)0xFFFF0000);
                record.RadialDopplerSpeed = doppler * 1.0; // m/s
                currentByte += 2;
            }

            // FX
            if (fspecIndex < fspec.Count && fspec[fspecIndex++])
            {
                // Tercer octeto FSPEC
            }

            // FRN 21 - I048/230 - Communications/ACAS Capability
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

            // FRN 27-28 - SP y RE
            for (int i = 0; i < 2; i++)
            {
                if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
                {
                    byte len = data[currentByte++];
                    if (CheckBytes(len, recordEnd))
                        currentByte += len;
                }
            }
        }

        private void DecodeModeSMBData(RawCat048Data record, int recordEnd)
        {
            // Verificar que hay al menos 1 byte para el repetition factor
            if (!CheckBytes(1, recordEnd)) return;

            // Repetition factor
            byte rep = data[currentByte++];

            for (int i = 0; i < rep; i++)
            {
                // Verificar que hay 8 bytes disponibles para este bloque
                if (!CheckBytes(8, recordEnd))
                {
                    break;
                }

                var block = new ModeSMBDataBlock();
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

                record.ModeSDataBlocks.Add(block);
            }
        }

        private void DecodeBDS40(ModeSMBDataBlock block)
        {
            // BDS 4.0 - Selected Vertical Intention
            // MCP/FCU Selected Altitude (bits 14-25)
            int mcpAlt = (block.RawData[1] & 0x0F) << 8 | block.RawData[2];
            if (mcpAlt != 0) block.MCP_FCU_SelectedAltitude = mcpAlt * 16;

            // FMS Selected Altitude (bits 27-38)
            int fmsAlt = (block.RawData[3] & 0x0F) << 8 | block.RawData[4];
            if (fmsAlt != 0) block.FMS_SelectedAltitude = fmsAlt * 16;

            // Barometric Pressure Setting (bits 40-51)
            int bps = (block.RawData[5] & 0x7F) << 5 | block.RawData[6] >> 3 & 0x1F;
            if (bps != 0) block.BarometricPressureSetting = 800.0 + bps * 0.1;
        }

        private void DecodeBDS50(ModeSMBDataBlock block)
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

            // Track Angle Rate (bits 36-45)
            int tar = (block.RawData[4] & 0x1F) << 5 | block.RawData[5] >> 3 & 0x1F;
            if (tar != 0)
            {
                if ((tar & 0x200) != 0) tar |= unchecked((int)0xFFFFFC00);
                block.TrackAngleRate = tar * 8.0 / 256.0;
            }

            // True Airspeed (bits 47-56)
            int tas = (block.RawData[5] & 0x03) << 8 | block.RawData[6];
            if (tas != 0) block.TrueAirspeed = tas * 2.0; // knots
        }

        private void DecodeBDS60(ModeSMBDataBlock block)
        {
            // BDS 6.0 - Heading and Speed Report
            // Magnetic Heading (bits 2-12)
            int heading = (block.RawData[0] & 0x3F) << 5 | block.RawData[1] >> 3 & 0x1F;
            if (heading != 0) block.MagneticHeading = heading * 90.0 / 512.0;

            // Indicated Airspeed (bits 14-23)
            int ias = (block.RawData[1] & 0x03) << 8 | block.RawData[2];
            if (ias != 0) block.IndicatedAirspeed = ias; // knots

            // Mach Number (bits 25-34)
            int mach = (block.RawData[3] & 0x7F) << 3 | block.RawData[4] >> 5 & 0x07;
            if (mach != 0) block.MachNumber = mach * 0.008;

            // Barometric Altitude Rate (bits 36-45)
            int bar = (block.RawData[4] & 0x1F) << 5 | block.RawData[5] >> 3 & 0x1F;
            if (bar != 0)
            {
                if ((bar & 0x200) != 0) bar |= unchecked((int)0xFFFFFC00);
                block.BarometricAltitudeRate = bar * 32.0; // ft/min
            }

            // Inertial Vertical Velocity (bits 47-56)
            int ivv = (block.RawData[5] & 0x03) << 8 | block.RawData[6];
            if (ivv != 0)
            {
                if ((ivv & 0x200) != 0) ivv |= unchecked((int)0xFFFFFC00);
                block.InertialVerticalVelocity = ivv * 32.0; // ft/min
            }
        }

        private char DecodeIA5Character(byte b)
        {
            b &= 0x3F; // solo 6 bits

            // Bits altos: columna
            int col = ((b >> 4) & 0x03); // b6 y b5

            // Bits bajos: fila
            int row = b & 0x0F; // b4–b1

            // La tabla define:
            // Col 0: Letras P, A–O
            // Col 1: Letras Q–O, V–O
            // Col 2: SP, 0–9
            // Col 3: Dígitos 0–9

            // Caracteres para las columnas, según la tabla
            char[] col0 = { ' ', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O' };
            char[] col1 = { 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', ' ', ' ', ' ', ' ', ' ' };
            char[] col2 = { ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ' };
            char[] col3 = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ' ', ' ', ' ', ' ', ' ', ' ' };

            switch (col)
            {
                case 0: return col0[row];
                case 1: return col1[row];
                case 2: return col2[row];
                case 3: return col3[row];
                default: return ' ';
            }
        }

    }
}