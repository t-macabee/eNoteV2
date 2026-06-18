using eNote.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace eNote.Application.Features.Lectures
{
    public class LectureCreateRequest
    {
        [Required]
        public string Name { get; set; } = null!;
        [Required]
        public string Location { get; set; } = null!;
        [Required]
        public LectureType LectureType { get; set; }

        [Required]
        public DateTime LectureTime { get; set; }
        [Required]
        public int Duration { get; set; }
        public int? Capacity { get; set; }

        public int? CourseId { get; set; }
    }
}
