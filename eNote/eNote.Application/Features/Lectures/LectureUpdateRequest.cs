using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Lectures
{
    public class LectureUpdateRequest
    {
        [Required]
        public string Name { get; set; } = null!;

        [Required]
        public string Location { get; set; } = null!;

        [Required]
        public DateTime LectureTime { get; set; }

        [Required]
        public int Duration { get; set; }

        public int? Capacity { get; set; }
    }
}
