using eNote.Application.Common.Search;
using eNote.Domain.Enums;

namespace eNote.Application.Features.Lectures.Search
{
    public class LectureSearchObject : BaseSearchObject
    {
        public string? Name
        {
            get; set;
        }
        public LectureType? LectureType
        {
            get; set;
        }
        public int? CourseId
        {
            get; set;
        }
        public DateTime? From
        {
            get; set;
        }
        public DateTime? To
        {
            get; set;
        }
    }
}
