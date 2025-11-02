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
		        hasExtension = (octet & 0x01) != 0; // LSB=1 means more FSPEC bytes
		        for (int bit = 7; bit >= 1; bit--)
			        fspec.Add((octet & (1 << (bit))) != 0);
	        }
	        return fspec;
        }
        private bool CheckBytes(int bytesNeeded, int recordEnd) =>
	        (currentByte + bytesNeeded) <= recordEnd;

		private void DecodeRecord(Cat021Record record, List<bool> fspec, int recordEnd)
		{
			int fspecIndex = 0;
			
			// FRN 1 - I021/010
			if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
			{
				int sac = data[currentByte++];
				int sic = data[currentByte++];
				record.DataSourceIdentifier = $"SAC:{sac} SIC:{sic}";
			}
          
			// FRN 2 - I021/040  Target Report Descriptor
			if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
			{
				byte octet1 = data[currentByte++];
				byte octet2 = data[currentByte++];
				// Bit 5 of second octet: 0=OnGround, 1=Airborne
				record.IsOnGround = (octet2 & 0x08) != 0;			
			}

			fspecIndex += 4;
			
			// FRN 7 – I021/131 – High-Resolution Position in WGS-84 Coordinates
			if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(8, recordEnd))
			{
				// --- Bytes 1-4 (Contains LONGITUDE in your data) ---
				int rawVal1 = (data[currentByte] << 24) |
				              (data[currentByte + 1] << 16) |
				              (data[currentByte + 2] << 8) |
				              (data[currentByte + 3]);

				// --- Bytes 5-8 (Contains LATITUDE in your data) ---
				int rawVal2 = (data[currentByte + 4] << 24) |
				              (data[currentByte + 5] << 16) |
				              (data[currentByte + 6] << 8) |
				              (data[currentByte + 7]);

				currentByte += 8;

				// *** FIX 1: The LSB for 32-bit signed is 180 / 2^31 ***
				double lsb = 180.0 / Math.Pow(2, 30); 

				// *** FIX 2: Swap assignments to match your data feed ***
				// Your output shows rawLon (rawVal2) is the Latitude.
				record.WGS84_Latitude  = rawVal2 * lsb;
				record.WGS84_Longitude = rawVal1 * lsb;

				Console.WriteLine($"rawLon(Lat)={rawVal2}, rawLat(Lon)={rawVal1}, Lat={record.WGS84_Latitude:F5}, Lon={record.WGS84_Longitude:F5}");
			}

			// ... (Skipping FRNs 8-10) ...
			fspecIndex += 3; // Skip FRN 8, 9, 10 in FSPEC


            // FRN 11 - I021/080 - Target Address
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(3, recordEnd))
            {
	            record.Target_Address = BitConverter.ToString(data, currentByte, 3).Replace("-", "");
	            currentByte += 3;
            }

            // FRN 12 - I021/073 - Time of Reception of Position
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(3, recordEnd))
            {
	            int timeRaw = (data[currentByte] << 16) | (data[currentByte + 1] << 8) | data[currentByte + 2];
	            record.Time_Reception_Position = TimeSpan.FromSeconds(timeRaw / 128.0);
	            currentByte += 3;
            }
            
            fspecIndex += 6; // Skip FRN 13, 14, 15, 16, 17, 18 in FSPEC

            // FRN 19 - I021/070 - Mode 3/A Code
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
            {
                int mode3a = (data[currentByte] << 8) | data[currentByte + 1];
                currentByte += 2;
                record.Mode3A_Code = Convert.ToString(mode3a & 0x0FFF, 8); // octal code
            }
            
            fspecIndex += 1; // Skip FRN 20

            // FRN 21 - I021/145 - Flight Level
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
            {
                int flRaw = (short)((data[currentByte] << 8) | data[currentByte + 1]);
                record.Flight_Level = (int)(flRaw * 0.25); // each unit = 25 ft
                currentByte += 2;
            }
            
            fspecIndex += 7; // Skip FRN 22, 23, 24, 25, 26, 27, 28
            
            // FRN 29 - I021/170 - Target Identification
            if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(6, recordEnd))
            {
                record.Target_Identification = DecodeTargetIdentification(currentByte);
                currentByte += 6;
            }
            
            fspecIndex += 18; // Skip FRN 30...47

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
			const string charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZ#####_###############0123456789######";
			var chars = new StringBuilder();

			StringBuilder sb = new StringBuilder();
			long bitString = ((long)data[start] << 40) |
			                 ((long)data[start + 1] << 32) |
			                 ((long)data[start + 2] << 24) |
			                 ((long)data[start + 3] << 16) |
			                 ((long)data[start + 4] << 8) |
			                 (long)data[start + 5];

			const string charSetStandard = " ABCDEFGHIJKLMNOPQRSTUVWXYZ      0123456789      "; // 64 chars

			for (int i = 7; i >= 0; i--) // 8 characters, from left to right
			{
				int charIndex = (int)((bitString >> (i * 6)) & 0x3F); // 0x3F = 00111111
				if (charIndex < charSetStandard.Length)
				{
					sb.Append(charSetStandard[charIndex]);
				}
			}

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






