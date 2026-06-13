using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Announcements.Services.Interfaces;
using eNote.Application.Features.MusicStores.Services.Interfaces;
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Announcements.Services
{
    public class AnnouncementService(IAppDbContext context, IClock clock, IMusicStoreContextService storeContext) : IAnnouncementService
    {
        private static readonly InstrumentRentalStatus[] StoreAudienceRentalStatuses =
        [
            InstrumentRentalStatus.Approved,
            InstrumentRentalStatus.Active,
            InstrumentRentalStatus.Completed,
            InstrumentRentalStatus.ReturnedEarly
        ];

        public async Task<IReadOnlyList<AnnouncementDto>> GetFeedForStudentAsync(int userId)
        {
            var studentId = await context.Set<Student>()
                .Where(s => s.AppUserId == userId)
                .Select(s => s.Id)
                .SingleOrDefaultAsync();

            if (studentId == 0)
                throw new BusinessException(Messages.StudentProfileNotFound);

            var enrolledCourseIds = await context.Set<Enrollment>()
                .AsNoTracking()
                .Where(e => e.StudentId == studentId && e.EnrollmentStatus == EnrollmentStatus.Active)
                .Select(e => e.CourseId)
                .ToListAsync();

            var storeIds = await context.Set<InstrumentRental>()
                .AsNoTracking()
                .Where(r => r.StudentProfileId == studentId && StoreAudienceRentalStatuses.Contains(r.RentalStatus))
                .Select(r => r.Instrument.MusicStoreId)
                .Distinct()
                .ToListAsync();

            var items = await context.Set<Announcement>()
                .AsNoTracking()
                .Include(a => a.Course)
                .Include(a => a.MusicStore)
                .Where(a =>
                    (a.CourseId != null && enrolledCourseIds.Contains(a.CourseId.Value)) ||
                    (a.MusicStoreId != null && storeIds.Contains(a.MusicStoreId.Value)))
                .OrderByDescending(a => a.PublishedAt)
                .Take(50)
                .ToListAsync();

            return [.. items.Select(MapToDto)];
        }

        public async Task<AnnouncementDto> CreateForCourseAsync(int userId, int courseId, AnnouncementCreateRequest request)
        {
            await EnsureInstructorOwnsCourseAsync(userId, courseId);

            var entity = new Announcement
            {
                Title = request.Title.Trim(),
                Content = request.Content.Trim(),
                PublishedAt = clock.UtcNow,
                CreatedById = userId,
                CourseId = courseId,
                MusicStoreId = null
            };

            context.Set<Announcement>().Add(entity);

            await context.SaveChangesAsync();

            return await LoadDtoAsync(entity.Id);
        }

        public async Task<IReadOnlyList<AnnouncementDto>> GetForCourseAsync(int userId, int courseId)
        {
            await EnsureInstructorOwnsCourseAsync(userId, courseId);

            var items = await context.Set<Announcement>()
                .AsNoTracking()
                .Include(a => a.Course)
                .Where(a => a.CourseId == courseId)
                .OrderByDescending(a => a.PublishedAt)
                .ToListAsync();

            return [.. items.Select(MapToDto)];
        }

        public async Task<AnnouncementDto> CreateForStoreAsync(int userId, AnnouncementCreateRequest request)
        {
            var storeId = await storeContext.GetActiveStoreAsync(userId);

            var entity = new Announcement
            {
                Title = request.Title.Trim(),
                Content = request.Content.Trim(),
                PublishedAt = clock.UtcNow,
                CreatedById = userId,
                CourseId = null,
                MusicStoreId = storeId
            };

            context.Set<Announcement>().Add(entity);
            await context.SaveChangesAsync();

            return await LoadDtoAsync(entity.Id);
        }

        public async Task<IReadOnlyList<AnnouncementDto>> GetForStoreAsync(int userId)
        {
            var storeId = await storeContext.GetActiveStoreAsync(userId);

            var items = await context.Set<Announcement>()
                .AsNoTracking()
                .Include(a => a.MusicStore)
                .Where(a => a.MusicStoreId == storeId)
                .OrderByDescending(a => a.PublishedAt)
                .ToListAsync();

            return [.. items.Select(MapToDto)];
        }

        private async Task EnsureInstructorOwnsCourseAsync(int userId, int courseId)
        {
            var ownsCourse = await context.Set<Course>()
                .AsNoTracking()
                .AnyAsync(c => c.Id == courseId && c.Instructor.AppUserId == userId);

            if (!ownsCourse)
                throw new BusinessException(Messages.AnnouncementCourseForbidden);
        }

        private async Task<AnnouncementDto> LoadDtoAsync(int announcementId)
        {
            var entity = await context.Set<Announcement>()
                .AsNoTracking()
                .Include(a => a.Course)
                .Include(a => a.MusicStore)
                .FirstOrDefaultAsync(a => a.Id == announcementId)
                ?? throw new NotFoundException(Messages.AnnouncementNotFound);

            return MapToDto(entity);
        }

        private static AnnouncementDto MapToDto(Announcement entity)
        {
            var scope = entity.CourseId.HasValue
                ? AnnouncementScope.Course
                : AnnouncementScope.MusicStore;

            return new AnnouncementDto
            {
                Id = entity.Id,
                Title = entity.Title,
                Content = entity.Content,
                PublishedAt = entity.PublishedAt,
                Scope = scope,
                CourseId = entity.CourseId,
                CourseName = entity.Course?.Name,
                MusicStoreId = entity.MusicStoreId,
                StoreName = entity.MusicStore?.StoreName
            };
        }
    }
}
