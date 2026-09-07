using ClosedXML.Excel;
using NKKSeatBangpoo.Models;

namespace NKKSeatBangpoo.Services
{
    /// <summary>
    /// Service สำหรับอ่านไฟล์ Excel (MasterData & Work Order Template)
    /// และแปลงข้อมูลเป็น Model Objects
    /// </summary>
    public class ExcelImportService
    {
        // =====================================================================
        // 1. อ่านไฟล์ MasterData (ProductMaster)
        // =====================================================================
        /// <summary>
        /// อ่านไฟล์ MasterData Excel และคืนค่ารายการ ProductMaster
        /// Sheet: "Isuzu Delivery System"
        /// Header อยู่ Row 4, Data เริ่ม Row 5
        /// Col1=NHK Part, Col2=Isuzu Part, Col3=Part Name, Col4=Model, Col12=Kanban
        /// </summary>
        public List<ProductMaster> ReadMasterData(string filePath)
        {
            var products = new List<ProductMaster>();

            using var workbook = new XLWorkbook(filePath);
            var ws = workbook.Worksheet("Isuzu Delivery System");

            if (ws == null)
                throw new Exception("Sheet 'Isuzu Delivery System' not found in MasterData file.");

            var usedRange = ws.RangeUsed();
            if (usedRange == null) return products;

            int lastRow = usedRange.LastRow().RowNumber();

            // Data เริ่มที่ Row 5 (Row 4 = Header)
            for (int row = 2; row <= lastRow; row++)
            {
                string nhkPart = ws.Cell(row, 1).GetFormattedString().Trim();
                string kanban = ws.Cell(row, 12).GetFormattedString().Trim();
                string code = ws.Cell(row, 7).GetFormattedString().Trim(); // Column 7

                // ข้ามแถวว่าง
                if (string.IsNullOrWhiteSpace(nhkPart))
                    continue;

                var product = new ProductMaster
                {
                    NHKPartNo = nhkPart,
                    KanbanCode = kanban,
                    Code = code,
                    PartCode = nhkPart,
                    CustomerPartNo = ws.Cell(row, 2).GetFormattedString().Trim(),
                    PartName = ws.Cell(row, 3).GetFormattedString().Trim(),
                    CustomerPartName = ws.Cell(row, 3).GetFormattedString().Trim(), // ISUZU: Same as PartName
                    Model = ws.Cell(row, 4).GetFormattedString().Trim(),
                    Type = ws.Cell(row, 5).GetFormattedString().Trim(),
                    Cover = ws.Cell(row, 6).GetFormattedString().Trim(),                    
                    BarcodePI = ws.Cell(row, 8).GetFormattedString().Trim(),
                    LineID = ws.Cell(row, 9).GetFormattedString().Trim(),
                    SeatTypeID = ws.Cell(row, 10).GetFormattedString().Trim(),
                    MainPartFlag = ws.Cell(row, 11).GetFormattedString().Trim(),
                    WeldingBackNo = ws.Cell(row, 13).GetFormattedString().Trim(),
                    WeldingCusionNo = ws.Cell(row, 14).GetFormattedString().Trim(),
                    WeldBack = ws.Cell(row, 15).GetFormattedString().Trim(),
                    WeldCusion = ws.Cell(row, 16).GetFormattedString().Trim(),
                    Sawing = ws.Cell(row, 17).GetFormattedString().Trim(),
                    CustomerName = "ISUZU",
                    IsActive = true
                };

                products.Add(product);
            }

            return products;
        }

        /// <summary>
        /// อ่านไฟล์ Toyota Master Kanban Excel
        /// Mapping Columns (A-P)
        /// </summary>
        public List<ProductMaster> ReadToyotaMasterData(string filePath)
        {
            var products = new List<ProductMaster>();

            using var workbook = new XLWorkbook(filePath);
            var ws = workbook.Worksheets.First(); // Toyota usually first sheet

            var usedRange = ws.RangeUsed();
            if (usedRange == null) return products;

            int lastRow = usedRange.LastRow().RowNumber();

            // Data เริ่มที่ Row 2 (Row 1 = Header)
            for (int row = 2; row <= lastRow; row++)
            {
                string code = ws.Cell(row, 1).GetFormattedString().Trim();       // A: Code
                string cusPartNo = ws.Cell(row, 2).GetFormattedString().Trim();  // B: Part No. (Cus)
                string nhkPartNo = ws.Cell(row, 4).GetFormattedString().Trim();  // D: Part No. (NHK)
                string kanbanNo = ws.Cell(row, 11).GetFormattedString().Trim();   // K: Kanban no.

                if (string.IsNullOrWhiteSpace(nhkPartNo)) continue;

                var product = new ProductMaster
                {
                    PartCode = cusPartNo, // For TOYOTA: PartCode = Kanban No
                    NHKPartNo = nhkPartNo,
                    KanbanCode = kanbanNo,
                    Code = code,
                    CustomerPartNo = cusPartNo,
                    CustomerPartName = ws.Cell(row, 3).GetFormattedString().Trim(), // C: Part Name (Cus)
                    PartName = ws.Cell(row, 5).GetFormattedString().Trim(),         // E: Part name (NHK)
                    Cover = ws.Cell(row, 6).GetFormattedString().Trim(),            // F: Cover
                    Color = ws.Cell(row, 7).GetFormattedString().Trim(),            // G: Color
                    Type = ws.Cell(row, 9).GetFormattedString().Trim(),             // I: Part Type
                    Side = ws.Cell(row, 10).GetFormattedString().Trim(),            // J: Side
                    CustomerName = "TOYOTA",
                    GroupStage = ws.Cell(row, 13).GetFormattedString().Trim(),      // M: Group Stage
                    KanbanSet = ws.Cell(row, 15).GetFormattedString().Trim(),       // O: Kanban Set
                    Model = ws.Cell(row, 16).GetFormattedString().Trim(),           // P: Model
                    IsActive = true
                };

                products.Add(product);
            }

            return products;
        }

