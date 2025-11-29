using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using AsterixDecoder.Models.CAT048;
using AsterixDecoder.Models.CAT021;
using AsterixDecoder.IO;
using MultiCAT6.Utils;

namespace AsterixDecoder.Models
{
    public class AsterixCombinedList
    {
        // Categorías disponibles
        public enum AsterixCategory { CAT048, CAT021 }

        // Registro unificado para UI/Export
        public class AsterixRecord
        {
            public AsterixCategory Category { get; }
            public Cat048? Cat048 { get; }
            public Cat021? Cat021 { get; }

            private AsterixRecord(AsterixCategory category, Cat048? cat048, Cat021? cat021)
            {
                Category = category;
                Cat048 = cat048;
                Cat021 = cat021;
            }

            public static AsterixRecord From(Cat048 value) => new AsterixRecord(AsterixCategory.CAT048, value, null);
            public static AsterixRecord From(Cat021 value) => new AsterixRecord(AsterixCategory.CAT021, null, value);
        }

        private readonly List<AsterixRecord> _records = new List<AsterixRecord>();

        // Acceso directo a listas por categoría cuando haga falta
        private readonly Cat048List cat048List;
        private readonly Cat021List cat021List;

        public AsterixCombinedList()
        {
            cat048List = new Cat048List();
            cat021List = new Cat021List();
        }

        public AsterixCombinedList(Cat048List cat048, Cat021List cat021)
        {
            cat048List = cat048 ?? new Cat048List();
            cat021List = cat021 ?? new Cat021List();

            // Aplicar inferencia de BP y recalcular altitudes en CAT021
            cat021List.InferMissingBP();
            cat021List.RecomputeCorrectedAltitudes();

            // Poblar registros combinados en orden de llegada (concatenado)
            foreach (var r in cat048List.Records) _records.Add(AsterixRecord.From(r));
            foreach (var r in cat021List.Records) _records.Add(AsterixRecord.From(r));
        }

        // Número total de registros combinados
        public int Count => _records.Count;

        // API esperada por las UIs
        public void Add(Cat048 cat048)
        {
            if (cat048 == null) return;
            cat048List.Add(cat048);
            _records.Add(AsterixRecord.From(cat048));
        }

        public void Add(Cat021 cat021)
        {
            if (cat021 == null) return;
            cat021List.Add(cat021);
            _records.Add(AsterixRecord.From(cat021));
        }

        public void Add(AsterixRecord record)
        {
            if (record == null) return;
            _records.Add(record);
            if (record.Category == AsterixCategory.CAT048 && record.Cat048 != null)
                cat048List.Add(record.Cat048);
            else if (record.Category == AsterixCategory.CAT021 && record.Cat021 != null)
                cat021List.Add(record.Cat021);
        }

        public void AddRange(IEnumerable<AsterixRecord> records)
        {
            if (records == null) return;
            foreach (var r in records) Add(r);
        }

        public IReadOnlyList<AsterixRecord> GetAll() => _records;

        public List<Cat048> GetCat048() => _records.Where(r => r.Category == AsterixCategory.CAT048 && r.Cat048 != null)
                                                   .Select(r => r.Cat048)
                                                   .Where(x => x != null)!
                                                   .ToList()!;

        public List<Cat021> GetCat021() => _records.Where(r => r.Category == AsterixCategory.CAT021 && r.Cat021 != null)
                                                   .Select(r => r.Cat021)
                                                   .Where(x => x != null)!
                                                   .ToList()!;

        public (int cat048, int cat021) GetCountsByCategory()
        {
            int c48 = _records.Count(r => r.Category == AsterixCategory.CAT048);
            int c21 = _records.Count(r => r.Category == AsterixCategory.CAT021);
            return (c48, c21);
        }

        // Filtros geográficos combinados sencillos
        public AsterixCombinedList FilterGeographic(double lat1, double lon1, double lat2, double lon2)
        {
            double minLat = Math.Min(lat1, lat2);
            double maxLat = Math.Max(lat1, lat2);
            double minLon = Math.Min(lon1, lon2);
            double maxLon = Math.Max(lon1, lon2);

            var result = new AsterixCombinedList();
            foreach (var r in _records)
            {
                if (r.Category == AsterixCategory.CAT048 && r.Cat048 != null)
                {
                    var c = r.Cat048;
                    if (c.LAT.HasValue && c.LON.HasValue &&
                        c.LAT.Value >= minLat && c.LON.Value >= minLon &&
                        c.LAT.Value <= maxLat && c.LON.Value <= maxLon)
                    {
                        result.Add(c);
                    }
                }
                else if (r.Category == AsterixCategory.CAT021 && r.Cat021 != null)
                {
                    var c = r.Cat021;
                    if (c.LAT >= minLat && c.LON >= minLon && c.LAT <= maxLat && c.LON <= maxLon)
                    {
                        result.Add(c);
                    }
                }
            }
            return result;
        }

