using System;
using System.Collections.Generic;
using System.Text;

namespace AsterixDecoder.Models
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
			public double WGS84_Latitude {get; set;}
			public double WGS84_Longitude {get; set;}
			
			// FRN 11 - I021/080
			public string Target_Address {get; set;}
		
			// FRN 12 - I021/073
			public double Time_Reception_Position {get; set;}

			// FRN 19 - I021/070
			public string Mode3A_Code {get; set;}

			// FRN21 - I021/145
			public int Flight_Level {get; set;}

			// FRN29 - I021/170
			public string Target_Identification {get; set;}

			// FRN48 - Re-data
			public byte[] Reserved_Expansion_Field {get; set;}
		}





