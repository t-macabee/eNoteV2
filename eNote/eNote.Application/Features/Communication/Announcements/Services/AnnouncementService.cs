using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Common.Time;
using eNote.Application.Features.Communication.Announcements;
using eNote.Application.Features.Identity.Instructors;
using eNote.Application.Features.Identity.Users.Services;
using eNote.Application.Features.Rentals.MusicStores.Services;
using eNote.Domain.Entities;
using eNote.Domain.Entities.Communication;
using eNote.Domain.Entities.Rentals;
using eNote.Domain.Enums;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.Communication.Announcements.Services;

public sealed class AnnouncementService(IAppDbContext context, IClock clock, IUserContextResolver resolver, IInstructorAccessService instructorAccess, IMusicStoreContextService storeContext, ICurrentUserService currentUserService, IFileStorageService fileStorage, IMapper mapper)
     : ICourseAnnouncementService, IStoreAnnouncementService, IStudentAnnouncementService
{
    public async Task<PagedResult<AnnouncementDto>> GetFeedForStudentAsync(AnnouncementSearchObject search)
    {
        var studentId = await resolver.GetCurrentStudentIdAsync(currentUserService.UserId);

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

        return await query.ToPagedResultAsync(search, mapper.Map<AnnouncementDto>, q => q.OrderByDescending(x => x.PublishedAt));
    }

    public async Task<AnnouncementDto> CreateForCourseAsync(int courseId, AnnouncementRequest request)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUserService.UserId);

        if (!await instructorAccess.OwnsCourseAsync(courseId, instructorId))
        {
            throw new BusinessException(Messages.AnnouncementCourseForbidden);
        }

        var entity = new Announcement(request.Title.Trim(), request.Content.Trim(), courseId, null, clock.UtcNow)
        {
            CreatedById = currentUserService.UserId
        };

        context.Set<Announcement>().Add(entity);
        await context.SaveChangesAsync();

        return mapper.Map<AnnouncementDto>(entity);
    }

    public async Task<AnnouncementDto> GetByIdForCourseAsync(int courseId, int announcementId)
    {
        var entity = await (await GetCourseAnnouncementQueryAsync(courseId)).FirstOrDefaultAsync(a => a.Id == announcementId) ?? throw new NotFoundException(Messages.AnnouncementNotFound);

        return mapper.Map<AnnouncementDto>(entity);
    }

    public async Task<PagedResult<AnnouncementDto>> GetForCourseAsync(int courseId, AnnouncementSearchObject search)
    {
        return await (await GetCourseAnnouncementQueryAsync(courseId)).ToPagedResultAsync(search, mapper.Map<AnnouncementDto>, q => q.OrderByDescending(x => x.PublishedAt));
    }

    public async Task<AnnouncementDto> UpdateForCourseAsync(int courseId, int announcementId, AnnouncementRequest request)
    {
        var entity = await (await GetCourseAnnouncementQueryAsync(courseId, track: true)).FirstOrDefaultAsync(a => a.Id == announcementId) ?? throw new NotFoundException(Messages.AnnouncementNotFound);

        entity.UpdateDetails(request.Title.Trim(), request.Content.Trim());
        entity.UpdatedById = currentUserService.UserId;

        await context.SaveChangesAsync();

        return mapper.Map<AnnouncementDto>(entity);
    }

    public async Task DeleteForCourseAsync(int courseId, int announcementId)
    {
        var entity = await (await GetCourseAnnouncementQueryAsync(courseId, track: true)).FirstOrDefaultAsync(a => a.Id == announcementId) ?? throw new NotFoundException(Messages.AnnouncementNotFound);

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

        return mapper.Map<AnnouncementDto>(entity);
    }

    public async Task<AnnouncementDto> GetByIdForStoreAsync(int announcementId)
    {
        var storeId = await storeContext.GetActiveStoreAsync(currentUserService.UserId);

        var entity = await context.Set<Announcement>()
            .AsNoTracking()
            .Include(a => a.MusicStore)
            .FirstOrDefaultAsync(a => a.Id == announcementId && a.MusicStoreId == storeId) ?? throw new NotFoundException(Messages.AnnouncementNotFound);

        return mapper.Map<AnnouncementDto>(entity);
    }

    public async Task<PagedResult<AnnouncementDto>> GetForStoreAsync(AnnouncementSearchObject search)
    {
        var storeId = await storeContext.GetActiveStoreAsync(currentUserService.UserId);

        return await context.Set<Announcement>()
            .AsNoTracking()
            .Include(a => a.MusicStore)
            .Where(a => a.MusicStoreId == storeId)
            .ToPagedResultAsync(search, mapper.Map<AnnouncementDto>, q => q.OrderByDescending(x => x.PublishedAt));
    }

    public async Task<AnnouncementDto> UpdateForStoreAsync(int announcementId, AnnouncementRequest request)
    {
        var storeId = await storeContext.GetActiveStoreAsync(currentUserService.UserId);

        var entity = await context.Set<Announcement>()
            .FirstOrDefaultAsync(a => a.Id == announcementId && a.MusicStoreId == storeId) ?? throw new NotFoundException(Messages.AnnouncementNotFound);

        entity.UpdateDetails(request.Title.Trim(), request.Content.Trim());
        entity.UpdatedById = currentUserService.UserId;

        await context.SaveChangesAsync();

        return mapper.Map<AnnouncementDto>(entity);
    }

    public async Task DeleteForStoreAsync(int announcementId)
    {
        var storeId = await storeContext.GetActiveStoreAsync(currentUserService.UserId);

        var entity = await context.Set<Announcement>()
            .FirstOrDefaultAsync(a => a.Id == announcementId && a.MusicStoreId == storeId) ?? throw new NotFoundException(Messages.AnnouncementNotFound);

        entity.SoftDelete();
        entity.UpdatedById = currentUserService.UserId;

        await context.SaveChangesAsync();
    }

    public async Task<AnnouncementDto> UploadImageForCourseAsync(int courseId, int announcementId, Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        var entity = await (await GetCourseAnnouncementQueryAsync(courseId, track: true)).FirstOrDefaultAsync(a => a.Id == announcementId, ct) ?? throw new NotFoundException(Messages.AnnouncementNotFound);
        var path = await fileStorage.SaveAsync(stream, fileName, contentType, "announcements", ct);

        entity.SetImagePath(path);
        entity.UpdatedById = currentUserService.UserId;

        await context.SaveChangesAsync(ct);

        return mapper.Map<AnnouncementDto>(entity);
    }

    public async Task<AnnouncementDto> UploadImageForStoreAsync(int announcementId, Stream stream, string fileName, string contentType, CancellationToken ct = default)
    {
        var storeId = await storeContext.GetActiveStoreAsync(currentUserService.UserId);

        var entity = await context.Set<Announcement>()
            .FirstOrDefaultAsync(a => a.Id == announcementId && a.MusicStoreId == storeId, ct) ?? throw new NotFoundException(Messages.AnnouncementNotFound);

        var path = await fileStorage.SaveAsync(stream, fileName, contentType, "announcements", ct);

        entity.SetImagePath(path);
        entity.UpdatedById = currentUserService.UserId;

        await context.SaveChangesAsync(ct);

        return mapper.Map<AnnouncementDto>(entity);
    }

    private async Task<IQueryable<Announcement>> GetCourseAnnouncementQueryAsync(int courseId, bool track = false)
    {
        var instructorId = await instructorAccess.GetCurrentInstructorIdAsync(currentUserService.UserId);

        return instructorAccess.CourseAnnouncementsFor(courseId, instructorId, track);
    }
}
