using eNote.Domain.Enums;

namespace eNote.Application.Features.Lectures
{
    public class LectureDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;
        public string Location { get; set; } = null!;
        public LectureType LectureType { get; set; }
        public LectureStatus LectureStatus { get; set; }
        public bool IsCancelled { get; set; }

        public DateTime LectureTime { get; set; }
        public int Duration { get; set; }
        public int? Capacity { get; set; }

        public int AttendeeCount { get; set; }
    }
}
