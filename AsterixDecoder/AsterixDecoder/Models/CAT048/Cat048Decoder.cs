using System;
using System.Collections.Generic;
using System.Text;

namespace AsterixDecoder.Models.CAT048
{
    /// <summary>
    /// Decodificador ASTERIX CAT048 - CORREGIDO
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
                bool sim = (octet1 & 0x10) != 0;
                bool rdp = (octet1 & 0x08) != 0;
                bool spi = (octet1 & 0x04) != 0;
                bool rab = (octet1 & 0x02) != 0;

                // Construir descripción detallada
                string typDesc = typ switch
                {
                    0 => "No detection",
                    1 => "Single PSR detection",
                    2 => "Single SSR detection",
                    3 => "SSR + PSR detection",
                    4 => "Single ModeS All-Call",
                    5 => "Single ModeS Roll-Call",
                    6 => "ModeS All-Call + PSR",
                    7 => "ModeS Roll-Call + PSR",
                    _ => "Unknown"
                };

                string alertStatus = spi ? "SPI" : "No alert";
                string radarStatus = rdp ? "RDP" : "no SPI";
                string groundStatus = typ == 5 || typ == 7 ? "aircraft on ground" : "aircraft airborne";

                trd.Append($"{alertStatus}, {radarStatus}, {groundStatus}");

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

