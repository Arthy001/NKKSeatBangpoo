namespace NKKSeatBangpoo.Models
{
    public class WorkOrderDetail
    {
        public int Id { get; set; }
        public string OrderNo { get; set; } = string.Empty;
        public string PartCode { get; set; } = string.Empty;
        public string? CodeNo { get; set; }
        public int? LineNo { get; set; }
        public string? ItemNumber { get; set; }
        public string? ItemName { get; set; }
        public int? Slip { get; set; }
        public string? KanbanNo { get; set; }
        public string? KBNo { get; set; }
        public int? KanbanQty { get; set; }
        public int? PalletQty { get; set; }
        public int? TargetQty { get; set; }
        public int? RegisteredQty { get; set; } = 0;
        public string? Package { get; set; }
        public string? Status { get; set; } = "Pending";
        public string? CustomerPartNo { get; set; }
        
        // Not in DB (For View)
        public string? PartName { get; set; }
        public string? NHKPartNo { get; set; }
        public string? CardNo { get; set; }
        public string? TagEPC { get; set; }
        public string? WorkCenter { get; set; }
    }
}
