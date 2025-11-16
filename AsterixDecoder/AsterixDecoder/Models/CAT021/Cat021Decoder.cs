using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using AsterixDecoder.Models.CAT021;


namespace AsterixDecoder.Models
{
    /// <summary>
    /// Decodificador ASTERIX CAT021 
    /// </summary>

    public class Cat021Decoder
    {
        private byte[] data;
        private int currentByte;

        public Cat021Decoder(byte[] asterixData, double qnhActual=1013.25)
        {
            data = asterixData;
            currentByte = 0;
        }

        public List<RawCat021Data> Decode()
        {
            var records = new List<RawCat021Data>();

            while (currentByte < data.Length)
            {
	            // Ensure enough bytes for category and length
	            if (currentByte + 3 >= data.Length) break;

	            // Category (should be 21)
	            byte cat = data[currentByte++];
	            if (cat != 21)
		            continue;

	            // Record length (2 bytes)
	            int length = (data[currentByte] << 8) | data[currentByte + 1];
	            currentByte += 2;

	            int recordEnd = currentByte + length - 3;
	            if (recordEnd > data.Length) break;

	            // Decode FSPEC + record
	            var fspec  = ReadFSPEC();
	            var record = new RawCat021Data();
	            DecodeRecord(record, fspec, recordEnd);

                records.Add(record);
                currentByte = recordEnd;
            }

            return records;
            //Console.WriteLine($"Keeping record: OnGround={record.IsOnGround}, Lat={record.WGS84_Latitude}, Lon={record.WGS84_Longitude}");
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
        
        private bool CheckBytes(int bytesNeeded, int recordEnd) =>
	        (currentByte + bytesNeeded) <= recordEnd;
        
        

        private void DecodeRecord(RawCat021Data record, List<bool> fspec, int recordEnd)
		{
		    int fspecIndex = 0;
		    
		    // FRN 1 - I021/010 Data Source Identifier
		    if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
		    {
		        record.SAC = data[currentByte++];
		        record.SIC = data[currentByte++];
		        //Console.WriteLine($"After FRN FRN1: currentByte={currentByte}");
		    }

		    // FRN 2 - I021/040 Target Report Descriptor
		    if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
		    {
		        var trdBytes = new List<byte>();
		        bool fx = true;
		        while (fx && currentByte < recordEnd)
		        {
		            byte b = data[currentByte++];
		            trdBytes.Add(b);
		            fx = (b & 0x01) != 0;
		        }
		        byte octet1 = trdBytes[0];
		        record.ATP = (octet1 >> 5) & 0x07;
		        record.ARC = (octet1 >> 3) & 0x03;
		        record.RC  = (octet1 >> 2) & 0x01;
		        record.RAB = (octet1 >> 1) & 0x01;
		        record.TargetReportDescriptor = $"ATP={record.ATP}, ARC={record.ARC}, RC={record.RC}, RAB={record.RAB}";
		    }

		    // FRN 3 - I021/161 Track Number
		    if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
		    {
		        currentByte += 2;
		        //Console.WriteLine($"After FRN FRN3: currentByte={currentByte}");
		    }

		    // FRN 4 - I021/015 Service Identification
		    if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
		    {
		        currentByte += 1;
		        //Console.WriteLine($"After FRN FRN4: currentByte={currentByte}");
		    }

		    // FRN 5 - I021/071 Time Applicability Position
		    if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(3, recordEnd))
		    {
		        currentByte += 3;
		        //Console.WriteLine($"After FRN FRN5: currentByte={currentByte}");
		    }

		    // FRN 6 - I021/130 Position WGS84 (low-res)
		    if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(6, recordEnd))
		    {
		        currentByte += 6;
		        //Console.WriteLine($"After FRN FRN6: currentByte={currentByte}");
		    }

