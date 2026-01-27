namespace eNote.Application.Features.InstrumentRentals.Requests
{
    public class RentalCreateRequest
    {
        public int InstrumentId { get; set; }
        public string? Note { get; set; }
    }
}
