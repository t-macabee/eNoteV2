namespace eNote.Application.Requests.InstrumentRental
{
    public class RentalCreateRequest
    {
        public int InstrumentId { get; set; }
        public string? Note { get; set; }
    }
}
