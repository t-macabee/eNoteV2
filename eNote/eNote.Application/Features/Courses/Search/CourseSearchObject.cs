using eNote.Application.Common.Search;

namespace eNote.Application.Features.Courses.Search
{
    public class CourseSearchObject : BaseSearchObject
    {
        public string? Name
        {
            get; set;
        }
        public bool? IsPublished
        {
            get; set;
        }
    }
}
