using Microsoft.EntityFrameworkCore;

namespace eNote.Tests.TestUtils;

public static class TestManufacturers
{
    private static readonly Dictionary<string, int> Ids = new(StringComparer.Ordinal)
    {
        ["Fender"] = 3,
        ["Gibson"] = 4,
        ["Yamaha"] = 20,
        ["AAA"] = 21,
        ["BBB"] = 22,
        ["CCC"] = 23,
        ["KOR"] = 25,
        ["Maker"] = 41,
        ["Maker A"] = 26,
        ["Maker B"] = 27,
        ["Manufacturer"] = 28,
        ["ROL"] = 29,
        ["Second Maker"] = 30,
        ["Second Manufacturer"] = 31,
        ["TGT"] = 32,
        ["VM"] = 33,
        ["WM"] = 34,
        ["XM"] = 35,
        ["XXX"] = 36,
        ["YAM"] = 37,
        ["YM"] = 38,
        ["YYY"] = 39,
        ["ZM"] = 40
    };

    public static int Id(string name) => Ids[name];

    public static void Seed(ENoteContext context)
    {
        var existingIds = context.Set<Manufacturer>().Select(x => x.Id).ToHashSet();

        foreach (var (name, id) in Ids)
        {
            if (existingIds.Contains(id))
            {
                continue;
            }

            if (context.Database.IsRelational())
            {
                context.Database.ExecuteSqlRaw(
                    """
                    BEGIN TRY
                        IF NOT EXISTS (SELECT 1 FROM [Manufacturer] WHERE [Id] = {0})
                        BEGIN
                            SET IDENTITY_INSERT [Manufacturer] ON;
                            INSERT INTO [Manufacturer] ([Id], [Name]) VALUES ({0}, {1});
                            SET IDENTITY_INSERT [Manufacturer] OFF;
                        END
                    END TRY
                    BEGIN CATCH
                        SET IDENTITY_INSERT [Manufacturer] OFF;
                    END CATCH
                    """,
                    id,
                    name);
            }
            else
            {
                context.Set<Manufacturer>().Add(new Manufacturer { Id = id, Name = name });
            }
        }
    }
}
