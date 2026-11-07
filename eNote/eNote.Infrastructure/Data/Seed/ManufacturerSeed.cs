namespace eNote.Infrastructure.Data.Seed;

internal static class ManufacturerSeed
{
    public const int Bach = 1;
    public const int Boss = 2;
    public const int Fender = 3;
    public const int Gibson = 4;
    public const int Ibanez = 5;
    public const int Jackson = 6;
    public const int Korg = 7;
    public const int Ludwig = 8;
    public const int Marshall = 9;
    public const int Martin = 10;
    public const int MusicMan = 11;
    public const int Orange = 12;
    public const int Pearl = 13;
    public const int Prs = 14;
    public const int Roland = 15;
    public const int Seagull = 16;
    public const int Tama = 17;
    public const int Taylor = 18;
    public const int Vox = 19;
    public const int Yamaha = 20;

    public static readonly (int Id, string Name)[] All =
    [
        (Bach, "Bach"),
        (Boss, "Boss"),
        (Fender, "Fender"),
        (Gibson, "Gibson"),
        (Ibanez, "Ibanez"),
        (Jackson, "Jackson"),
        (Korg, "Korg"),
        (Ludwig, "Ludwig"),
        (Marshall, "Marshall"),
        (Martin, "Martin"),
        (MusicMan, "Music Man"),
        (Orange, "Orange"),
        (Pearl, "Pearl"),
        (Prs, "PRS"),
        (Roland, "Roland"),
        (Seagull, "Seagull"),
        (Tama, "Tama"),
        (Taylor, "Taylor"),
        (Vox, "Vox"),
        (Yamaha, "Yamaha")
    ];
}
