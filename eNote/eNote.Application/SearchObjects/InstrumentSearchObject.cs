namespace eNote.Application.SearchObjects
{
    public class InstrumentSearchObject : BaseSearchObject
    {
        public string? Model { get; set; }
        public string? Manufacturer { get; set; }
        public bool? IsAvailable { get; set; }
        public int? InstrumentTypeId { get; set; }
        public int? MusicShopId { get; set; }
    }
}
