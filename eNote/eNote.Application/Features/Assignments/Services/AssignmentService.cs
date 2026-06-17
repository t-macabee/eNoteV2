using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Assignments;
using eNote.Application.Features.Users.Services;
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Assignments.Services
{
    public class AssignmentService(IAppDbContext context, IClock clock, IUserContextResolver resolver, ICurrentUserService currentUserService) : IAssignmentService
    {
        public async Task<PagedResult<AssignmentDto>> GetForLectureAsync(int lectureId, AssignmentSearchObject search)
        {
            Instructor instructor = await resolver.GetInstructorAsync(currentUserService.UserId);

            IQueryable<Assignment> query = context.Set<Assignment>()
                .AsNoTracking()
                .Where(x => x.LectureId == lectureId && x.Lecture.Course.InstructorId == instructor.Id);

            if (!string.IsNullOrWhiteSpace(search.Title))
            {
                query = query.Where(x => x.Title.Contains(search.Title));
            }

            if (search.DueAfter.HasValue)
            {
                query = query.Where(x => x.DueAt >= search.DueAfter.Value);
            }

            if (search.DueBefore.HasValue)
            {
                query = query.Where(x => x.DueAt <= search.DueBefore.Value);
            }

            return await query.ToPagedResultAsync(search.Page, search.PageSize, search.IncludeTotalCount, Map, q => q.OrderBy(x => x.DueAt));
        }

        public async Task<AssignmentDto> GetByIdForInstructorAsync(int lectureId, int assignmentId)
        {
            Assignment entity = await GetAssignmentForInstructorAsync(lectureId, assignmentId);

            return Map(entity);
        }

        public async Task<AssignmentDto> CreateAsync(int lectureId, AssignmentRequest request)
        {
            Instructor instructor = await resolver.GetInstructorAsync(currentUserService.UserId);

            _ = await context.Set<Lecture>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == lectureId && x.Course.InstructorId == instructor.Id)
                ?? throw new AuthorizationException(Messages.CourseNotOwned);

            var entity = new Assignment(request.Title.Trim(), request.Description.Trim(), request.DueAt, lectureId)
            {
                CreatedById = currentUserService.UserId
            };

            context.Set<Assignment>().Add(entity);
            await context.SaveChangesAsync();

            return Map(entity);
        }

        public async Task<AssignmentDto> UpdateAsync(int lectureId, int assignmentId, AssignmentRequest request)
        {
            Assignment entity = await GetAssignmentForInstructorAsync(lectureId, assignmentId, track: true);

            entity.UpdateDetails(
                request.Title.Trim(),
                request.Description.Trim(),
                request.DueAt
            );
            entity.UpdatedById = currentUserService.UserId;

            await context.SaveChangesAsync();

            return Map(entity);
        }

        public async Task DeleteAsync(int lectureId, int assignmentId)
        {
            Assignment entity = await GetAssignmentForInstructorAsync(lectureId, assignmentId, track: true);

            entity.SoftDelete();
            entity.UpdatedById = currentUserService.UserId;

            await context.SaveChangesAsync();
        }

        public async Task<PagedResult<AssignmentDto>> GetForStudentAsync(AssignmentSearchObject search)
        {
            Student student = await resolver.GetStudentAsync(currentUserService.UserId);

            IQueryable<Assignment> query = context.Set<Assignment>()
                .AsNoTracking()
                .Where(x => x.Lecture.LectureStatus != LectureStatus.Cancelled &&
                            x.Lecture.Course.Enrollments.Any(e =>
                                e.StudentId == student.Id &&
                                e.EnrollmentStatus == EnrollmentStatus.Active));

            if (!string.IsNullOrWhiteSpace(search.Title))
            {
                query = query.Where(x => x.Title.Contains(search.Title));
            }

            if (search.DueAfter.HasValue)
            {
                query = query.Where(x => x.DueAt >= search.DueAfter.Value);
            }

            if (search.DueBefore.HasValue)
            {
                query = query.Where(x => x.DueAt <= search.DueBefore.Value);
            }

            return await query.ToPagedResultAsync(search.Page, search.PageSize, search.IncludeTotalCount, Map, q => q.OrderBy(x => x.DueAt));
        }

        public async Task<AssignmentDto> GetByIdForStudentAsync(int assignmentId)
        {
            Student student = await resolver.GetStudentAsync(currentUserService.UserId);

            Assignment entity = await GetStudentAssignmentQuery(student.Id, assignmentId)
                .AsNoTracking()
                .FirstOrDefaultAsync()
                ?? throw new NotFoundException(Messages.AssignmentNotFound);

            return Map(entity);
        }

        public async Task<AssignmentSubmissionDto> SubmitAsync(int assignmentId, AssignmentSubmitRequest request)
        {
            Student student = await resolver.GetStudentAsync(currentUserService.UserId);

            Assignment assignment = await GetStudentAssignmentQuery(student.Id, assignmentId)
                .Include(x => x.AssignmentSubmissions)
                .FirstOrDefaultAsync()
                ?? throw new NotFoundException(Messages.AssignmentNotFound);

            AssignmentSubmission? existing = assignment.AssignmentSubmissions.FirstOrDefault(x => x.StudentId == student.Id);

            if (existing?.SubmittedAt is not null)
            {
                throw new ConflictException(Messages.AssignmentAlreadySubmitted);
            }

            if (existing is null)
            {
                existing = new AssignmentSubmission(assignment.Id, student.Id)
                {
                    CreatedById = currentUserService.UserId
                };
                assignment.AssignmentSubmissions.Add(existing);
            }

            existing.Submit(request.FilePath?.Trim(), clock.UtcNow);
            existing.UpdatedById = currentUserService.UserId;

            await context.SaveChangesAsync();

            return MapSubmission(existing, student, await resolver.GetStudentDisplayNameAsync(student));
        }

        public async Task<PagedResult<AssignmentSubmissionDto>> GetSubmissionsAsync(int lectureId, int assignmentId, int page, int pageSize)
        {
            await GetAssignmentForInstructorAsync(lectureId, assignmentId);

            IQueryable<AssignmentSubmission> query = context.Set<AssignmentSubmission>()
                .AsNoTracking()
                .Include(x => x.Student)
                .Where(x => x.AssignmentId == assignmentId);

            return await query.ToPagedResultAsync(page, pageSize, includeTotalCount: true,
                items => resolver.GetStudentDisplayNamesAsync(items.Select(x => x.Student)),
                (x, names) => MapSubmission(x, x.Student, names.GetValueOrDefault(x.StudentId, $"Student {x.StudentId}")),
                q => q.OrderBy(x => x.StudentId));
        }

        public async Task<AssignmentSubmissionDto> GradeAsync(int lectureId, int assignmentId, int submissionId, GradeAssignmentRequest request)
        {
            await GetAssignmentForInstructorAsync(lectureId, assignmentId);

            AssignmentSubmission submission = await context.Set<AssignmentSubmission>()
                .Include(x => x.Student)
                .FirstOrDefaultAsync(x => x.Id == submissionId && x.AssignmentId == assignmentId)
                ?? throw new NotFoundException(Messages.AssignmentSubmissionNotFound);

            submission.SetGrade(request.Grade);
            submission.UpdatedById = currentUserService.UserId;

            await context.SaveChangesAsync();

            return await MapSubmissionAsync(submission, submission.Student);
        }

        private async Task<Assignment> GetAssignmentForInstructorAsync(int lectureId, int assignmentId, bool track = false)
        {
            Instructor instructor = await resolver.GetInstructorAsync(currentUserService.UserId);

            IQueryable<Assignment> query = context.Set<Assignment>()
                .Where(x => x.Id == assignmentId && x.LectureId == lectureId
                && x.Lecture.Course.InstructorId == instructor.Id);

            return await (track ? query : query.AsNoTracking())
                .FirstOrDefaultAsync()
                ?? throw new NotFoundException(Messages.AssignmentNotFound);
        }

        private IQueryable<Assignment> GetStudentAssignmentQuery(int studentId, int assignmentId) =>
            context.Set<Assignment>()
            .Where(x => x.Id == assignmentId && x.Lecture.Course.Enrollments
            .Any(e => e.StudentId == studentId && e.EnrollmentStatus == EnrollmentStatus.Active));

        private static AssignmentDto Map(Assignment entity) => new()
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            DueAt = entity.DueAt,
            LectureId = entity.LectureId
        };

        private async Task<AssignmentSubmissionDto> MapSubmissionAsync(AssignmentSubmission submission, Student student)
        {
            return MapSubmission(submission, student, await resolver.GetStudentDisplayNameAsync(student));
        }

        private static AssignmentSubmissionDto MapSubmission(AssignmentSubmission submission, Student student, string studentName) => new()
        {
            Id = submission.Id,
            AssignmentId = submission.AssignmentId,
            StudentId = submission.StudentId,
            StudentName = studentName,
            SubmittedAt = submission.SubmittedAt,
            FilePath = submission.FilePath,
            Grade = submission.Grade
        };
    }
}
