using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Localization;
using eNote.Application.Common.Paging;
using eNote.Application.Common.Persistence;
using eNote.Application.Features.Users.Services;
using eNote.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eNote.Application.Features.ReferenceData.Addresses;

public sealed class AddressService(IAppDbContext context, IUserAccountService accountService) : IAddressService
{
    public async Task<PagedResult<AddressReferenceDto>> GetPagedAsync(int page, int pageSize)
    {
        IQueryable<Address> query = context.Set<Address>().AsNoTracking();

        return await query.ToPagedResultAsync(
            page,
            pageSize,
            includeTotalCount: true,
            MapToDto,
            q => q.OrderBy(x => x.City).ThenBy(x => x.Street));
    }

    public async Task<AddressReferenceDto> GetByIdAsync(int id)
    {
        Address entity = await context.Set<Address>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException(Messages.AddressNotFound);

        return MapToDto(entity);
    }

    public async Task<AddressReferenceDto> CreateAsync(AddressRequest request)
    {
        var entity = new Address
        {
            City = request.City.Trim(),
            Street = request.Street.Trim(),
            Number = request.Number.Trim()
        };

        context.Set<Address>().Add(entity);
        await context.SaveChangesAsync();

        return MapToDto(entity);
    }

    public async Task<AddressReferenceDto> UpdateAsync(int id, AddressRequest request)
    {
        Address entity = await context.Set<Address>()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException(Messages.AddressNotFound);

        entity.City = request.City.Trim();
        entity.Street = request.Street.Trim();
        entity.Number = request.Number.Trim();

        await context.SaveChangesAsync();

        return MapToDto(entity);
    }

    public async Task DeleteAsync(int id)
    {
        Address entity = await context.Set<Address>()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new NotFoundException(Messages.AddressNotFound);

        if (await accountService.IsAddressInUseAsync(id))
        {
            throw new BusinessException(Messages.AddressDeleteBlocked);
        }

        context.Set<Address>().Remove(entity);
        await context.SaveChangesAsync();
    }

    private static AddressReferenceDto MapToDto(Address entity) => new()
    {
        Id = entity.Id,
        City = entity.City,
        Street = entity.Street,
        Number = entity.Number
    };
}
