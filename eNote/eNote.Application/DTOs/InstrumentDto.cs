namespace eNote.Application.DTOs
{
    public class InstrumentDto
    {
        public int Id { get; set; }
        public string Model { get; set; } = null!;
        public string Manufacturer { get; set; } = null!;
        public string? Description { get; set; }
        public string InstrumentType { get; set; } = null!;
        public string MusicShop { get; set; } = null!;
        public string? ImagePath { get; set; }
        public bool IsAvailable { get; set; }
    }
}
