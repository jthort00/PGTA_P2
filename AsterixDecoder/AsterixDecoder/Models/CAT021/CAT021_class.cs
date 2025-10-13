using System;
using System.Collections.Generic;
using System.Text;

namespace AsterixDecoder.Models.CAT021
{
    /// <summary>
    /// Decodificador ASTERIX CAT021 
    /// </summary>

    public class Cat021Decoder
    {
        private byte[] data;
        private int currentByte;

        public class Cat021Record
        {
            // FRN 1 - I021/010
            public string DataSourceIdentifier { get; set; }

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

        public Cat021Decoder(byte[] asterixData)
        {
            data = asterixData;
            currentByte = 0;
        }

        public List<Cat021Record> Decode()
        {
            var records = new List<Cat021Record>();

            while (currentByte < data.Length)
            {
                // Here we read the category and make sure its CAT021
                if (currentByte >= data.Length) break;
                byte cat = data[currentByte++];
                if (cat != 21) continue;

                // Here we read the length of the record
                if (currentByte + 1 >= data.Length) break;
                int length = data[currentByte] << 8 | data[currentByte + 1];
                currentByte += 2;

                int recordEnd = currentByte + length - 3;
                if (recordEnd > data.Length) break;

                //var fspec = ReadFSPEC();
                var record = new Cat021Record();
                //DecodeRecord(record, fspec, recordEnd);
                records.Add(record);

                currentByte = recordEnd;
            }

            return records;
        }
    }
}

//public List<bool> ReadFSPEC()
//{
//	var fspec = new List<bool>();
//	bool hasExtension = true;

//	while (hasExtension && currentByte < data.length)
//	{
//		byte octet = data[currentByte++];

//		for (int i = 7; i >= 1; i--)
//		{
//			fspec.Add((octet & 1 << i) != 0);
//		}
//	}
//	return fspec;
//}

//private void DecodeRecord(Cat021Record record, List<bool> fspec, int recordEnd)
//{
//	int fspecIndex = 0;

//	// FRN 1 - I021/010
//	if (fspecIndex < fspec.Count && fspec[fspecIndex++] && CheckBytes(2, recordEnd))
//	{
//		int sac = data[currentByte++];
//		int sic = data[currentByte++];
//		record.DataSourceIdentifier = $"SAC:{sac} SIC:{sic}";
//	}
//}





