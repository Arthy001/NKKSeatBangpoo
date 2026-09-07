namespace NKKSeatBangpoo.Models
{
    public class RfidCardMaster
    {
        public int Id { get; set; }
        public string TagEPC { get; set; } = string.Empty;
        public string? CardNo { get; set; }
        public string? CurrentStatus { get; set; }
        public DateTime? RegisterTime { get; set; }
        public DateTime? GateInTime { get; set; } // SQL had FateInTime, assumed GateInTime
    }
}
