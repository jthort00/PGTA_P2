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

        public string ExportCombinedToCSV()
        {
            var sb = new System.Text.StringBuilder();
            var inv = CultureInfo.InvariantCulture;

            // Cabecera CSV
            sb.AppendLine("CAT;SAC;SIC;Time;Latitude;Longitude;h_wgs84;h_ft;RHO;THETA;Mode3A;Flight_Level;ModeC_Corrected;TA;TI;Mode_S;BP;RA;TTA;GS;TAR;TAS;HDG;IAS;MACH;BAR;IVV;Track_number;Ground_Speedkt;Heading;STAT230");

            // Combinar registros
            var allRecords = new List<object>();
            allRecords.AddRange(cat048List.Records.Cast<object>());
            allRecords.AddRange(
                cat021List.Records.Where(r =>
                    !r.IsOnGround &&
                    r.Mode3A != "7777" &&
                    !string.IsNullOrEmpty(r.TI) &&
                    !(char.IsDigit(r.TI[0]) || r.TI.Length == 3)
                ).Cast<object>()
            );

            // Ordenar por tiempo
            var orderedRecords = allRecords.OrderBy(r =>
            {
                string timeStr = (r is Cat048 c48) ? c48.Time : ((Cat021)r).Time;
                if (string.IsNullOrEmpty(timeStr)) return DateTime.MaxValue;

                int lastColon = timeStr.LastIndexOf(':');
                if (lastColon >= 0)
                    timeStr = timeStr.Substring(0, lastColon) + "." + timeStr.Substring(lastColon + 1);

                return DateTime.TryParseExact(timeStr, "HH:mm:ss.fff", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
                    ? dt
                    : DateTime.MaxValue;
            });

            // Escribir CSV
            foreach (var r in orderedRecords)
            {
                if (r is Cat048 c48)
                {
                    sb.Append("CAT048;");
                    sb.Append($"{c48.SAC?.ToString(inv) ?? "NV"};");
                    sb.Append($"{c48.SIC?.ToString(inv) ?? "NV"};");
                    sb.Append($"{c48.Time ?? "NV"};");
                    sb.Append($"{c48.LAT?.ToString("F6", inv) ?? "NV"};");
                    sb.Append($"{c48.LON?.ToString("F6", inv) ?? "NV"};");
                    sb.Append("NV;NV;"); // h_wgs84; h_ft
                    sb.Append($"{c48.RHO?.ToString("F1", inv) ?? "NV"};");
                    sb.Append($"{c48.THETA?.ToString("F1", inv) ?? "NV"};");
                    sb.Append($"{c48.Mode3A ?? "NV"};");
                    sb.Append($"{c48.FL?.ToString("F1", inv) ?? "NV"};");
                    sb.Append("NV;"); // ModeC_Corrected
                    sb.Append($"{c48.TA ?? "NV"};");
                    sb.Append($"{c48.TI ?? "NV"};");
                    sb.Append("NV;"); // Mode_S
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
                    sb.Append("NV;NV;NV;"); // Track_number; Ground_Speedkt; Heading
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
                    sb.Append("NV;NV;NV;NV;"); // h_wgs84, h_ft, RHO, THETA
                    sb.Append($"{c21.Mode3A ?? "NV"};");
                    sb.Append($"{c21.FL.ToString("F1", inv)};");
                    sb.Append($"{c21.ModeC_Corrected.ToString("F1", inv)};");
                    sb.Append($"{c21.TA ?? "NV"};");
                    sb.Append($"{c21.TI ?? "NV"};");
                    sb.Append("NV;"); // Mode_S
                    sb.Append($"{c21.BP?.ToString("F1", inv) ?? "NV"};");
                    sb.Append("NV;NV;NV;NV;NV;NV;NV;NV;NV;NV;NV;NV;"); // Campos CAT048
                    sb.AppendLine("NV"); // STAT230 no aplica
                }
            }

            string filename = $"combined_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            File.WriteAllText(filename, sb.ToString());
            return filename;
        }
    }
}
