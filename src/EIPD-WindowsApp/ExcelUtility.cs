using OfficeOpenXml;
using OfficeOpenXml.FormulaParsing.Excel.Functions.RefAndLookup;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

public static class ExcelUtility
{
    public static ExcelPackage CreateExcelWithColumns(string filePath, IEnumerable<string> columnNames,
    params string[] sheetNames)
    {
        // 🔹 Delete file if it exists
        if (File.Exists(filePath))
        {
            //#if DEBUG
            //            while (IsFileLocked(filePath))
            //            {
            //                var result = MessageBox.Show(
            //                    $"The file is currently open:\n\n{filePath}\n\nPlease close it and click Retry.",
            //                    "File In Use",
            //                    MessageBoxButtons.RetryCancel,
            //                    MessageBoxIcon.Warning);

            //                if (result == DialogResult.Cancel)
            //                {
            //                    return null; // stop processing
            //                }
            //            }
            //#endif

            File.Delete(filePath);
        }

        var fileInfo = new FileInfo(filePath);
        var package = new ExcelPackage(fileInfo);

        foreach (var sheetName in sheetNames)
        {
            var worksheet = package.Workbook.Worksheets.Add(sheetName);

            int colIndex = 1;
            foreach (var colName in columnNames)
            {
                worksheet.Cells[1, colIndex].Value = colName;
                colIndex++;
            }

            worksheet.Column(1).Width = 40;
            worksheet.Column(2).Width = 35;
            worksheet.Column(3).Width = 35;
            worksheet.Column(4).Width = 35;
            worksheet.Column(5).Width = 35;
            worksheet.Column(6).Width = 35;


            // Header formatting
            using (var range = worksheet.Cells[1, 1, 1, columnNames.Count()])
            {
                range.Style.Font.Bold = true;

                // Yellow background
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.Yellow);

                // Center align
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }

            // Header row height
            worksheet.Row(1).Height = 25;

            // Vertical alignment for entire worksheet
            worksheet.Cells.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        }

        package.Save();
        return package;
    }

    private static bool IsFileLocked(string filePath)
    {
        try
        {
            using (var stream = File.Open(
                filePath,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.None))
            {
                return false;
            }
        }
        catch (IOException)
        {
            return true;
        }
    }

    public static void SVCWriteOldSheet_EPPlus1(ExcelPackage package, List<Test> lstCautions, string sheetName)
    {
        // Get or create worksheet
        var worksheet = package.Workbook.Worksheets[sheetName]
                        ?? package.Workbook.Worksheets.Add(sheetName);


        int excelRow = worksheet.Dimension?.End.Row + 1 ?? 2;

        foreach (var caution in lstCautions)
        {
            worksheet.Cells[excelRow, 1].Value = caution.Series;
            //worksheet.Cells[excelRow, 2].Value = caution.DMC;
            worksheet.Cells[excelRow, 2].Hyperlink = new Uri(caution.DMCLink);
            worksheet.Cells[excelRow, 2].Value = caution.DMC.Split('.')[0];
            worksheet.Cells[excelRow, 3].Value = caution.Title;
            worksheet.Cells[excelRow, 4].Value = caution.EIPDMatch;

            if (caution.EIPDMatchLink != null)
            {
                worksheet.Cells[excelRow, 4].Hyperlink = new Uri(caution.EIPDMatchLink);
            }

            worksheet.Cells[excelRow, 5].Value = caution.EIPDMatchTitle;
            worksheet.Cells[excelRow, 6].Value = caution.Attempt;
            worksheet.Cells[excelRow, 7].Value = caution.PartOfDMC;
            worksheet.Cells[excelRow, 8].Value = caution.Records;
            worksheet.Cells[excelRow, 9].Value = caution.WordsMatch;
            worksheet.Cells[excelRow, 3].Style.WrapText = true;
            worksheet.Cells[excelRow, 5].Style.WrapText = true;


            // Format row
            worksheet.Row(excelRow).Style.VerticalAlignment =
                ExcelVerticalAlignment.Center;

            excelRow++;
        }
    }
}