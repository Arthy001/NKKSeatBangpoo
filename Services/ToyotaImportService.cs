using NKKSeatBangpoo.Models;
using System.Globalization;
using System.Text;

namespace NKKSeatBangpoo.Services
{
    public class ToyotaImportService
    {
        public (WorkOrderHeader Header, List<WorkOrderDetail> Details) ReadToyotaWorkOrder(string filePath)
        {
            var details = new List<WorkOrderDetail>();
            WorkOrderHeader? header = null;

            // Toyota file is likely a text file with Pipe '|' separator
            // Use UTF-8 or Default encoding
            var lines = File.ReadAllLines(filePath, Encoding.UTF8);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var columns = line.Split('|');
                if (columns.Length < 44) continue; // Minimum columns based on sample

                // Positions based on User's 1-based indexing (converted to 0-based)
                // 6  -> Index 5  : Work Center
                // 10 -> Index 9  : Order No
                // 11 -> Index 10 : Order Date
                // 40 -> Index 39 : NHK Part No
                // 43 -> Index 42 : Kanban No
                // 44 -> Index 43 : Target Qty

                string orderNo = columns[9].Trim();
                string workCenter = columns[5].Trim();
                string orderDateStr = columns[10].Trim();
                
                string customerPartNo = columns[38].Trim();
                string nhkPartNo = customerPartNo;
                string itemName = columns[39];
                string codeNo = columns[40].Trim(); // col 40
                string kanbanNo = columns[41].Trim(); // col 41
                string targetQtyStr = columns[43].Trim();

                // Initialize Header from the first valid line
                if (header == null)
                {
                    header = new WorkOrderHeader
                    {
                        OrderNo = orderNo,
                        WorkCenter = workCenter,
                        OrderDate = ParseToyotaDate(orderDateStr),
                        ImportTimestamp = DateTime.Now,
                        Status = "Open"
                    };
                }

                int.TryParse(targetQtyStr, out int targetQty);

                int lineNo = details.Count + 1; // Auto-increment line number
                details.Add(new WorkOrderDetail
                {
                    OrderNo = orderNo,
                    CustomerPartNo = customerPartNo,
                    NHKPartNo = nhkPartNo,
                    ItemNumber = customerPartNo,
                    ItemName = itemName,
                    CodeNo = codeNo,
                    KanbanNo = kanbanNo,
                    LineNo = lineNo,
                    TargetQty = targetQty > 0 ? targetQty : 1, // Default to 1 if parsing fails
                    RegisteredQty = 0,
                    Status = "Pending"
                });
            }

            if (header == null)
                throw new Exception("ไม่พบข้อมูล Work Order ในไฟล์ที่เลือก");

            return (header, details);
        }

        private DateTime ParseToyotaDate(string dateStr)
        {
            // Format 20/06/2023
            if (DateTime.TryParseExact(dateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
            {
                return result;
            }
            return DateTime.Now;
        }
    }
}