        // Filtro FIR Barcelona (coincidente con Cat021.IsWithinBarcelonaFIR)
        public AsterixCombinedList FilterBarcelonaFIRStrict()
        {
            // Caja aproximada del FIR BCN
            const double minLat = 40.9, maxLat = 41.7, minLon = 1.5, maxLon = 2.6;
            return FilterGeographic(minLat, minLon, maxLat, maxLon);
        }

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

        public string ExportCombinedToCSV() => ExportCombinedToCSV(null);

        public string ExportCombinedToCSV(string? outputPath)
        {
            var sb = new System.Text.StringBuilder();
            var inv = CultureInfo.InvariantCulture;

            // Cabecera CSV
            sb.AppendLine("CAT;SAC;SIC;Time;Latitude;Longitude;h_wgs84;h_ft;RHO;THETA;Mode3A;Flight_Level;ModeC_Corrected;TA;TI;Mode_S;BP;RA;TTA;GS;TAR;TAS;HDG;IAS;MACH;BAR;IVV;Track_number;Ground_Speedkt;Heading;STAT230");

            // Ordenar por tiempo real calculado (si no hay tiempo, al final)
            var ordered = _records
                .Select(r => (time: r.Category == AsterixCategory.CAT048 ? ParseTimeSafe(r.Cat048?.Time) : ParseTimeSafe(r.Cat021?.Time), r))
                .OrderBy(x => x.time)
                .Select(x => x.r);

            foreach (var r in ordered)
            {
                if (r.Category == AsterixCategory.CAT048 && r.Cat048 != null)
                {
                    var c48 = r.Cat048;
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
                    sb.Append($"{(c48.FL.HasValue && c48.FL.Value < 60.0 && c48.H.HasValue ? c48.H.Value.ToString("F1", inv) : "")};");
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
                else if (r.Category == AsterixCategory.CAT021 && r.Cat021 != null)
                {
                    var c21 = r.Cat021;
                    sb.Append("CAT021;");
                    sb.Append($"{c21.SAC};");
                    sb.Append($"{c21.SIC};");
                    sb.Append($"{c21.Time ?? "NV"};");
                    sb.Append($"{c21.LAT.ToString("F6", inv)};");
                    sb.Append($"{c21.LON.ToString("F6", inv)};");
                    sb.Append("NV;NV;NV;NV;");
                    sb.Append($"{c21.Mode3A ?? "NV"};");
                    sb.Append($"{c21.FL.ToString("F1", inv)};");
                    sb.Append($"{(c21.FL < 60 && c21.ModeC_Corrected.HasValue ? c21.ModeC_Corrected.Value.ToString("F1", inv) : "")};");
                    sb.Append($"{c21.TA ?? "NV"};");
                    sb.Append($"{c21.TI ?? "NV"};");
                    sb.Append("NV;");
                    sb.Append($"{c21.BP?.ToString("F1", inv) ?? "NV"};");
                    sb.Append("NV;NV;NV;NV;NV;NV;NV;NV;NV;NV;NV;NV;");
                    sb.AppendLine("NV");
                }
            }

            string filename = !string.IsNullOrWhiteSpace(outputPath)
                ? outputPath
                : $"combined_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            File.WriteAllText(filename, sb.ToString());
            return filename;
        }

        // Carga rápida desde archivo .ast (muy básica, para la UI de arrastrar/soltar)
        public static AsterixCombinedList FromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Ruta de archivo vacía", nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Archivo no encontrado", filePath);

            var reader = new BinaryFileReader(filePath);
            var messages = reader.ReadMessages();

            var c48List = new Cat048List();
            var c21List = new Cat021List();

            // Utilidades necesarias para CAT048
            var geoUtils = new GeoUtils();
            // Radar BCN (igual que en interfaz de archivo)
            var radarPos = new CoordinatesWGS84(
                GeoUtils.LatLon2Radians(41, 18, 2.5284, 0),
                GeoUtils.LatLon2Radians(2, 6, 7.4095, 0),
                27.257
            );

            foreach (var msg in messages)
            {
                if (msg.Length < 3) continue;
                byte cat = msg[0];
                try
                {
                    if (cat == 48)
                    {
                        var dec = new Cat048Decoder(msg);
                        var raws = dec.Decode();
                        foreach (var raw in raws)
                        {
                            var item = new Cat048(raw, geoUtils, radarPos);
                            c48List.Add(item);
                        }
                    }
                    else if (cat == 21)
                    {
                        var dec = new Cat021Decoder(msg);
                        var raws = dec.Decode();
                        foreach (var raw in raws)
                        {
                            var item = new Cat021(raw);
                            c21List.Add(item);
                        }
                    }
                }
                catch
                {
                    // Ignorar registros corruptos individuales para robustez
                }
            }

            return new AsterixCombinedList(c48List, c21List);
        }
    }
}
