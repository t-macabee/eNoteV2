using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.InstrumentRentals
{
    public class RentalCreateRequest
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int InstrumentId
        {
            get; set;
        }
        public string? Note
        {
            get; set;
        }
    }
}
