namespace NKKSeatBangpoo.Models
{
    public class WorkOrderHeader
    {
        public int Id { get; set; }
        public string OrderNo { get; set; } = string.Empty;
        public string? WorkCenter { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? OrderDate { get; set; } // New field
        public DateTime? ImportTimestamp { get; set; } = DateTime.Now; // New field
        public string? Status { get; set; } = "Open";
        public DateTime? ImportDate { get; set; } // Legacy or manual field
    }
}