        // =====================================================================
        // 2. อ่านไฟล์ Work Order Template
        // =====================================================================
        /// <summary>
        /// อ่านไฟล์ Work Order Template Excel
        /// คืนค่า Tuple ของ (WorkOrderHeader, List of WorkOrderDetail)
        /// 
        /// Layout:
        ///   Row 4, Col3  = Work Center
        ///   Row 5, Col3  = Due Date
        ///   Row 6, Col3  = Order No.
        ///   Row 8, Col3  = Order Date
        ///   Row 13       = Detail Header
        ///   Row 14       = Separator (----)
        ///   Row 15+      = Detail Data (2 rows per item: data row + part name row)
        /// </summary>
        public (WorkOrderHeader Header, List<WorkOrderDetail> Details) ReadWorkOrder(string filePath)
        {
            using var workbook = new XLWorkbook(filePath);
            var ws = workbook.Worksheets.First();

            // --- อ่าน Header ---
            string workCenter = ws.Cell(4, 3).GetFormattedString().Trim();
            string dueDateStr = ws.Cell(5, 3).GetFormattedString().Trim();
            string orderNo = ws.Cell(6, 3).GetFormattedString().Trim();
            string orderDateStr = ws.Cell(8, 3).GetFormattedString().Trim();

            var header = new WorkOrderHeader
            {
                OrderNo = orderNo,
                WorkCenter = workCenter,
                DueDate = ParseFlexibleDate(dueDateStr),
                OrderDate = ParseFlexibleDate(orderDateStr), // เก็บ Order Date
                ImportTimestamp = DateTime.Now,
                Status = "Open"
            };

            // --- อ่าน Detail ---
            var details = new List<WorkOrderDetail>();

            var usedRange = ws.RangeUsed();
            if (usedRange == null) return (header, details);

            int lastRow = usedRange.LastRow().RowNumber();

            // Detail data เริ่มที่ Row 15 (Row 13 = Header, Row 14 = separator)
            for (int row = 15; row <= lastRow; row++)
            {
                string lineNoStr = ws.Cell(row, 2).GetFormattedString().Trim();

                // ถ้า Column 2 (Ln.) มีค่า แสดงว่าเป็น data row
                if (!string.IsNullOrWhiteSpace(lineNoStr) && int.TryParse(lineNoStr, out int lineNo))
                {
                    string codeNo = ws.Cell(row, 1).GetFormattedString().Trim();
                    string itemNumber = ws.Cell(row, 3).GetFormattedString().Trim();
                    string itemName = ws.Cell(row + 1, 3).GetFormattedString().Trim(); // ชื่อพาร์ทอยู่บรรทัดถัดไป คอลัมน์เดียวกัน
                    string slipStr = ws.Cell(row, 4).GetFormattedString().Trim();
                    string kanbanNo = ws.Cell(row, 5).GetFormattedString().Trim();
                    string kbNo = ws.Cell(row, 6).GetFormattedString().Trim();
                    string kanbanQtyStr = ws.Cell(row, 7).GetFormattedString().Trim();
                    string palletQtyStr = ws.Cell(row, 8).GetFormattedString().Trim();
                    string productionQtyStr = ws.Cell(row, 10).GetFormattedString().Trim(); // Col J = Production QTY
                    string packageStr = ws.Cell(row, 11).GetFormattedString().Trim();      // Col K = Package

                    int.TryParse(slipStr, out int slip);
                    int.TryParse(kanbanQtyStr, out int kanbanQty);
                    int.TryParse(palletQtyStr, out int palletQty);
                    int.TryParse(productionQtyStr, out int targetQty);

                    var detail = new WorkOrderDetail
                    {
                        OrderNo = orderNo,
                        NHKPartNo = itemNumber, 
                        CodeNo = codeNo,
                        LineNo = lineNo,
                        ItemNumber = itemNumber,
                        ItemName = itemName,
                        Slip = slip,
                        KanbanNo = kanbanNo,
                        KBNo = kbNo,
                        KanbanQty = kanbanQty,
                        PalletQty = palletQty,
                        TargetQty = targetQty > 0 ? targetQty : (palletQty > 0 ? palletQty : 1),
                        RegisteredQty = 0,
                        Package = packageStr,
                        Status = "Pending"
                    };

                    details.Add(detail);
                }

            }

            return (header, details);
        }

        // =====================================================================
        // Helper: แปลงวันที่แบบยืดหยุ่น
        // =====================================================================
        private DateTime ParseFlexibleDate(string dateStr)
        {
            // ลองแปลงหลายรูปแบบ เช่น "8-25-2025", "2025-08-25"
            string[] formats = new[]
            {
                "M-dd-yyyy", "M-d-yyyy", "MM-dd-yyyy",
                "yyyy-MM-dd", "dd/MM/yyyy", "M/d/yyyy"
            };

            if (DateTime.TryParseExact(dateStr, formats,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out DateTime result))
            {
                return result;
            }

            // Fallback: ลอง Parse ตรงๆ
            if (DateTime.TryParse(dateStr, out result))
                return result;

            return DateTime.Now; // Default ถ้าแปลงไม่ได้
        }
    }
}
