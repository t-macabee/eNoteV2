namespace eNote.Application.Features.Lectures
{
    public class RsvpResponse
    {
        public int LectureId { get; set; }
        public int StudentId { get; set; }

        public bool Confirmed { get; set; }
    }
}
