namespace eNote.Application.Features.Identity.Employees;

public sealed class ShopEmployeeDto
{
    public int Id { get; init; }
    public int AppUserId { get; init; }
    public int MusicStoreId { get; init; }
    public string? StoreName { get; init; }
    public string? MusicStoreName => StoreName;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Username { get; init; }
    public bool IsManager { get; init; }
    public bool IsActive { get; init; }
}
