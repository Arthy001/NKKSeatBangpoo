using ClosedXML.Excel;

string templateDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "Templates");
string outputFile = Path.Combine(Directory.GetCurrentDirectory(), "..", "TempReader", "ExcelPeek.txt");

using var writer = new StreamWriter(outputFile);

writer.WriteLine("=== Scanning Excel files in Templates folder ===\n");

foreach (var file in Directory.GetFiles(templateDir, "*.xlsx"))
{
    writer.WriteLine($"========================================");
    writer.WriteLine($"FILE: {Path.GetFileName(file)}");
    writer.WriteLine($"========================================");

    using var workbook = new XLWorkbook(file);

    foreach (var ws in workbook.Worksheets)
    {
        writer.WriteLine($"\n--- Sheet: \"{ws.Name}\" ---");
        writer.WriteLine($"    Used Range: {ws.RangeUsed()?.RangeAddress}");

        var usedRange = ws.RangeUsed();
        if (usedRange == null)
        {
            writer.WriteLine("    (empty sheet)");
            continue;
        }

        int maxRow = Math.Min(usedRange.LastRow().RowNumber(), 25); // show first 25 rows
        int maxCol = usedRange.LastColumn().ColumnNumber();

        for (int row = 1; row <= maxRow; row++)
        {
            var cells = new List<string>();
            for (int col = 1; col <= maxCol; col++)
            {
                var cell = ws.Cell(row, col);
                string val = cell.GetFormattedString();
                if (!string.IsNullOrWhiteSpace(val))
                    cells.Add($"[Col{col}]{val}");
            }
            if (cells.Count > 0)
                writer.WriteLine($"    Row {row,2}: {string.Join(" | ", cells)}");
        }

        writer.WriteLine($"    ... Total rows in sheet: {usedRange.LastRow().RowNumber()}, Total cols: {maxCol}");
    }
    writer.WriteLine();
}

Console.WriteLine($"Output written to: {outputFile}");
