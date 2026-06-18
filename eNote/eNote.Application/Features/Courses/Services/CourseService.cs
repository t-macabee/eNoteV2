using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Users.Services;
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eNote.Application.Features.Courses.Services
{
    public class CourseService(IAppDbContext context, IMapper mapper, IUserContextResolver resolver, ICurrentUserService currentUserService, ILogger<CourseService> logger) : ICourseService
    {
        public async Task<CourseDto> GetByIdForInstructorAsync(int id)
        {
            Instructor instructor = await resolver.GetInstructorAsync(currentUserService.UserId);

            Course entity = await context.Set<Course>()
                .Include(c => c.Enrollments)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && c.InstructorId == instructor.Id)
                ?? throw new NotFoundException(Messages.CourseNotFound);

            return mapper.Map<CourseDto>(entity);
        }

        public async Task<CourseDto> GetByIdForStudentAsync(int id)
        {
            Student student = await resolver.GetStudentAsync(currentUserService.UserId);

            Course entity = await context.Set<Course>()
                .Include(c => c.Enrollments)
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    (c.IsPublished ||
                     c.Enrollments.Any(e =>
                         e.StudentId == student.Id &&
                         e.EnrollmentStatus == EnrollmentStatus.Active)))
                ?? throw new NotFoundException(Messages.CourseNotFound);

            return mapper.Map<CourseDto>(entity);
        }

        public async Task<PagedResult<CourseDto>> GetPagedForInstructorAsync(CourseSearchObject search)
        {
            Instructor instructor = await resolver.GetInstructorAsync(currentUserService.UserId);

            IQueryable<Course> query = context.Set<Course>()
                .AsNoTracking()
                .Include(c => c.Enrollments)
                .Where(c => c.InstructorId == instructor.Id);

            query = query.ApplySearch(search);

            return await query.ToPagedResultAsync(search.Page, search.PageSize, search.IncludeTotalCount, mapper.Map<CourseDto>, q => q.OrderByDescending(x => x.StartDate));
        }

        public async Task<PagedResult<CourseDto>> GetPagedForStudentAsync(CourseSearchObject search)
        {
            IQueryable<Course> query = context.Set<Course>()
                .AsNoTracking()
                .Include(c => c.Enrollments)
                .Where(c => c.IsPublished);

            query = query.ApplySearch(search);

            return await query.ToPagedResultAsync(search.Page, search.PageSize, search.IncludeTotalCount, mapper.Map<CourseDto>, q => q.OrderByDescending(x => x.StartDate));
        }

        public async Task<CourseDto> CreateAsync(CourseRequest request)
        {
            Instructor instructor = await resolver.GetInstructorAsync(currentUserService.UserId);

            logger.LogInformation("Creating course {CourseName} by instructor user {InstructorUserId}", request.Name, currentUserService.UserId);

            var entity = new Course(request.Name.Trim(), request.Description?.Trim(), request.Price, request.StartDate, request.EndDate, instructor.Id);
            entity.SetPublishedStatus(request.IsPublished);
            entity.CreatedById = currentUserService.UserId;

            context.Set<Course>().Add(entity);
            await context.SaveChangesAsync();

            logger.LogInformation("Course {CourseId} created by instructor user {InstructorUserId}", entity.Id, currentUserService.UserId);

            return mapper.Map<CourseDto>(entity);
        }

        public async Task<CourseDto> UpdateAsync(int id, CourseRequest request)
        {
            Instructor instructor = await resolver.GetInstructorAsync(currentUserService.UserId);

            Course entity = await context.Set<Course>()
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.Id == id && c.InstructorId == instructor.Id)
                ?? throw new NotFoundException(Messages.CourseNotFound);

            entity.UpdateDetails(
                request.Name.Trim(),
                request.Description?.Trim(),
                request.Price,
                request.StartDate,
                request.EndDate
            );
            entity.SetPublishedStatus(request.IsPublished);
            entity.UpdatedById = currentUserService.UserId;

            await context.SaveChangesAsync();

            return mapper.Map<CourseDto>(entity);
        }

        public async Task DeleteAsync(int id)
        {
            Instructor instructor = await resolver.GetInstructorAsync(currentUserService.UserId);

            Course entity = await context.Set<Course>()
                .Include(c => c.Lectures)
                .FirstOrDefaultAsync(c => c.Id == id && c.InstructorId == instructor.Id)
                ?? throw new NotFoundException(Messages.CourseNotFound);

            entity.SoftDelete();
            entity.UpdatedById = currentUserService.UserId;

            foreach (Lecture lecture in entity.Lectures)
            {
                lecture.SoftDelete();
                lecture.UpdatedById = currentUserService.UserId;
            }

            await context.SaveChangesAsync();

            logger.LogInformation("Course {CourseId} soft-deleted by instructor user {InstructorUserId}", id, currentUserService.UserId);
        }

        public async Task EnrollAsync(int courseId)
        {
            Student student = await resolver.GetStudentAsync(currentUserService.UserId);

            Course course = await context.Set<Course>()
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.Id == courseId && c.IsPublished)
                ?? throw new NotFoundException(Messages.CourseNotFound);

            Enrollment? existing = course.Enrollments.FirstOrDefault(e => e.StudentId == student.Id && e.EnrollmentStatus == EnrollmentStatus.Active);

            if (existing != null)
            {
                return;
            }

            var enrollment = new Enrollment(student.Id, course.Id, EnrollmentStatus.Active);

            context.Set<Enrollment>().Add(enrollment);
            await context.SaveChangesAsync();

            logger.LogInformation("Student {StudentUserId} enrolled in course {CourseId}", currentUserService.UserId, courseId);
        }

        public async Task UnenrollAsync(int courseId)
        {
            Student student = await resolver.GetStudentAsync(currentUserService.UserId);

            Enrollment enrollment = await context.Set<Enrollment>()
                .FirstOrDefaultAsync(e =>
                    e.CourseId == courseId &&
                    e.StudentId == student.Id &&
                    e.EnrollmentStatus == EnrollmentStatus.Active)
                ?? throw new BusinessException(Messages.StudentNotEnrolled);

            enrollment.UpdateStatus(EnrollmentStatus.Canceled);

            await context.SaveChangesAsync();

            logger.LogInformation("Student {StudentUserId} unenrolled from course {CourseId}", currentUserService.UserId, courseId);
        }

    }
}
