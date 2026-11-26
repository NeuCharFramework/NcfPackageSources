/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: EpPlusExtension.cs
    File description: EpPlusExtension related implementation

    Created by: Senparc - 20211124

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.12.16 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.OfficeExtension
{

    /// <summary>
    /// Provides helpers for reading EPPlus workbooks and converting worksheet content to text.
    /// </summary>
    public static class EpPlusExtension
    {
        /// <summary>
        /// Opens an Excel workbook and returns its first worksheet.
        /// </summary>
        /// <param name="fileName">The path to the workbook file.</param>
        /// <returns>The first worksheet in the workbook.</returns>
        /// <exception cref="ArgumentException">Thrown when the file does not exist.</exception>
        public static ExcelWorksheet GetWorksheet(string fileName)
        {
            if (!File.Exists(fileName))
            {
                throw new ArgumentException("The path is empty.");
            }
            var fileInfo = new FileInfo(fileName);
            if (!fileInfo.Exists)
            {
                throw new NullReferenceException("file not found");
            }
            var excelPackage = new ExcelPackage(fileInfo);
            if (excelPackage.Workbook.Worksheets.Count <= 0)
            {
                throw new NullReferenceException("worksheet not found");
            }
            return excelPackage.Workbook.Worksheets.FirstOrDefault();
        }

        /// <summary>
        /// Reads the first worksheet from an in-memory workbook stream.
        /// </summary>
        /// <param name="stream">The workbook stream.</param>
        /// <returns>The first worksheet in the workbook.</returns>
        public static ExcelWorksheet GetWorksheet(MemoryStream stream)
        {
            var excelPackage = new ExcelPackage(stream);
            if (excelPackage.Workbook.Worksheets.Count <= 0)
            {
                throw new NullReferenceException("worksheet not found");
            }
            return excelPackage.Workbook.Worksheets.FirstOrDefault();
        }

        /// <summary>
        /// Copies an uploaded workbook into memory and returns its first worksheet.
        /// </summary>
        /// <param name="file">The uploaded workbook.</param>
        /// <returns>The first worksheet in the workbook.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="file"/> is <see langword="null"/>.</exception>
        public static async Task<ExcelWorksheet> GetWorksheetAsync(IFormFile file)
        {
            var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream).ConfigureAwait(false);
            return GetWorksheet(memoryStream);
        }

        /// <summary>
        /// Converts the populated worksheet range to tab-delimited text.
        /// </summary>
        /// <param name="worksheet">The worksheet to read.</param>
        /// <returns>The worksheet content as rows separated by new lines.</returns>
        public static string ReadToString(ExcelWorksheet worksheet)
        {
            var rowCount = worksheet.Dimension?.Rows;
            var colCount = worksheet.Dimension?.Columns;

            if (!rowCount.HasValue || !colCount.HasValue)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            for (int row = 1; row <= rowCount.Value; row++)
            {
                for (int col = 1; col <= colCount.Value; col++)
                {
                    sb.AppendFormat("{0}\t", worksheet.Cells[row, col].Value);
                }
                sb.Append(Environment.NewLine);
            }
            return sb.ToString();
        }
    }
}
