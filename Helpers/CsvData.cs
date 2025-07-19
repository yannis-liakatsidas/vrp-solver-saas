using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace Helpers
{
    public class CsvData
    {
        public required string Timestamp { get; set; }
        public required string ServiceName { get; set; }
        public required string ActionPerformed { get; set; }
        public required int JobId { get; set; }

        //public class CsvDataMap : ClassMap<CsvData>
        //{
        //    public CsvDataMap()
        //    {
        //        Map(m => m.Timestamp).Index(0).Name("Timestamp");
        //        Map(m => m.ServiceName).Index(1).Name("ServiceName");
        //        Map(m => m.ActionPerformed).Index(1).Name("ActionPerformed");
        //        Map(m => m.JobId).Index(1).Name("JobIteration");
        //    }
        //}

        public static List<CsvData> LoadCsvFile(FileStream fileStream)
        {
            using (var reader = new StreamReader(fileStream))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                return csv.GetRecords<CsvData>().ToList();
            }
        }

        public static void SaveCsvFile(string filePath, IEnumerable<CsvData> records)
        {
            using (var writer = new StreamWriter(filePath))
            using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)))
            {
                csv.WriteRecords(records);
            }
        }
        public static string SerializeCsvData(List<CsvData> csvData)
        {
            using (var writer = new StringWriter())
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(csvData);
                return writer.ToString();
            }
        }
        public static string SerializeCsvData(CsvData csvData)
        {
            using (var writer = new StringWriter())
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecord(csvData);
                csv.Flush();
                writer.Flush(); // Explicitly flush the StringWriter
                return writer.ToString();
            }
        }

        //public static List<CsvData> DeserializeCsvData(string csvString)
        //{
        //    using (var reader = new StringReader(csvString))
        //    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        //    {
        //        // Configure CsvReader to map CSV columns to CsvData properties
        //        csv.Context.RegisterClassMap<CsvDataMap>();

        //        // Read CSV records into a list of CsvData objects
        //        var csvDataList = csv.GetRecords<CsvData>().ToList();
        //        return csvDataList;
        //    }
        //}
    }
}
