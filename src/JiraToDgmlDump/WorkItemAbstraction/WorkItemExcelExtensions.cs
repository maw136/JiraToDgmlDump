using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;

namespace JiraToDgmlDump
{
    public static class WorkItemExcelExtensions
    {
        public static Task SaveToExcel(this IEnumerable<WorkItem> workItems, string filename)
        {
            if (workItems == null)
                throw new ArgumentNullException(nameof(workItems));

            if (string.IsNullOrEmpty(filename))
                throw new ArgumentException("Value cannot be null or empty.", nameof(filename));
            return Task.Run(() =>
            {
                using var stream = File.Create(filename);
                SaveToExcel(workItems, stream);
            });
        }

        private static void SaveToExcel(this IEnumerable<WorkItem> workItems, Stream stream)
        {
            if (workItems == null)
                throw new ArgumentNullException(nameof(workItems));

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("jira");

            worksheet.Cell(0, 0).InsertData(workItems.Select(WorkItemExtensions.ToRow));

            workbook.SaveAs(stream,
                new SaveOptions
                {
                    ValidatePackage = true,
                    EvaluateFormulasBeforeSaving = true,
                    ConsolidateConditionalFormatRanges = true,
                    ConsolidateDataValidationRanges = true
                });
        }
    }
}