using Microsoft.Maui.Graphics;


namespace NKKSeatBangpoo.Models
{
    public class OrderHeaderVM
    {
        public string OrderNo { get; set; } = string.Empty;
        public string WorkCenter { get; set; } = string.Empty;
        public string Status { get; set; } = "Open";
        public DateTime? DueDate { get; set; }
        public string DueDateStr { get; set; } = string.Empty;

        public Color StatusColor => Status switch
        {
            "Open" => Color.FromArgb("#4444AA"),
            "In-Progress" => Color.FromArgb("#AA8800"),
            "Closed" => Color.FromArgb("#44AA66"),
            _ => Color.FromArgb("#555555")
        };

        public Color StatusBgColor => Status switch
        {
            "Open" => Color.FromArgb("#2A2A6E"),
            "In-Progress" => Color.FromArgb("#5A4400"),
            "Closed" => Color.FromArgb("#1A4A2A"),
            _ => Color.FromArgb("#333333")
        };

        public OrderHeaderVM(WorkOrderHeader h)
        {
            OrderNo = h.OrderNo;
            WorkCenter = h.WorkCenter ?? "-";
            Status = h.Status ?? "Open";
            DueDate = h.DueDate;
            DueDateStr = h.DueDate.HasValue ? $"Due: {h.DueDate.Value:dd MMM yyyy}" : "No Due Date";
        }
    }

    public class OrderDetailVM : WorkOrderDetail
    {
        public int RowIndex { get; set; }
        public string PartName { get; set; } = "-";
        public string ProgressText => $"{RegisteredQty ?? 0} / {TargetQty ?? 0}";
        public double ProgressVal => (TargetQty ?? 0) > 0 ? (double)(RegisteredQty ?? 0) / (TargetQty ?? 0) : 0;
        
        public int RemainingQty => (TargetQty ?? 0) - (RegisteredQty ?? 0);
        public Color RemainingColor => RemainingQty <= 0
            ? Color.FromArgb("#FF8844")
            : Color.FromArgb("#FFFFFF");

        public Color StatusBgColor => Status switch
        {
            "Pending" => Color.FromArgb("#2A2A4E"),
            "In-Progress" => Color.FromArgb("#4A3A00"),
            "Completed" => Color.FromArgb("#1A4A2A"),
            _ => Color.FromArgb("#333333")
        };


        public OrderDetailVM(WorkOrderDetail d)
        {
            Id = d.Id;
            OrderNo = d.OrderNo;
            PartCode = d.PartCode;
            CodeNo = d.CodeNo;
            LineNo = d.LineNo;
            ItemNumber = d.ItemNumber;
            ItemName = d.ItemName;
            Slip = d.Slip;
            KanbanNo = d.KanbanNo;
            KBNo = d.KBNo;
            KanbanQty = d.KanbanQty;
            PalletQty = d.PalletQty;
            TargetQty = d.TargetQty;
            RegisteredQty = d.RegisteredQty;
            Package = d.Package;
            Status = d.Status;
            
            // Map the joined fields
            NHKPartNo = d.NHKPartNo;
            PartName = d.PartName ?? "-";
            CardNo = d.CardNo;
            TagEPC = d.TagEPC;
            WorkCenter = d.WorkCenter;
        }
    }
}
