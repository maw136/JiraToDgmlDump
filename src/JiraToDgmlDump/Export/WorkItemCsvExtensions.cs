using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using JiraToDgmlDump.WorkItemAbstraction;

namespace JiraToDgmlDump.Export
{
    public static class WorkItemCsvExtensions
    {
        public static async Task<string> SaveToCsv(this IEnumerable<WorkItem> workItems)
        {
            if (workItems == null)
                throw new ArgumentNullException(nameof(workItems));

            using var stringWriter = new StringWriter();
            await SaveToCsv(workItems, stringWriter);
            return stringWriter.ToString();
        }

        public static async Task SaveToCsv(this IEnumerable<WorkItem> workItems, string filename)
        {
            if (workItems == null)
                throw new ArgumentNullException(nameof(workItems));

            if (string.IsNullOrEmpty(filename))
                throw new ArgumentException("Value cannot be null or empty.", nameof(filename));

            await using var streamWriter = File.CreateText(filename);
            await SaveToCsv(workItems, streamWriter);
        }

        private static async Task SaveToCsv(this IEnumerable<WorkItem> workItems, TextWriter textWriter)
        {
            if (workItems == null)
                throw new ArgumentNullException(nameof(workItems));

            if (textWriter == null)
                throw new ArgumentNullException(nameof(textWriter));

            await using var csvWriter = new CsvWriter(textWriter, new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = ";" });
            await csvWriter.WriteRecordsAsync(workItems.Select(WorkItemExtensions.ToRow));
        }
    }
}