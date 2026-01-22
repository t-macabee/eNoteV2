namespace eNote.Application.Requests.Instruments
{
    public class InstrumentCreateRequest
    {
        public string Model { get; set; } = null!;
        public string Manufacturer { get; set; } = null!;
        public string? Description { get; set; }
        public string? ImagePath { get; set; }

        public int InstrumentTypeId { get; set; }
        public int MusicShopId { get; set; }
    }
}
