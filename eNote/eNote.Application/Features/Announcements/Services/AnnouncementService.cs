using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Announcements.Services.Interfaces;
using eNote.Application.Features.MusicStores.Services.Interfaces;
using eNote.Application.Features.Users;
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Announcements.Services
{
    public class AnnouncementService(IAppDbContext context, IClock clock, IMusicStoreContextService storeContext, ICurrentUserService currentUserService) : IAnnouncementService
    {
        private static readonly InstrumentRentalStatus[] StoreAudienceRentalStatuses =
        [
            InstrumentRentalStatus.Approved,
            InstrumentRentalStatus.Active,
            InstrumentRentalStatus.Completed,
            InstrumentRentalStatus.ReturnedEarly
        ];

        public async Task<IReadOnlyList<AnnouncementDto>> GetFeedForStudentAsync()
        {
            var studentId = await context.Set<Student>()
                .Where(s => s.AppUserId == currentUserService.UserId)
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
                .Where(a => (a.CourseId != null && enrolledCourseIds.Contains(a.CourseId.Value)) ||
                            (a.MusicStoreId != null && storeIds.Contains(a.MusicStoreId.Value)))
                .OrderByDescending(a => a.PublishedAt)
                .Take(50)
                .ToListAsync();

            return [.. items.Select(MapToDto)];
        }

        public async Task<AnnouncementDto> CreateForCourseAsync(int courseId, AnnouncementCreateRequest request)
        {
            var instructor = await UserProfileHelper.GetInstructorByUserIdAsync(context, currentUserService.UserId);

            var ownsCourse = await context.Set<Course>()
                .AsNoTracking()
                .AnyAsync(c => c.Id == courseId && c.InstructorId == instructor.Id);

            if (!ownsCourse)
                throw new BusinessException(Messages.AnnouncementCourseForbidden);

            var entity = new Announcement(
                request.Title.Trim(),
                request.Content.Trim(),
                courseId,
                null,
                clock.UtcNow
            )
            {
                CreatedById = currentUserService.UserId
            };

            context.Set<Announcement>().Add(entity);
            await context.SaveChangesAsync();

            return await LoadDtoAsync(entity.Id);
        }

        public async Task<IReadOnlyList<AnnouncementDto>> GetForCourseAsync(int courseId)
        {
            var items = await GetCourseAnnouncementQuery(courseId)
                .OrderByDescending(a => a.PublishedAt)
                .ToListAsync();

            return [.. items.Select(MapToDto)];
        }

        public async Task<AnnouncementDto> UpdateForCourseAsync(int courseId, int announcementId, AnnouncementUpdateRequest request)
        {
            var entity = await GetCourseAnnouncementQuery(courseId, track: true)
                .FirstOrDefaultAsync(a => a.Id == announcementId)
                ?? throw new NotFoundException(Messages.AnnouncementNotFound);

            entity.UpdateDetails(request.Title.Trim(), request.Content.Trim());
            entity.UpdatedById = currentUserService.UserId;

            await context.SaveChangesAsync();

            return await LoadDtoAsync(entity.Id);
        }

        public async Task DeleteForCourseAsync(int courseId, int announcementId)
        {
            var entity = await GetCourseAnnouncementQuery(courseId, track: true)
                .FirstOrDefaultAsync(a => a.Id == announcementId)
                ?? throw new NotFoundException(Messages.AnnouncementNotFound);

            entity.SoftDelete();
            entity.UpdatedById = currentUserService.UserId;

            await context.SaveChangesAsync();
        }

        public async Task<AnnouncementDto> CreateForStoreAsync(AnnouncementCreateRequest request)
        {
            var storeId = await storeContext.GetActiveStoreAsync(currentUserService.UserId);

            var entity = new Announcement(request.Title.Trim(), request.Content.Trim(), null, storeId, clock.UtcNow)
            {
                CreatedById = currentUserService.UserId
            };

            context.Set<Announcement>().Add(entity);
            await context.SaveChangesAsync();

            return await LoadDtoAsync(entity.Id);
        }

        public async Task<IReadOnlyList<AnnouncementDto>> GetForStoreAsync()
        {
            var storeId = await storeContext.GetActiveStoreAsync(currentUserService.UserId);

            var items = await context.Set<Announcement>()
                .AsNoTracking()
                .Include(a => a.MusicStore)
                .Where(a => a.MusicStoreId == storeId)
                .OrderByDescending(a => a.PublishedAt)
                .ToListAsync();

            return [.. items.Select(MapToDto)];
        }

        public async Task<AnnouncementDto> UpdateForStoreAsync(int announcementId, AnnouncementUpdateRequest request)
        {
            var storeId = await storeContext.GetActiveStoreAsync(currentUserService.UserId);

            var entity = await context.Set<Announcement>()
                .FirstOrDefaultAsync(a => a.Id == announcementId && a.MusicStoreId == storeId)
                ?? throw new NotFoundException(Messages.AnnouncementNotFound);

            entity.UpdateDetails(request.Title.Trim(), request.Content.Trim());
            entity.UpdatedById = currentUserService.UserId;

            await context.SaveChangesAsync();

            return await LoadDtoAsync(entity.Id);
        }

        public async Task DeleteForStoreAsync(int announcementId)
        {
            var storeId = await storeContext.GetActiveStoreAsync(currentUserService.UserId);

            var entity = await context.Set<Announcement>()
                .FirstOrDefaultAsync(a => a.Id == announcementId && a.MusicStoreId == storeId)
                ?? throw new NotFoundException(Messages.AnnouncementNotFound);

            entity.SoftDelete();
            entity.UpdatedById = currentUserService.UserId;

            await context.SaveChangesAsync();
        }

        private IQueryable<Announcement> GetCourseAnnouncementQuery(int courseId, bool track = false)
        {
            var query = context.Set<Announcement>()
                .Include(a => a.Course)
                .Where(a => a.CourseId == courseId && a.Course != null
                && a.Course.Instructor.AppUserId == currentUserService.UserId);

            return track ? query : query.AsNoTracking();
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
            var scope = entity.CourseId.HasValue? AnnouncementScope.Course: AnnouncementScope.MusicStore;

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