            // FRN 11 - I048/161 - Track Number (CORREGIR)
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
            {
                // Los bits 16-13 están a 0, Track Number está en bits 12-1
                int tn = ((data[currentByte] & 0x0F) << 8) | data[currentByte + 1];
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
                // Groundspeed (bits 32-17) and Heading (bits 16-1)
                // Both are 16-bit values as per ASTERIX CAT048 I048/200:
                // - GS LSB = 2^-14 NM/s (unsigned magnitude)
                // - Heading LSB = 360/2^16 degrees (0..360)
                int gsRaw = (data[currentByte] << 8) | data[currentByte + 1];
                currentByte += 2;

                int hdgRaw = (data[currentByte] << 8) | data[currentByte + 1];
                currentByte += 2;

                // Store as-is in VxRaw (GS) and VyRaw (Heading) for later scaling in Cat048
                record.VxRaw = gsRaw;
                record.VyRaw = hdgRaw;
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

			// FX del segundo octeto FSPEC
            if (fspecIndex < fspec.Count && fspec[fspecIndex++])
            {
                // Extension marker
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

            // FRN 19 - I048/110 - Height 3D Radar ✗ NO DECODIFICAR (SKIP)
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
            {
                currentByte += 2; // Skip
            }

            // FRN 20 - I048/120 - Radial Doppler Speed ✗ NO DECODIFICAR (SKIP)
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
            {
                currentByte += 2; // Skip
            }

            // FRN 21 - I048/230 - Communications/ACAS Capability ✓ DECODIFICAR
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
            {
                byte byte1 = data[currentByte++];
                byte byte2 = data[currentByte++];

                int com = (byte1 >> 5) & 0x07;   // bits 16-14
                int stat = (byte1 >> 2) & 0x07;  // bits 13-11
                bool si = (byte1 & 0x02) != 0;   // bit 10

                bool mssc = (byte2 & 0x80) != 0; // bit 8
                bool arc = (byte2 & 0x40) != 0;  // bit 7
                bool aic = (byte2 & 0x20) != 0;  // bit 6
                int b1a = (byte2 >> 4) & 0x01;   // bit 5
                int b1b = byte2 & 0x0F;          // bits 4-1

                record.COM = com;
                record.STAT = stat;
                record.SI = si;
                record.MSSC = mssc;
                record.ARC = arc;
                record.AIC = aic;
                record.B1A = b1a;
                record.B1B = b1b;
            }
			
			// FX
            if (fspecIndex < fspec.Count && fspec[fspecIndex++])
            {
                // Tercer octeto FSPEC
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
			
			// FX
            if (fspecIndex < fspec.Count && fspec[fspecIndex++])
            {
                // Cuarto octeto FSPEC
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

                // IMPORTANTE: El formato de I048/250 según página 47 del PDF es:
                // bits 64-9:  BDS DATA (56 bits = 7 bytes, índices 0-6)
                // bits 8-5:   BDS1 (4 bits)
                // bits 4-1:   BDS2 (4 bits)
                // Por lo tanto, el BDS register está en el ÚLTIMO byte (índice 7)
                int bds1 = (block.RawData[7] >> 4) & 0x0F;
                int bds2 = block.RawData[7] & 0x0F;
                block.BDSRegister = (bds1 << 4) | bds2;

                // Decodificar según BDS
                if (block.BDSRegister == 0x40) DecodeBDS40(block);
                else if (block.BDSRegister == 0x50) DecodeBDS50(block);
                else if (block.BDSRegister == 0x60) DecodeBDS60(block);

                record.ModeSDataBlocks.Add(block);
            }
        }

        private void DecodeBDS40(ModeSMBDataBlock block)
        {
            // MCP/FCU Selected Altitude with status (bits 14-25 + status bit in bit 1 of byte0)
            bool mcpStatus = (block.RawData[0] & 0x80) != 0; // bit 1 of BDS 4.0 (MSB of byte 0 here)
            int mcpAlt = ((block.RawData[0] & 0x7F) << 5) | ((block.RawData[1] >> 3) & 0x1F);
            if (mcpStatus && mcpAlt != 0)
            {
                block.MCP_FCU_SelectedAltitude = mcpAlt * 16; // feet
            }

            // FMS Selected Altitude with status (bits 27-38 + status bit in byte1 bit 3)
            bool fmsStatus = (block.RawData[1] & 0x04) != 0;
            int fmsAlt = ((block.RawData[1] & 0x03) << 10) | (block.RawData[2] << 2) | ((block.RawData[3] >> 6) & 0x03);
            if (fmsStatus && fmsAlt != 0)
            {
                block.FMS_SelectedAltitude = fmsAlt * 16; // feet
            }

            // Barometric Pressure Setting with status (bits 29-18 + status bit in byte3 bit 6)
            bool bpsStatus = (block.RawData[3] & 0x20) != 0;
            double bps = ((block.RawData[3] & 0x1F) << 7) | ((block.RawData[4] >> 1) & 0x7F);

            if (bpsStatus && bps != 0)
            {
                double pressure = 800.0 + (bps * 0.1);
                if (pressure >= 800.0 && pressure <= 1200.0)
                {
                    block.BarometricPressureSetting = pressure; // hPa
                }
            }
        }

        private void DecodeBDS50(ModeSMBDataBlock block)
        {
            // BDS 5.0 - Track and Turn Report
            // Roll Angle (bits 2-11) - 10 bits con signo
            int roll = (block.RawData[0] & 0x3F) << 4 | (block.RawData[1] >> 4) & 0x0F;
            if ((roll & 0x200) != 0) roll |= unchecked((int)0xFFFFFC00);
            block.RollAngle = roll * 45.0 / 256.0; // Siempre guardar, incluso si es 0

            // True Track Angle (bits 13-23) - 11 bits
            int track = (block.RawData[1] & 0x07) << 8 | block.RawData[2];
            block.TrueTrackAngle = track * 90.0 / 512.0;

            // Ground Speed (bits 25-34) - 10 bits
            int gs = (block.RawData[3] & 0x7F) << 3 | (block.RawData[4] >> 5) & 0x07;
            block.GroundSpeed = gs * 2.0; // knots - 0 es válido (avión parado)

            // Track Angle Rate (bits 36-45) - 10 bits con signo
            int tar = (block.RawData[4] & 0x1F) << 5 | (block.RawData[5] >> 3) & 0x1F;
            if ((tar & 0x200) != 0) tar |= unchecked((int)0xFFFFFC00);
            block.TrackAngleRate = tar * 8.0 / 256.0; // Siempre guardar

            // True Airspeed (bits 47-56) - 10 bits
            int tas = (block.RawData[5] & 0x03) << 8 | block.RawData[6];
            if (tas != 0) block.TrueAirspeed = tas * 2.0; // knots - 0 no es válido
        }

        private void DecodeBDS60(ModeSMBDataBlock block)
        {
            // BDS 6.0 - Heading and Speed Report
            // Magnetic Heading (bits 2-12) - 11 bits
            int heading = (block.RawData[0] & 0x3F) << 5 | (block.RawData[1] >> 3) & 0x1F;
            block.MagneticHeading = heading;

            // Indicated Airspeed (bits 14-23) - 10 bits
            int ias = (block.RawData[1] & 0x03) << 8 | block.RawData[2];
            if (ias != 0) block.IndicatedAirspeed = ias; // knots

            // Mach Number (bits 25-34) - 10 bits
            int mach = (block.RawData[3] & 0x7F) << 3 | (block.RawData[4] >> 5) & 0x07;
            if (mach != 0) block.MachNumber = mach * 0.008;

            // Barometric Altitude Rate (bits 36-45) - 10 bits con signo
            int bar = (block.RawData[4] & 0x1F) << 5 | (block.RawData[5] >> 3) & 0x1F;
            if ((bar & 0x200) != 0) bar |= unchecked((int)0xFFFFFC00);
            block.BarometricAltitudeRate = bar * 32.0; // ft/min - Siempre guardar

            // Inertial Vertical Velocity (bits 47-56) - 10 bits con signo
            int ivv = (block.RawData[5] & 0x03) << 8 | block.RawData[6];
            if ((ivv & 0x200) != 0) ivv |= unchecked((int)0xFFFFFC00);
            block.InertialVerticalVelocity = ivv * 32.0; // ft/min - Siempre guardar
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