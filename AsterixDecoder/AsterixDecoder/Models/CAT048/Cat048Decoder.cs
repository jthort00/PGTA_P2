using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AsterixDecoder.Models.CAT048
{
    /// <summary>
    /// Decodificador ASTERIX CAT048
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

                record.TypDesc = typ;
                record.TargetType = typ;
                record.SPIPresent = spi;
                record.RABPresent = rab;
                

                if ((octet1 & 0x01) != 0 && CheckBytes(1, recordEnd))
                {
                    byte octet2 = data[currentByte++];
                }
            }

            // FRN 4 - I048/040 - Posición medida
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
                record.HasFlightLevel = true; // marcar presencia incluso si vale 0
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

            //// FX del primer octeto FSPEC
            //if (fspecIndex < fspec.Count && fspec[fspecIndex++])
            //{
            //    // Extension marker
            //}

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
            //Console.WriteLine($"Fspec index {fspecIndex}, {fspec[fspecIndex-1]}, {CheckBytes(2, recordEnd)}");
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
            {
                // Los bits 16-13 están a 0, Track Number está en bits 12-1
                int tn = ((data[currentByte] & 0x0F) << 8) | data[currentByte + 1];
                currentByte += 2;
                record.TrackNumber = tn;
                //Console.WriteLine($"Track Number: {tn}");
            }

            // FRN 12 - I048/042 - Calculated Position (NO DECODIFICAR)
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(4, recordEnd))
            {
                currentByte += 4; // Skip sin decodificar
            }

            // FRN 13 - I048/200 - Calculated Track Velocity in Polar Co-ordinates
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(4, recordEnd))
            {
                
                // Ground Speed - 16 bits sin signo, LSB = 0.22 kt o 2^-14 NM/s
                int gsRaw = (data[currentByte] << 8) | data[currentByte + 1];
                currentByte += 2;
                double groundSpeedKt = gsRaw * 0.22; // Convertir a knots
                //Console.WriteLine($"GroundSpeed: {groundSpeedKt} kt");

                

                // Heading - 16 bits sin signo, LSB = 360 / 2^16 grados
                int hdgRaw = (data[currentByte] << 8) | data[currentByte + 1];
                currentByte += 2;
                double heading = hdgRaw * (360.0 / Math.Pow(2, 16)); // grados
                
                record.VxRaw = groundSpeedKt;
                record.VyRaw = heading;
            }

            // FRN 14 - I048/170 - Track Status
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
            {
                var trackStatus = new StringBuilder();
                int octetCount = 0;
                bool hasExtension;
   
                do
                {
                    byte octet = data[currentByte++];
                    octetCount++;
            
                    // Primer octeto - Información principal del estado de la traza
                    if (octetCount == 1)
                    {
                        // CNF - Bit 8 - Confirmed vs. Tentative Track
                        if ((octet & 0x80) != 0)
                            trackStatus.Append("CNF ");
        
                        // TRE - Bit 7 - Track vs. Plot
                        if ((octet & 0x40) != 0)
                            trackStatus.Append("TRE ");
   
                        // CST - Bit 6 - Coasting
                        if ((octet & 0x20) != 0)
                            trackStatus.Append("CST ");
        
                        // MAH - Bit 5 - Horizontal Manoeuvre
                        if ((octet & 0x10) != 0)
                            trackStatus.Append("MAH ");
          
                        // TCC - Bit 4 - Trajectory Change
                        if ((octet & 0x08) != 0)
                            trackStatus.Append("TCC ");

                        // STH - Bit 3 - Smoothed Track Height
                        if ((octet & 0x04) != 0)
                            trackStatus.Append("STH ");
        
                        // TOM - Bit 2 - Track detection type
                        if ((octet & 0x02) != 0)
                            trackStatus.Append("TOM ");
                    }
                    // Segundo octeto - Información adicional
                    else if (octetCount == 2)
                    {
                        // DOU - Bit 8 - Signals Doubt about Track
                        if ((octet & 0x80) != 0)
                            trackStatus.Append("DOU ");
   
                        // MRS - Bit 7 - Military Response
                        if ((octet & 0x40) != 0)
                            trackStatus.Append("MRS ");
  
                        // GHO - Bit 6 - Ghost Track
                        if ((octet & 0x20) != 0)
                            trackStatus.Append("GHO ");
                    }
          
                    hasExtension = (octet & 0x01) != 0;
          
                } while (hasExtension && CheckBytes(1, recordEnd));
     
                record.TrackStatus = trackStatus.ToString().Trim();
            }

			//// FX del segundo octeto FSPEC
   //         if (fspecIndex < fspec.Count && fspec[fspecIndex++])
   //         {
   //             // Extension marker
   //         }

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

            // FRN 21 - I048/230 - Communications/ACAS Capability
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

                // Actualizar la descripción del Target Report Descriptor desde I048/230 (STAT - Flight Status)
                // Sobrescribe cualquier texto previo generado desde I048/020 para cumplir el requisito.
                record.TargetReportDescriptor = stat switch
                {
                    0 => "No alert, no SPI, aircraft airborne",
                    1 => "No alert, no SPI, aircraft on ground",
                    2 => "Alert, no SPI, aircraft airborne",
                    3 => "Alert, no SPI, aircraft on ground",
                    4 => "Alert, SPI, aircraft airborne or on ground",
                    5 => "No alert, SPI, aircraft airborne or on ground",
                    6 => "Not assigned",
                    7 => "Unknown",
                    _ => "Unknown"
                };
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
            bool mcpStatus = (block.RawData[0] & 0x80) != 0; // bit 1 of BDS 4.0 (MSB of byte 0 here)
            int mcpAlt = ((block.RawData[0] & 0x7F) << 5) | ((block.RawData[1] >> 3) & 0x1F);
            if (mcpStatus && mcpAlt != 0)
            {
                block.MCP_FCU_SelectedAltitude = mcpAlt * 16; // feet
            }
            
            bool fmsStatus = (block.RawData[1] & 0x04) != 0;
            int fmsAlt = ((block.RawData[1] & 0x03) << 10) | (block.RawData[2] << 2) | ((block.RawData[3] >> 6) & 0x03);
            if (fmsStatus && fmsAlt != 0)
            {
                block.FMS_SelectedAltitude = fmsAlt * 16; // feet
            }

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
            bool rollStatus = (block.RawData[0] & 0x80) != 0; // bit 1
            if (rollStatus)
            {
                bool rollSign = (block.RawData[0] & 0x40) != 0; // bit 2
                int rollValue = ((block.RawData[0] & 0x3F) << 3) | ((block.RawData[1] >> 5) & 0x07); // bits 3-11
                
                double rollAngle = rollValue * 45.0 / 256.0;
                block.RollAngle = rollSign ? -rollAngle : rollAngle;
            }
            
            bool trackStatus = (block.RawData[1] & 0x10) != 0; // bit 12
            if (trackStatus)
            {
                bool trackSign = (block.RawData[1] & 0x08) != 0; // bit 13
                int trackValue = ((block.RawData[1] & 0x07) << 7) | ((block.RawData[2] >> 1) & 0x7F); // bits 14-23
                
                double trackAngle = trackValue * 90.0 / 512.0;
                block.TrueTrackAngle = trackSign ? -trackAngle : trackAngle;
            }
            
            bool gsStatus = (block.RawData[2] & 0x01) != 0; // bit 24
            if (gsStatus)
            {
                int gsValue = (block.RawData[3] << 2) | ((block.RawData[4] >> 6) & 0x03); // bits 25-34
                block.GroundSpeed = gsValue * 2.0; // LSB = 2 kt
                
                if (block.GroundSpeed >= 2046)
                    block.GroundSpeed = 2046;
            }
            
            bool tarStatus = (block.RawData[4] & 0x20) != 0; // bit 35
            if (tarStatus)
            {
                bool tarSign = (block.RawData[4] & 0x10) != 0; // bit 36
                int tarValue = ((block.RawData[4] & 0x0F) << 5) | ((block.RawData[5] >> 3) & 0x1F); // bits 37-45
                
                double trackRate = tarValue * 8.0 / 256.0;
                block.TrackAngleRate = tarSign ? -trackRate : trackRate;
            }
            
            bool tasStatus = (block.RawData[5] & 0x04) != 0; // bit 46
            if (tasStatus)
            {
                int tasValue = ((block.RawData[5] & 0x03) << 8) | block.RawData[6]; // bits 47-56
                block.TrueAirspeed = tasValue * 2.0; // LSB = 2 kt
                
                if (block.TrueAirspeed >= 2046)
                    block.TrueAirspeed = 2046;
            }
        }

        private void DecodeBDS60(ModeSMBDataBlock block)
        {
            // BDS 6.0 - Heading and Speed Report
            
            bool hdgStatus = (block.RawData[0] & 0x80) != 0; // bit 0
            if (hdgStatus)
            {
                bool hdgSign = (block.RawData[0] & 0x40) != 0; // bit 1
                int hdgValue = ((block.RawData[0] & 0x3F) << 4) | ((block.RawData[1] >> 4) & 0x0F); // bits 2-11
                
                if (hdgSign)
                    block.MagneticHeading = -180.0 + hdgValue * 90.0 / 512.0;
                else
                    block.MagneticHeading = hdgValue * 90.0 / 512.0;
            }
            
            bool iasStatus = (block.RawData[1] & 0x08) != 0; // bit 12
            if (iasStatus)
            {
                int iasValue = ((block.RawData[1] & 0x07) << 7) | ((block.RawData[2] >> 1) & 0x7F); // bits 13-22
                block.IndicatedAirspeed = iasValue * 1.0; // LSB = 1 kt
            }
            
            bool machStatus = (block.RawData[2] & 0x01) != 0; // bit 23
            if (machStatus)
            {
                int machValue = (block.RawData[3] << 2) | ((block.RawData[4] >> 6) & 0x03); // bits 24-33
                block.MachNumber = machValue * 0.004; // LSB = 0.004 Mach
                
                if (block.MachNumber >= 4.092)
                    block.MachNumber = 4.092;
            }
            
            bool barStatus = (block.RawData[4] & 0x20) != 0; // bit 34
            if (barStatus)
            {
                bool barSign = (block.RawData[4] & 0x10) != 0; // bit 35
                int barValue = ((block.RawData[4] & 0x0F) << 5) | ((block.RawData[5] >> 3) & 0x1F); // bits 36-44
                
                if (barSign)
                    block.BarometricAltitudeRate = -32.0;
                else
                    block.BarometricAltitudeRate = barValue * 32.0;
            }
            
            bool ivvStatus = (block.RawData[5] & 0x04) != 0; // bit 45
            if (ivvStatus)
            {
                bool ivvSign = (block.RawData[5] & 0x02) != 0; // bit 46
                int ivvValue = ((block.RawData[5] & 0x01) << 8) | block.RawData[6]; // bits 47-55
                
                if (ivvSign)
                    block.InertialVerticalVelocity = ivvValue * -32.0;
                else
                    block.InertialVerticalVelocity = ivvValue * 32.0;
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