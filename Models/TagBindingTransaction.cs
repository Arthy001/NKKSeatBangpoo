namespace NKKSeatBangpoo.Models
{
    public class TagBindingTransaction
    {
        public int Id { get; set; }
        public int TransactionID { get; set; }
        public string? TagEPC { get; set; }
        public string? PartCode { get; set; } 
        public int? QtyPerSet { get; set; }
        public DateTime? BindingTime { get; set; }
        public DateTime? GateEntryTime { get; set; }
        public bool? IsExported { get; set; }
        public string? ExportFileName { get; set; }
        public int? WorkOrderDetailId { get; set; }

        // Joined items
        public string? OrderNo { get; set; }
        public string? NHKPartNo { get; set; }
        public string? PartName { get; set; }
        public int? LineNo { get; set; }
        public string? KanbanNo { get; set; }
        public string? WorkCenter { get; set; }
        public string? CardNo { get; set; }
        public string? ItemNumber { get; set; }
        public string? ItemName { get; set; }
        public string? CodeNo { get; set; }
        public string? KBNo { get; set; }
        
        public string? KanbanCode { get; set; }
        public string? CustomerPartNo { get; set; }
        public string? KanbanSet { get; set; }
    }
}
