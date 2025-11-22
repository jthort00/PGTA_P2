using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using AsterixDecoder.Models.CAT048;
using AsterixDecoder.Models.CAT021;

namespace AsterixDecoder.Models
{
    public class AsterixCombinedList
    {
        private Cat048List cat048List;
        private Cat021List cat021List;

        public AsterixCombinedList(Cat048List cat048, Cat021List cat021)
        {
            cat048List = cat048 ?? new Cat048List();
            cat021List = cat021 ?? new Cat021List();

            // Aplicar inferencia de BP y recalcular altitudes en CAT021
            cat021List.InferMissingBP();
            cat021List.RecomputeCorrectedAltitudes();
        }

        public int Count => cat048List.Count + cat021List.Count;
        
        private DateTime ParseTimeSafe(string t)
        {
            if (string.IsNullOrEmpty(t))
                return DateTime.MaxValue;

            // Convierte "HH:mm:ss:fff" → "HH:mm:ss.fff"
            int lastColon = t.LastIndexOf(':');
            if (lastColon > 0)
                t = t[..lastColon] + "." + t[(lastColon + 1)..];

            return DateTime.TryParseExact(t,
                "HH:mm:ss.fff",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dt)
                ? dt
                : DateTime.MaxValue;
        }


        public string ExportCombinedToCSV()
        {
            var sb = new System.Text.StringBuilder();
            var inv = CultureInfo.InvariantCulture;

            // Cabecera CSV
            sb.AppendLine("CAT;SAC;SIC;Time;Latitude;Longitude;h_wgs84;h_ft;RHO;THETA;Mode3A;Flight_Level;ModeC_Corrected;TA;TI;Mode_S;BP;RA;TTA;GS;TAR;TAS;HDG;IAS;MACH;BAR;IVV;Track_number;Ground_Speedkt;Heading;STAT230");

            // 1️⃣ CREAR LISTA UNIFICADA PARA ORDENAR
            var unified = new List<(DateTime time, object r)>();

            // --- CAT048 ---
            foreach (var r in cat048List.Records)
            {
                var t = ParseTimeSafe(r.Time);
                unified.Add((t, r));
            }

            // --- CAT021 filtrado ---
            foreach (var r in cat021List.Records)
            {
                if (r.IsOnGround) continue;
                if (r.Mode3A == "7777") continue;
                if (!string.IsNullOrEmpty(r.TI) &&
                    (char.IsDigit(r.TI[0]) || r.TI.Length == 3))
                    continue;

                var t = ParseTimeSafe(r.Time);
                unified.Add((t, r));
            }

            // 2️⃣ ORDENAR POR TIEMPO REAL
            var ordered = unified.OrderBy(x => x.time);

            // 3️⃣ ESCRIBIR CSV EN ORDEN REAL
            foreach (var item in ordered)
            {
                var r = item.r;

                if (r is Cat048 c48)
                {
                    sb.Append("CAT048;");
                    sb.Append($"{c48.SAC?.ToString(inv) ?? "NV"};");
                    sb.Append($"{c48.SIC?.ToString(inv) ?? "NV"};");
                    sb.Append($"{c48.Time ?? "NV"};");
                    sb.Append($"{c48.LAT?.ToString("F6", inv) ?? "NV"};");
                    sb.Append($"{c48.LON?.ToString("F6", inv) ?? "NV"};");
                    sb.Append("NV;NV;");
                    sb.Append($"{c48.RHO?.ToString("F1", inv) ?? "NV"};");
                    sb.Append($"{c48.THETA?.ToString("F1", inv) ?? "NV"};");
                    sb.Append($"{c48.Mode3A ?? "NV"};");
                    sb.Append($"{c48.FL?.ToString("F1", inv) ?? "NV"};");
                    sb.Append("NV;");
                    sb.Append($"{c48.TA ?? "NV"};");
                    sb.Append($"{c48.TI ?? "NV"};");
                    sb.Append("NV;");
                    sb.Append($"{c48.BP?.ToString("F1", inv) ?? "NV"};");
                    sb.Append($"{c48.RA?.ToString("F1", inv) ?? "NV"};");
                    sb.Append($"{c48.TTA?.ToString("F1", inv) ?? "NV"};");
                    sb.Append($"{c48.GS?.ToString("F1", inv) ?? "NV"};");
                    sb.Append($"{c48.TAR?.ToString("F1", inv) ?? "NV"};");
                    sb.Append($"{c48.TAS?.ToString("F1", inv) ?? "NV"};");
                    sb.Append($"{c48.HDG?.ToString("F1", inv) ?? "NV"};");
                    sb.Append($"{c48.IAS?.ToString("F1", inv) ?? "NV"};");
                    sb.Append($"{c48.MACH?.ToString("F2", inv) ?? "NV"};");
                    sb.Append($"{c48.BAR?.ToString("F1", inv) ?? "NV"};");
                    sb.Append($"{c48.IVV?.ToString("F1", inv) ?? "NV"};");
                    sb.Append("NV;NV;NV;");
                    sb.AppendLine($"{c48.STAT_230?.ToString() ?? "NV"}");
                }
                else if (r is Cat021 c21)
                {
                    sb.Append("CAT021;");
                    sb.Append($"{c21.SAC};");
                    sb.Append($"{c21.SIC};");
                    sb.Append($"{c21.Time ?? "NV"};");
                    sb.Append($"{c21.LAT.ToString("F6", inv)};");
                    sb.Append($"{c21.LON.ToString("F6", inv)};");
                    sb.Append("NV;NV;NV;NV;");
                    sb.Append($"{c21.Mode3A ?? "NV"};");
                    sb.Append($"{c21.FL.ToString("F1", inv)};");
                    sb.Append($"{c21.ModeC_Corrected.ToString("F1", inv)};");
                    sb.Append($"{c21.TA ?? "NV"};");
                    sb.Append($"{c21.TI ?? "NV"};");
                    sb.Append("NV;");
                    sb.Append($"{c21.BP?.ToString("F1", inv) ?? "NV"};");
                    sb.Append("NV;NV;NV;NV;NV;NV;NV;NV;NV;NV;NV;NV;");
                    sb.AppendLine("NV");
                }
            }

            string filename = $"combined_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            File.WriteAllText(filename, sb.ToString());
            return filename;
        }

    }
}
