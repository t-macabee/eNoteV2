using eNote.Application.Common.Paging;
using eNote.Application.Features.Academic.Courses;
using eNote.Tests.TestUtils;
using Microsoft.EntityFrameworkCore;

namespace eNote.Tests.Common;

public sealed class PagingExtensionsTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ToPagedResultAsync_WithoutOrderBy_OrdersById()
    {
        await using var context = await CreateContextWithTiedCoursesAsync();

        var result = await context.Set<Course>().AsNoTracking()
            .ToPagedResultAsync(new CourseSearchObject { Page = 1, PageSize = 10 }, c => c.Id);

        Assert.Equal(new[] { 1, 2, 3 }, result.Items);
    }

    [Fact]
    public async Task ToPagedResultAsync_WithOrderBy_OrdersTiesById()
    {
        await using var context = await CreateContextWithTiedCoursesAsync();

        var result = await context.Set<Course>().AsNoTracking()
            .ToPagedResultAsync(new CourseSearchObject { Page = 1, PageSize = 10 }, c => c.Id, q => q.OrderBy(c => c.StartDate));

        Assert.Equal(new[] { 1, 2, 3 }, result.Items);
    }

    private static async Task<ENoteContext> CreateContextWithTiedCoursesAsync()
    {
        var context = TestDbContextFactory.CreateContext(Now);
        var instructor = new Instructor(100);
        context.Set<Instructor>().Add(instructor);
        await context.SaveChangesAsync();

        var third = new Course("Course C", null, 10m, Now, null, instructor.Id) { Id = 3 };
        var first = new Course("Course A", null, 10m, Now, null, instructor.Id) { Id = 1 };
        var second = new Course("Course B", null, 10m, Now, null, instructor.Id) { Id = 2 };
        context.Set<Course>().Add(third);
        await context.SaveChangesAsync();
        context.Set<Course>().Add(first);
        await context.SaveChangesAsync();
        context.Set<Course>().Add(second);
        await context.SaveChangesAsync();

        return context;
    }
}