		    // FRN 7 – I021/131 – High-Resolution Position in WGS-84 Coordinates
		    if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(8, recordEnd))
		    {
			    // Read 8 bytes: first 4 = LAT, next 4 = LON  (based on verified data order)
			    byte[] latBytes = data.Skip(currentByte).Take(4).ToArray();
			    byte[] lonBytes = data.Skip(currentByte + 4).Take(4).ToArray();
			    currentByte += 8;

			    // Convert to signed 32-bit big-endian integers
			    Array.Reverse(latBytes);
			    Array.Reverse(lonBytes);
			    int rawLat = BitConverter.ToInt32(latBytes, 0);
			    int rawLon = BitConverter.ToInt32(lonBytes, 0);

			    // Scale factor per ASTERIX CAT021 spec
			    double lsb = 180.0 / Math.Pow(2, 30);
			    record.WGS84_Latitude  = rawLat * lsb;
			    record.WGS84_Longitude = rawLon * lsb;

			    //Console.WriteLine($"FRN7 decoded: Lat={record.WGS84_Latitude:F6}, Lon={record.WGS84_Longitude:F6}");
		    }

		    // FRN 8 - I021/072 Time Applicability Velocity
		    if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(3, recordEnd))
		    {
			    currentByte += 3;
			    //Console.WriteLine($"[DEBUG] Entering FRN8 decode at offset={currentByte:X}, next 6 bytes = " +
			                      //$"{BitConverter.ToString(data, currentByte, 6)}");
		    }

		    // FRN 9 - I021/100 Airspeed
		    if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
		    {
		        currentByte += 2;
		    }

		    // FRN 10 - I021/101 True Airspeed
		    if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
		    {
		        currentByte += 2;
		    }

		    // FRN 11 - I021/080 Target Address
		    if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(3, recordEnd))
		    {
		        record.Target_Address = BitConverter.ToString(data, currentByte, 3).Replace("-", "");
		        currentByte += 3;
		        //Console.WriteLine($"Target_Address:{record.Target_Address}");
		    }

		    // FRN 12 - I021/073 Time of Reception of Position
		    if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(3, recordEnd))
		    {
		        int timeRaw = (data[currentByte] << 16) | (data[currentByte + 1] << 8) | data[currentByte + 2];
		        record.Time_Reception_Position = TimeSpan.FromSeconds(timeRaw / 128.0);
		        currentByte += 3;
		        //Console.WriteLine($"Time_Reception_Position:{record.Time_Reception_Position}");
		    }

		    // FRN 13 - I021/074 Time of Message Reception Position
		    if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(4, recordEnd))
		    {
		        currentByte += 4;
		    }

		    // FRN 14 - I021/075 Time of Message Reception Velocity
		    if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(3, recordEnd))
		    {
		        currentByte += 3;
		    }

		    // FRN 15 - I021/076 Time of Message Reception Velocity High
		    if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(4, recordEnd))
		    {
		        currentByte += 4;
		    }

		    // FRN 16 - I021/140 Geometric Height
		    if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
		    {
		        currentByte += 2;
		    }

		    // FRN 17 - I021/141 Quality Indicators
		    if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
		    {
			    bool fx = true;
			    while (fx && currentByte < recordEnd)
			    {
				    byte b = data[currentByte++];
				    fx = (b & 0x01) != 0;  // FX bit (bit 1) -> if set, another octet follows
			    }
		    }

		    // FRN 18 - I021/142 MOPS Version
		    if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
		    {
		        currentByte += 1;
		    }

		    // FRN 19 - I021/070 Mode 3/A Code
		    if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
		    {
		        int mode3a = (data[currentByte] << 8) | data[currentByte + 1];
		        currentByte += 2;
		        record.Mode3A_Code = Convert.ToString(mode3a & 0x0FFF, 8);
		        //Console.WriteLine(record.Mode3A_Code);
		    }

		    // FRN 20 - I021/143 Roll Angle
		    if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
		    {
		        currentByte += 2;
		    }

		    // FRN 21 – I021/145 – Flight Level (2 bytes, 25 ft increments)
		    if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
		    {
			    byte msb = data[currentByte++];
			    byte lsb = data[currentByte++];

			    // Strip bit 16 (MSB) which is a status bit
			    int rawFl = ((msb & 0x7F) << 8) | lsb;

			    // Convert to Flight Level (1 FL = 25 ft)
			    record.FlightLevel_Raw = rawFl / 4;
		    }


			// FRN 22 – I021/152 – Magnetic Heading
			if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
			{
				currentByte += 2;
			}

			// FRN 23 – I021/200 – Target Status
			if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
			{
				currentByte += 1;
			}

			// FRN 24 – I021/155 – Barometric Vertical Rate
			if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
			{
				currentByte += 2;
			}

			// FRN 25 – I021/157 – Geometric Vertical Rate
			if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
			{
				currentByte += 2;
			}

			// FRN 26 – I021/160 – Airborne Ground Vector
			if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(4, recordEnd))
			{
				currentByte += 4;
			}

			// FRN 27 – I021/165 – Track Angle Rate
			if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
			{
				currentByte += 2;
			}

			// --- FRN 28 – I021/166 – Time of Report Transmission ---
			if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(3, recordEnd))
			{
				currentByte += 3; // keep as-is
			}
			
			// --- FRN 29 – I021/170 – Target Identification (6 bytes primary, optional extensions) ---
			if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(6, recordEnd))
			{
				int frn29Start = currentByte;

				// Debug: show primary 6 bytes
				//Console.WriteLine($"[FRN29 DEBUG] Start={frn29Start:X} Next6={BitConverter.ToString(data, frn29Start, 6)}");

				// Decode primary 6 bytes (8 IA-5 characters)
				record.Target_Identification = DecodeTargetIdentification(data, frn29Start, 6);
				currentByte += 6;

				// Check for optional extension bytes (FX = Field Extension bit, bit 1 of last byte of each segment)
				while (currentByte < recordEnd && HasFRN29Extension(data, currentByte - 1))
				{
					int bytesLeft = recordEnd - currentByte;
					int bytesToRead = Math.Min(6, bytesLeft); // decode in 6-byte chunks

					//Console.WriteLine($"[FRN29 EXT DEBUG] Start={currentByte:X} Next6={BitConverter.ToString(data, currentByte, bytesToRead)}");

					// Decode extension segment and append
					string extension = DecodeTargetIdentification(data, currentByte, bytesToRead);
					record.Target_Identification += extension;

					currentByte += bytesToRead;
				}

				//Console.WriteLine($"→ FRN29 decoded = '{record.Target_Identification}'");
			}


            // FRN 30 - I021/020 - Emitter Category (no need to decode)
            if(fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
			{
				currentByte += 1; // Skip Emitter Category
			}
            
            // FRN 31 - I021/220 - Met Information (no need to decode)
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
            {
	            currentByte+= 1; // Skip Met Information
            }
            
            // FRN 32 - I021/146 - Selected Altitude (no need to decode)
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
            {
	            currentByte += 2; // Skip Selected Altitude
            }
            
            // FRN 33 - I021/148 - Final State Selected Altitude (no need to decode)
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
            {
	            currentByte += 2; // Skip Final State Selected Altitude
            }
            
            // FRN 34 - I021/110 - Trajectory Intent (no need to decode)
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
            {
	            // Keep reading until FX bit (bit 1) = 0
	            do
	            {
		            byte b = data[currentByte++];
		            if ((b & 0b00000001) == 0) break; // FX=0 => done
	            } while (currentByte < recordEnd);
            }

            
            // FRN 35 - I021/016 - Service Management (no need to decode)
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
            {
	            currentByte += 1;
            }
            
            // FRN 36 - I021/008 - Aircraft Operational Status (no need to decode)
			if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
			{
				currentByte += 1; // Skip Aircraft Operational Status
			}
			
			// FRN 37 - I021/271 - Surface Capabilities (no need to decode)
			if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
			{
				currentByte += 1; // Skip Surface Capabilities
			}
			
			// FRN 38 - I021/132 - Message Amplitude (no need to decode)
			if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
			{
				currentByte += 1; // Skip ACAS Resolution Advisory Report
			}
			
			// FRN 39 - I021/250 - Mode S MB Data (no need to decode)
			if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
			{
				byte rep = data[currentByte++]; // number of repetitions
				int bytesToSkip = rep * 8;      // each repetition = 8 bytes
				if (CheckBytes(bytesToSkip, recordEnd))
					currentByte += bytesToSkip;
			}

			
			// FRN 40 - I021/260 - ACAS Resolution Advisory Report (no need to decode)
			if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(7, recordEnd))
			{
				currentByte += 7; // Skip ACAS Resolution Advisory Report
			}
			
			// FRN 41 - I021/270 - Reciever ID (no need to decode)
			if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
			{
				currentByte += 1; // Reciever ID
			}
			
			// FRN 42 - I021/280 - Data Ages (no need to decode)
          if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
          {
             // Skip 1..N bytes while FX=1
             do
             {
                byte b = data[currentByte++];
                if ((b & 0b00000001) == 0) break;
             } while (currentByte < recordEnd);
          }

          // --- BEGIN FIX: ALIGNING FSPEC FOR FRN 48 ---
          // We must increment fspecIndex for all fields between 42 and 48.
          // For fields that are defined (like 46 & 47), we must also
          // skip their data if they are present, or the cursor will be wrong.

          // FRN 43 - (Unassigned)
          if (fspecIndex < fspec.Count && fspec[fspecIndex++])
          {
              // This item is not expected in this spec.
              // If this (fspec[42]) is 1, the decoder will likely fail,
              // as we don't know the item's length to skip it.
          }
          
          // FRN 44 - (Unassigned)
          if (fspecIndex < fspec.Count && fspec[fspecIndex++])
          {
              // Increment fspecIndex (for bit 44)
          }
          
          // FRN 45 - (Unassigned)
          if (fspecIndex < fspec.Count && fspec[fspecIndex++])
          {
              // Increment fspecIndex (for bit 45)
          }
          
          // FRN 46 - I021/295 - Reserved Expansion Field (v2.1)
          // You said this is empty, but if its FSPEC bit (fspec[45]) is set,
          // we MUST skip its variable-length data.
          if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
          {
             Console.WriteLine("[DEBUG] Skipping FRN 46 data...");
             do
             {
                byte b = data[currentByte++];
                if ((b & 0x01) == 0) break; // FX=0
             } while (currentByte < recordEnd);
          }
          
          // FRN 47 - I021/276 - Special Purpose Field (v2.1)
          // We must also skip this variable-length (FX) field if present.
          if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
          {
             Console.WriteLine("[DEBUG] Skipping FRN 47 (SPF) data...");
             do
             {
                byte b = data[currentByte++];
                if ((b & 0x01) == 0) break; // FX=0
             } while (currentByte < recordEnd);
          }

          // --- FRN 48 – Reserved Expansion Field (REF) ---
          if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
          {
              // Llegeixo primer el primer octet de REF, per saber com està el bit de BPS
              byte refOctet1 = data[currentByte++];

              // Per your spec: (BPS SelH NAV GAO SGV STA TNH MES)
              bool bpsPresent = (refOctet1 & 0b1000_0000) != 0;
              bool fx = (refOctet1 & 0x01) != 0;

              while (fx && currentByte < recordEnd)
              {
	              byte nextOctet = data[currentByte++];
	              if ((nextOctet & 0b1000_0000) != 0)
		              bpsPresent = true;
	              fx = (nextOctet & 0x01) != 0;
              }


              if (bpsPresent) // Si el bit de BPS està a 1, llegeixo els 2 octets següents (16-13 son palla) / (12-1 són BPS)
              {
	              if (CheckBytes(2, recordEnd))
	              {
		              ushort raw = (ushort)((data[currentByte] << 8) | data[currentByte + 1]);
		              currentByte += 2;
		              ushort bps12bit = (ushort)(raw & 0x0FFF);
		              double bpsValue = 800 + (bps12bit * 0.1);

		              if (bps12bit == 0)
		              {
			              record.BarometricPressureSetting = 800.0;
			              Console.WriteLine($"[REF/BPS] DECODED: BPS raw=0 -> (<= 800.0 hPa)");
		              }
		              else if (bps12bit == 0x0FFF)
		              {
			              record.BarometricPressureSetting = 1209.5;
			              Console.WriteLine($"[REF/BPS] DECODED: BPS raw=4095 -> (>= 1209.5 hPa)");
		              }
		              else
		              {
			              record.BarometricPressureSetting = bpsValue;
			              //Console.WriteLine(
				              //$"[BPS_FOUND] Found BPS={record.BarometricPressureSetting:F1} | At LAT={record.WGS84_Latitude:F5}, LON={record.WGS84_Longitude:F5}");
		              }
	              }
	              else
	              {
		              Console.WriteLine("[REF/BPS] ⚠️ REF indicated BPS, but data ended prematurely.");
	              }
              }
          }
		} 
        
        
        // Em faig aquests mètodes per poder decodificar la FRN29 amb els noms que em dona problema per captar els números dels callsigns
        private static string DecodeTargetIdentification(byte[] data, int startIndex, int length)
        {
	        char[] chars = new char[length * 8 / 6]; // 6-bit IA-5 mapping
	        int bitPos = 0;

	        for (int i = 0; i < chars.Length; i++)
	        {
		        int val = 0;
		        for (int b = 0; b < 6; b++)
		        {
			        int byteIndex = startIndex + (bitPos + b) / 8;
			        int bitIndex = 7 - ((bitPos + b) % 8);
			        int bit = ((data[byteIndex] >> bitIndex) & 1);
			        val = (val << 1) | bit;
		        }
		        chars[i] = IA5Map(val); // map 6-bit value to IA-5 char
		        bitPos += 6;
	        }

	        return new string(chars);
        }

		// Check FX (bit 1 of last byte in segment)
        private static bool HasFRN29Extension(byte[] data, int lastByteIndex)
        {
	        return (data[lastByteIndex] & 0x01) != 0; // FX = 1 → extension exists
        }

		// Map 6-bit IA-5 values to chars
        private static char IA5Map(int val)
        {
	        if (val >= 1 && val <= 26) return (char)('A' + val - 1);
	        if (val >= 48 && val <= 57) return (char)('0' + val - 48); // numeric
	        if (val == 32) return ' ';
	        return ' '; // fallback
        }

        private string DecodeTargetIdentification(int start)
        {
	        // IA-5 character mapping for ASTERIX (Table 3-9)
	        char[] ia5 = new char[64];
	        for (int i = 0; i < 64; i++) ia5[i] = ' ';
	        for (int i = 1; i <= 26; i++) ia5[i] = (char)('A' + i - 1); // A-Z
	        for (int i = 32; i <= 41; i++) ia5[i] = (char)('0' + (i - 32)); // 0-9

	        // Combine six bytes into a 48-bit stream (MSB first)
	        ulong bits = ((ulong)data[start] << 40) |
	                     ((ulong)data[start + 1] << 32) |
	                     ((ulong)data[start + 2] << 24) |
	                     ((ulong)data[start + 3] << 16) |
	                     ((ulong)data[start + 4] << 8) |
	                     ((ulong)data[start + 5]);

	        var sb = new StringBuilder(8);

	        // Extract 8 characters, from MSB (left) to LSB (right)
	        for (int i = 0; i < 8; i++)
	        {
		        int shift = (7 - i) * 6;  // character 1 = top 6 bits
		        int code = (int)((bits >> shift) & 0x3F);
		        sb.Append(ia5[code]);
	        }

	        return sb.ToString().Trim();
        }
    }
}







