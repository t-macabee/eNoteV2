namespace eNote.Application.Requests.Instruments
{
    public class InstrumentInsertRequest
    {
        public string Model { get; set; } = null!;
        public string Manufacturer { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
        public byte[]? Image { get; set; }

        public int InstrumentTypeId { get; set; }
        public int MusicShopId { get; set; }
    }
}
