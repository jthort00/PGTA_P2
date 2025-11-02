using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.IO;
using System.Linq;


namespace AsterixDecoder.Models
{
    /// <summary>
    /// Decodificador ASTERIX CAT021 
    /// </summary>

    public class Cat021Decoder
    {
        private byte[] data;
        private int currentByte;
        private double Actual_QNH; 

        public class Cat021Record
        {
            // FRN 1 - I021/010
            public string DataSourceIdentifier { get; set; }
            
            // FRN 2 - I021/040
            public bool IsOnGround { get; set; }
            public double Real_Altitude_ft { get; set; }
            public string TargetReportDescriptor { get; set; }

            // FRN 7 - I021/131 
            public double WGS84_Latitude { get; set; }
            public double WGS84_Longitude { get; set; }

            // FRN 11 - I021/080
            public string Target_Address { get; set; }

            // FRN 12 - I021/073
            public TimeSpan Time_Reception_Position { get; set; }

            // FRN 19 - I021/070
            public string Mode3A_Code { get; set; }

            // FRN21 - I021/145
            public int Flight_Level { get; set; }

            // FRN29 - I021/170
            public string Target_Identification { get; set; }

            // FRN48 - Re-data
            public byte[] Reserved_Expansion_Field { get; set; }
        }

        public Cat021Decoder(byte[] asterixData, double qnhActual=1013.25)
        {
            data = asterixData;
            currentByte = 0;
            Actual_QNH = qnhActual;
        }

        public List<Cat021Record> Decode()
        {
            var records = new List<Cat021Record>();

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
	            var record = new Cat021Record();
	            DecodeRecord(record, fspec, recordEnd);

                
                //Console.WriteLine($"IsOnGround={record.IsOnGround}");
                Console.WriteLine($"Lat={record.WGS84_Latitude:F5}, Lon={record.WGS84_Longitude:F5}, OnGround={record.IsOnGround}");

                //Applying CAT021 filter and corrections
                // Filter: Only airborne & within Barcelona FIR
                  if (record.IsOnGround) 
	                  continue;
                //
                if (!(record.WGS84_Latitude > 40.9 && record.WGS84_Latitude < 41.7 &&
                       record.WGS84_Longitude > 1.5 && record.WGS84_Longitude < 2.6))
	                  continue;
                
                // Altitude correction using QNH
                if (record.Flight_Level > 0)
                {
	                double indicatedAltitude = record.Flight_Level * 100; // FL100 = 10,000 ft
	                if (indicatedAltitude < 6000)
		                record.Real_Altitude_ft = indicatedAltitude + (Actual_QNH - 1013.25) * 30.0;
	                else
		                record.Real_Altitude_ft = indicatedAltitude;
                }
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
        
        

        private void DecodeRecord(Cat021Record record, List<bool> fspec, int recordEnd)
		{
		    int fspecIndex = 0;

		    Console.WriteLine("\n================ FIRST RECORD DEBUG =================");
		    Console.WriteLine($"FSPEC bits ({fspec.Count}): {string.Join("", fspec.Select(b => b ? "1" : "0"))}");
		    for (int i = 0; i < Math.Min(fspec.Count, 10); i++)
		        Console.WriteLine($"Bit {i+1,2}: FRN{i+1,2} = {(fspec[i] ? 1 : 0)}");
		    Console.WriteLine($"Record starts at byte offset {currentByte}");
		    Console.WriteLine($"RecordEnd={recordEnd}");
		    Console.WriteLine("----------------------------------------------------");

		    // FRN 1 - I021/010 Data Source Identifier
		    if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
		    {
		        int sac = data[currentByte++];
		        int sic = data[currentByte++];
		        record.DataSourceIdentifier = $"SAC:{sac} SIC:{sic}";
		        Console.WriteLine($"After FRN FRN1: currentByte={currentByte}");
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
		        int atp = (octet1 >> 5) & 0x07;
		        int arc = (octet1 >> 3) & 0x03;
		        int rc  = (octet1 >> 2) & 0x01;
		        int rab = (octet1 >> 1) & 0x01;
		        record.TargetReportDescriptor = $"ATP={atp}, ARC={arc}, RC={rc}, RAB={rab}";
		    }

		    // FRN 3 - I021/161 Track Number
		    if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
		    {
		        currentByte += 2;
		        Console.WriteLine($"After FRN FRN3: currentByte={currentByte}");
		    }

		    // FRN 4 - I021/015 Service Identification
		    if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
		    {
		        currentByte += 1;
		        Console.WriteLine($"After FRN FRN4: currentByte={currentByte}");
		    }

		    // FRN 5 - I021/071 Time Applicability Position
		    if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(3, recordEnd))
		    {
		        currentByte += 3;
		        Console.WriteLine($"After FRN FRN5: currentByte={currentByte}");
		    }

		    // FRN 6 - I021/130 Position WGS84 (low-res)
		    if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(6, recordEnd))
		    {
		        currentByte += 6;
		        Console.WriteLine($"After FRN FRN6: currentByte={currentByte}");
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

			    Console.WriteLine($"FRN7 decoded: Lat={record.WGS84_Latitude:F6}, Lon={record.WGS84_Longitude:F6}");
		    }

