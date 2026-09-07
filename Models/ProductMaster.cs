namespace NKKSeatBangpoo.Models
{
    public class ProductMaster
    {
        public int Id { get; set; }
        public string PartCode { get; set; } = string.Empty; // Unique: NHKPartNo + KanbanCode
        public string NHKPartNo { get; set; } = string.Empty;
        public string? PartName { get; set; }
        public string? KanbanCode { get; set; }
        public string? CustomerPartNo { get; set; }
        public string? CustomerPartName { get; set; }
        public string? Model { get; set; }
        public string? Type { get; set; }
        public string? Cover { get; set; }
        public string? Code { get; set; }
        public string? BarcodePI { get; set; }
        public string? LineID { get; set; }
        public string? SeatTypeID { get; set; }
        public string? MainPartFlag { get; set; }
        public string? WeldingBackNo { get; set; }
        public string? WeldingCusionNo { get; set; }
        public string? WeldBack { get; set; }
        public string? WeldCusion { get; set; }
        public string? Sawing { get; set; }
        public bool? IsActive { get; set; } = true;
        public string? Color { get; set; }
        public string? Side { get; set; }
        public string? CustomerName { get; set; }
        public string? GroupStage { get; set; }
        public string? KanbanSet { get; set; }
    }
}
