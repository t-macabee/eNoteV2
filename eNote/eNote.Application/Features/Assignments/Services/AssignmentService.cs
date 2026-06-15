using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Assignments.Services.Interfaces;
using eNote.Application.Features.Users;
using eNote.Application.Features.Users.Services.Interfaces;
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Assignments.Services
{
    public class AssignmentService(IAppDbContext context, IClock clock, IUserIdentityService identity) : IAssignmentService
    {
        public async Task<PagedResult<AssignmentDto>> GetForLectureAsync(int lectureId, int instructorUserId, int page, int pageSize)
        {
            await EnsureInstructorOwnsLectureAsync(lectureId, instructorUserId);

            var query = context.Set<Assignment>()
                .AsNoTracking()
                .Where(x => x.LectureId == lectureId && x.IsActive);

            return await query.ToPagedResultAsync(
                page,
                pageSize,
                includeTotalCount: true,
                Map,
                q => q.OrderBy(x => x.DueAt));
        }

        public async Task<AssignmentDto> GetByIdForInstructorAsync(int lectureId, int assignmentId, int instructorUserId)
        {
            var entity = await GetAssignmentForInstructorAsync(lectureId, assignmentId, instructorUserId);
            return Map(entity);
        }

        public async Task<AssignmentDto> CreateAsync(int lectureId, int instructorUserId, AssignmentCreateRequest request)
        {
            await EnsureInstructorOwnsLectureAsync(lectureId, instructorUserId);

            var entity = new Assignment
            {
                Title = request.Title.Trim(),
                Description = request.Description.Trim(),
                DueAt = request.DueAt,
                LectureId = lectureId,
                CreatedById = instructorUserId
            };

            context.Set<Assignment>().Add(entity);
            await context.SaveChangesAsync();

            return Map(entity);
        }

        public async Task<AssignmentDto> UpdateAsync(int lectureId, int assignmentId, int instructorUserId, AssignmentUpdateRequest request)
        {
            var entity = await GetAssignmentForInstructorTrackedAsync(lectureId, assignmentId, instructorUserId);

            entity.Title = request.Title.Trim();
            entity.Description = request.Description.Trim();
            entity.DueAt = request.DueAt;
            entity.UpdatedAt = clock.UtcNow;
            entity.UpdatedById = instructorUserId;

            await context.SaveChangesAsync();

            return Map(entity);
        }

        public async Task DeleteAsync(int lectureId, int assignmentId, int instructorUserId)
        {
            var entity = await GetAssignmentForInstructorTrackedAsync(lectureId, assignmentId, instructorUserId);

            entity.IsActive = false;
            entity.UpdatedAt = clock.UtcNow;
            entity.UpdatedById = instructorUserId;
            await context.SaveChangesAsync();
        }

        public async Task<PagedResult<AssignmentDto>> GetForStudentAsync(int studentUserId, int page, int pageSize)
        {
            var student = await UserProfileHelper.GetStudentByUserIdAsync(context, studentUserId);

            var enrolledCourseIds = await context.Set<Enrollment>()
                .AsNoTracking()
                .Where(e => e.StudentId == student.Id && e.EnrollmentStatus == EnrollmentStatus.Active)
                .Select(e => e.CourseId)
                .ToListAsync();

            var query = context.Set<Assignment>()
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    enrolledCourseIds.Contains(x.Lecture.CourseId) &&
                    x.Lecture.IsActive &&
                    !x.Lecture.IsCancelled);

            return await query.ToPagedResultAsync(
                page,
                pageSize,
                includeTotalCount: true,
                Map,
                q => q.OrderBy(x => x.DueAt));
        }

        public async Task<AssignmentDto> GetByIdForStudentAsync(int assignmentId, int studentUserId)
        {
            var student = await UserProfileHelper.GetStudentByUserIdAsync(context, studentUserId);

            var entity = await context.Set<Assignment>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == assignmentId &&
                    x.IsActive &&
                    x.Lecture.IsActive &&
                    x.Lecture.Course.Enrollments.Any(e =>
                        e.StudentId == student.Id &&
                        e.EnrollmentStatus == EnrollmentStatus.Active))
                ?? throw new NotFoundException(Messages.AssignmentNotFound);

            return Map(entity);
        }

        public async Task<AssignmentSubmissionDto> SubmitAsync(int assignmentId, int studentUserId, AssignmentSubmitRequest request)
        {
            var student = await UserProfileHelper.GetStudentByUserIdAsync(context, studentUserId);

            var assignment = await context.Set<Assignment>()
                .Include(x => x.AssignmentSubmissions)
                .FirstOrDefaultAsync(x =>
                    x.Id == assignmentId &&
                    x.IsActive &&
                    x.Lecture.IsActive &&
                    x.Lecture.Course.Enrollments.Any(e =>
                        e.StudentId == student.Id &&
                        e.EnrollmentStatus == EnrollmentStatus.Active))
                ?? throw new NotFoundException(Messages.AssignmentNotFound);

            var existing = assignment.AssignmentSubmissions.FirstOrDefault(x => x.StudentId == student.Id);

            if (existing?.SubmittedAt is not null)
                throw new ConflictException(Messages.AssignmentAlreadySubmitted);

            if (existing is null)
            {
                existing = new AssignmentSubmission
                {
                    AssignmentId = assignment.Id,
                    StudentId = student.Id,
                    CreatedById = studentUserId
                };
                assignment.AssignmentSubmissions.Add(existing);
            }

            existing.SubmittedAt = clock.UtcNow;
            existing.FilePath = request.FilePath?.Trim();
            existing.UpdatedAt = clock.UtcNow;
            existing.UpdatedById = studentUserId;

            await context.SaveChangesAsync();

            return MapSubmission(existing, student, await UserProfileHelper.GetStudentDisplayNameAsync(identity, student));
        }

        public async Task<PagedResult<AssignmentSubmissionDto>> GetSubmissionsAsync(int lectureId, int assignmentId, int instructorUserId, int page, int pageSize)
        {
            await GetAssignmentForInstructorAsync(lectureId, assignmentId, instructorUserId);

            var query = context.Set<AssignmentSubmission>()
                .AsNoTracking()
                .Include(x => x.Student)
                .Where(x => x.AssignmentId == assignmentId);

            (page, pageSize) = PagingLimits.Normalize(page, pageSize);

            var total = await query.CountAsync();
            var submissions = await query
                .OrderBy(x => x.StudentId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = new List<AssignmentSubmissionDto>();
            foreach (var submission in submissions)
                items.Add(await MapSubmissionAsync(submission, submission.Student));

            return new PagedResult<AssignmentSubmissionDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };
        }

        public async Task<AssignmentSubmissionDto> GradeAsync(int lectureId, int assignmentId, int submissionId, int instructorUserId, GradeAssignmentRequest request)
        {
            if (request.Grade is < 0 or > 100)
                throw new BusinessException(Messages.AssignmentInvalidGrade);

            await GetAssignmentForInstructorAsync(lectureId, assignmentId, instructorUserId);

            var submission = await context.Set<AssignmentSubmission>()
                .Include(x => x.Student)
                .FirstOrDefaultAsync(x => x.Id == submissionId && x.AssignmentId == assignmentId)
                ?? throw new NotFoundException(Messages.AssignmentSubmissionNotFound);

            submission.Grade = request.Grade;
            submission.UpdatedAt = clock.UtcNow;
            submission.UpdatedById = instructorUserId;

            await context.SaveChangesAsync();

            return await MapSubmissionAsync(submission, submission.Student);
        }

        private async Task<Assignment> GetAssignmentForInstructorAsync(int lectureId, int assignmentId, int instructorUserId)
        {
            await EnsureInstructorOwnsLectureAsync(lectureId, instructorUserId);

            return await context.Set<Assignment>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == assignmentId && x.LectureId == lectureId && x.IsActive)
                ?? throw new NotFoundException(Messages.AssignmentNotFound);
        }

        private async Task<Assignment> GetAssignmentForInstructorTrackedAsync(int lectureId, int assignmentId, int instructorUserId)
        {
            await EnsureInstructorOwnsLectureAsync(lectureId, instructorUserId);

            return await context.Set<Assignment>()
                .FirstOrDefaultAsync(x => x.Id == assignmentId && x.LectureId == lectureId && x.IsActive)
                ?? throw new NotFoundException(Messages.AssignmentNotFound);
        }

        private async Task EnsureInstructorOwnsLectureAsync(int lectureId, int instructorUserId)
        {
            var instructor = await UserProfileHelper.GetInstructorByUserIdAsync(context, instructorUserId);

            _ = await context.Set<Lecture>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == lectureId && x.Course.InstructorId == instructor.Id && x.IsActive)
                ?? throw new AuthorizationException(Messages.CourseNotOwned);
        }

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
            return MapSubmission(
                submission,
                student,
                await UserProfileHelper.GetStudentDisplayNameAsync(identity, student));
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
