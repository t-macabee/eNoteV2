using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Instructors;
using eNote.Application.Features.MusicStores.Services;
using eNote.Application.Features.Users.Services;
using eNote.Domain.Entities;
using eNote.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Announcements.Services
{
    public class AnnouncementService(IAppDbContext context, IClock clock, IUserContextResolver resolver, IInstructorAccessService instructorAccess, IMusicStoreContextService storeContext, ICurrentUserService currentUserService, IFileStorageService fileStorage) : IAnnouncementService
    {
        public async Task<PagedResult<AnnouncementDto>> GetFeedForStudentAsync(int page, int pageSize)
        {
            var studentId = (await resolver.GetStudentAsync(currentUserService.UserId)).Id;

            var query = context.Set<Announcement>()
                .AsNoTracking()
                .Include(a => a.Course)
                .Include(a => a.MusicStore)
                .Where(a =>
                    (a.CourseId != null && context.Set<Enrollment>().Any(e =>
                        e.StudentId == studentId &&
                        e.EnrollmentStatus == EnrollmentStatus.Active &&
                        e.CourseId == a.CourseId)) ||
                    (a.MusicStoreId != null && context.Set<InstrumentRental>().Any(r =>
                        r.StudentProfileId == studentId &&
                        InstrumentRentalStatusSets.History.Contains(r.RentalStatus) &&
                        r.Instrument.MusicStoreId == a.MusicStoreId)));

            return await query.ToPagedResultAsync(page, pageSize, includeTotalCount: true, MapToDto, q => q.OrderByDescending(x => x.PublishedAt));
        }

        public async Task<AnnouncementDto> CreateForCourseAsync(int courseId, AnnouncementRequest request)
        {
            var instructor = await instructorAccess.GetInstructorAsync(currentUserService.UserId);

            if (!await instructorAccess.OwnsCourseAsync(courseId, instructor.Id))
            {
                throw new BusinessException(Messages.AnnouncementCourseForbidden);
            }

            var entity = new Announcement(request.Title.Trim(), request.Content.Trim(), courseId, null, clock.UtcNow)
            {
                CreatedById = currentUserService.UserId
            };

            context.Set<Announcement>().Add(entity);
            await context.SaveChangesAsync();

            return await LoadDtoAsync(entity.Id);
        }

        public async Task<AnnouncementDto> GetByIdForCourseAsync(int courseId, int announcementId)
        {
            var entity = await (await GetCourseAnnouncementQueryAsync(courseId))
                .FirstOrDefaultAsync(a => a.Id == announcementId)
                ?? throw new NotFoundException(Messages.AnnouncementNotFound);

            return MapToDto(entity);
        }

        public async Task<PagedResult<AnnouncementDto>> GetForCourseAsync(int courseId, int page, int pageSize)
        {
            return await (await GetCourseAnnouncementQueryAsync(courseId))
                .ToPagedResultAsync(page, pageSize, includeTotalCount: true, MapToDto, q => q.OrderByDescending(x => x.PublishedAt));
        }

        public async Task<AnnouncementDto> UpdateForCourseAsync(int courseId, int announcementId, AnnouncementRequest request)
        {
            var entity = await (await GetCourseAnnouncementQueryAsync(courseId, track: true))
                .FirstOrDefaultAsync(a => a.Id == announcementId)
                ?? throw new NotFoundException(Messages.AnnouncementNotFound);

            entity.UpdateDetails(request.Title.Trim(), request.Content.Trim());
            entity.UpdatedById = currentUserService.UserId;

            await context.SaveChangesAsync();

            return await LoadDtoAsync(entity.Id);
        }

        public async Task DeleteForCourseAsync(int courseId, int announcementId)
        {
            var entity = await (await GetCourseAnnouncementQueryAsync(courseId, track: true))
                .FirstOrDefaultAsync(a => a.Id == announcementId)
                ?? throw new NotFoundException(Messages.AnnouncementNotFound);

            entity.SoftDelete();
            entity.UpdatedById = currentUserService.UserId;

            await context.SaveChangesAsync();
        }

        public async Task<AnnouncementDto> CreateForStoreAsync(AnnouncementRequest request)
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

        public async Task<AnnouncementDto> GetByIdForStoreAsync(int announcementId)
        {
            var storeId = await storeContext.GetActiveStoreAsync(currentUserService.UserId);

            var entity = await context.Set<Announcement>()
                .AsNoTracking()
                .Include(a => a.MusicStore)
                .FirstOrDefaultAsync(a => a.Id == announcementId && a.MusicStoreId == storeId)
                ?? throw new NotFoundException(Messages.AnnouncementNotFound);

            return MapToDto(entity);
        }

        public async Task<PagedResult<AnnouncementDto>> GetForStoreAsync(int page, int pageSize)
        {
            var storeId = await storeContext.GetActiveStoreAsync(currentUserService.UserId);

            return await context.Set<Announcement>()
                .AsNoTracking()
                .Include(a => a.MusicStore)
                .Where(a => a.MusicStoreId == storeId)
                .ToPagedResultAsync(page, pageSize, includeTotalCount: true, MapToDto, q => q.OrderByDescending(x => x.PublishedAt));
        }

        public async Task<AnnouncementDto> UpdateForStoreAsync(int announcementId, AnnouncementRequest request)
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

        public async Task<AnnouncementDto> UploadImageForCourseAsync(int courseId, int announcementId, Stream stream, string fileName, string contentType, CancellationToken ct = default)
        {
            var entity = await (await GetCourseAnnouncementQueryAsync(courseId, track: true))
                .FirstOrDefaultAsync(a => a.Id == announcementId, ct)
                ?? throw new NotFoundException(Messages.AnnouncementNotFound);

            var path = await fileStorage.SaveAsync(stream, fileName, contentType, "announcements", ct);
            entity.SetImagePath(path);
            entity.UpdatedById = currentUserService.UserId;

            await context.SaveChangesAsync(ct);

            return await LoadDtoAsync(entity.Id);
        }

        public async Task<AnnouncementDto> UploadImageForStoreAsync(int announcementId, Stream stream, string fileName, string contentType, CancellationToken ct = default)
        {
            var storeId = await storeContext.GetActiveStoreAsync(currentUserService.UserId);

            var entity = await context.Set<Announcement>()
                .FirstOrDefaultAsync(a => a.Id == announcementId && a.MusicStoreId == storeId, ct)
                ?? throw new NotFoundException(Messages.AnnouncementNotFound);

            var path = await fileStorage.SaveAsync(stream, fileName, contentType, "announcements", ct);
            entity.SetImagePath(path);
            entity.UpdatedById = currentUserService.UserId;

            await context.SaveChangesAsync(ct);

            return await LoadDtoAsync(entity.Id);
        }

        private async Task<IQueryable<Announcement>> GetCourseAnnouncementQueryAsync(int courseId, bool track = false)
        {
            var instructor = await instructorAccess.GetInstructorAsync(currentUserService.UserId);
            return instructorAccess.CourseAnnouncementsFor(courseId, instructor.Id, track);
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
            var scope = entity.CourseId.HasValue ? AnnouncementScope.Course : AnnouncementScope.MusicStore;

            return new AnnouncementDto
            {
                Id = entity.Id,
                Title = entity.Title,
                Content = entity.Content,
                ImagePath = entity.ImagePath,
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
