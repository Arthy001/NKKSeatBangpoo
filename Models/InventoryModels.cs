namespace NKKSeatBangpoo.Models
{
    public class InventoryStockItem
    {
        public string OrderNo { get; set; }
        public string NHKPartNo { get; set; }
        public string? KanbanNo { get; set; }
        public string PartName { get; set; }
        public int TotalQty { get; set; }
        public int TotalTags { get; set; }
        public DateTime? LastGateEntryTime { get; set; }
    }

    public class InventoryHistoryItem
    {
        public int TransactionID { get; set; }
        public string? TagEPC { get; set; }
        public string? OrderNo { get; set; }
        public string? NHKPartNo { get; set; }
        public string? KanbanNo { get; set; }
        public int? QtyPerSet { get; set; }
        public DateTime? GateEntryTime { get; set; }
        public bool? IsExported { get; set; }
        public string? CardNo { get; set; }
        
        public string StatusText => IsExported == true ? "Exported" : "In-Stock";
        public string StatusColor => IsExported == true ? "#8888AA" : "#44DD66";
    }
}