		    // FRN 8 - I021/072 Time Applicability Velocity
		    if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(3, recordEnd))
		    {
		        currentByte += 3;
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
		        Console.WriteLine($"Target_Address:{record.Target_Address}");
		    }

		    // FRN 12 - I021/073 Time of Reception of Position
		    if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(3, recordEnd))
		    {
		        int timeRaw = (data[currentByte] << 16) | (data[currentByte + 1] << 8) | data[currentByte + 2];
		        record.Time_Reception_Position = TimeSpan.FromSeconds(timeRaw / 128.0);
		        currentByte += 3;
		        Console.WriteLine($"Time_Reception_Position:{record.Time_Reception_Position}");
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
		        currentByte += 1;
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
		    }

		    // FRN 20 - I021/143 Roll Angle
		    if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
		    {
		        currentByte += 2;
		    }

		    // FRN 21 - I021/145 Flight Level
		    if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
		    {
			    byte msb = data[currentByte++];
			    byte lsbFL = data[currentByte++];
			    int rawFL = ((msb & 0x7F) << 8) | lsbFL;
			    record.Flight_Level = rawFL/4; // FL in 25 ft steps
			    record.Real_Altitude_ft = record.Flight_Level * 100; // convert to feet
			    Console.WriteLine($"FRN21 raw bytes @ {currentByte - 2}: {msb:X2} {lsbFL:X2}");
			    Console.WriteLine($"FRN21 decoded: raw={rawFL}, FlightLevel={record.Flight_Level}, Alt_ft={record.Real_Altitude_ft}");
		    }
		   

            
            // FRN 22 - I021/152 - Magnetic Heading (no need to decode)
			if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
			{
				currentByte += 2; // Skip Magnetic heading
			}
			
			// FRN 23 - I021/200 - Target Status (no need to decode)
			if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
			{
				currentByte += 1; // Skip Target Status
			}
			
			// FRN 24 - I021/155 - Barometric VR (no need to decode)
			if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
			{
				currentByte += 2; // Skip Barometric VR
			}
			
			// FRN 25 - I021/157 - Geometric VR (no need to decode)
			if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
			{
				currentByte += 2; // Skip Geometric VR
			}
			
			// FRN 26 - I021/160 - Airborne Ground Vector (no need to decode)
			if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(4, recordEnd))
			{
				currentByte += 4; // Skip Airborne Ground Vector
			}	
			
			// FRN 27 - I021/165 - Track Angle Rate (no need to decode)
			if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
			{
				currentByte += 2; // Skip Track Angle Rate
			}
			
			// FRN 28 - I021/166 - Time Of Report Transmission (no need to decode)
			if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(3, recordEnd))
			{
				currentByte += 3; // Skip Time Of Report Transmission
			}
            
            // FRN 29 - I021/170 - Target Identification
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(6, recordEnd))
            {
                record.Target_Identification = DecodeTargetIdentification(currentByte);
                Console.WriteLine($"FRN29 raw bytes @ {currentByte}: {BitConverter.ToString(data, currentByte, 6)} -> ID='{record.Target_Identification}'");
                currentByte += 6;
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
	            currentByte+= 1; // Skip Trajectory Intent
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
				currentByte += 1; // Skip Mode S MB Data
			}
			
			// FRN 40 - I021/260 - ACAS Resolution Advisory Report (no need to decode)
			if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(7, recordEnd))
			{
				currentByte += 7; // Skip ACAS Resolution Advisory Report
			}
			
			// FRN 41 - I021/270 - Reciever ID (no need to decode)
			if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
			{
				currentByte += 2; // Reciever ID
			}
			
			// FRN 42 - I021/280 - Data Ages (no need to decode)
			if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
			{
				currentByte += 1; // Data Ages
			}


            // FRN 48 - Reserved Expansion Field
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(1, recordEnd))
            {
	            int len = data[currentByte++];
	            record.Reserved_Expansion_Field = new byte[len];
	            Array.Copy(data, currentByte, record.Reserved_Expansion_Field, 0, len);
	            currentByte += len;
            }
        }

        private string DecodeTargetIdentification(int start)
        {
	        // IA-5 mapping for 6-bit values (per ASTERIX IA-5)
	        // index: 0 = space or '@' depending on implementation — here we treat 0 as space
	        char[] ia5 = new char[64];
	        // Fill IA-5 mapping (0..63)
	        // positions 1..26 -> A..Z (1..26)
	        ia5[0] = ' ';
	        for (int i = 1; i <= 26; i++) ia5[i] = (char)('A' + i - 1);
	        // positions 32..41 -> digits 0..9 (per common IA-5 mapping)
	        for (int i = 0; i <= 9; i++) ia5[32 + i] = (char)('0' + i);
	        // fill rest with space to be safe
	        for (int i = 27; i < 32; i++) ia5[i] = ' ';
	        for (int i = 42; i < 64; i++) ia5[i] = ' ';

	        // Read 6 bytes and build 48-bit stream
	        ulong bitString = ((ulong)data[start] << 40) |
	                          ((ulong)data[start + 1] << 32) |
	                          ((ulong)data[start + 2] << 24) |
	                          ((ulong)data[start + 3] << 16) |
	                          ((ulong)data[start + 4] << 8) |
	                          ((ulong)data[start + 5]);

	        var sb = new StringBuilder(8);
	        // Extract eight 6-bit values, left-to-right
	        for (int i = 7; i >= 0; i--)
	        {
		        int charIndex = (int)((bitString >> (i * 6)) & 0x3F);
		        char c = ia5[charIndex];
		        sb.Append(c);
	        }

	        // Trim trailing/leading spaces
	        return sb.ToString().Trim();
        }


		public static void WriteCsv(string filePath, List<Cat021Record> records)
		{
			using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
			{
				writer.WriteLine("DataSourceIdentifier,WGS84_Latitude,WGS84_Longitude,Target_Address,Time_Reception_Position,Mode3A_Code,Flight_Level,Target_Identification,Real_Altitude_ft");
				foreach (var r in records)
				{
					writer.WriteLine($"{r.DataSourceIdentifier},{r.WGS84_Latitude.ToString(CultureInfo.InvariantCulture)},{r.WGS84_Longitude.ToString(CultureInfo.InvariantCulture)},{r.Target_Address},{r.Time_Reception_Position},{r.Mode3A_Code},{r.Flight_Level},{r.Target_Identification},{r.Real_Altitude_ft.ToString("F1", CultureInfo.InvariantCulture)}");
				}
			}
		}
    }
}






