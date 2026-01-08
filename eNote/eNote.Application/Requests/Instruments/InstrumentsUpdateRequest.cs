namespace eNote.Application.Requests.Instruments
{
    public class InstrumentUpdateRequest
    {
        public string? Model { get; set; }
        public string? Description { get; set; }
        public byte[]? Image { get; set; }
        public bool? IsAvailable { get; set; }
    }
}
