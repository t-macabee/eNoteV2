using System.Linq.Expressions;
using System.Reflection;

namespace eNote.Application.Validation;

public static class ValidationDisplayNames
{
    private static readonly Dictionary<string, string> Names = new()
    {
        ["AddressId"] = "Adresa",
        ["Amount"] = "Iznos",
        ["AttendanceStatus"] = "Status prisustva",
        ["BusinessHours"] = "Radno vrijeme",
        ["Capacity"] = "Kapacitet",
        ["CityId"] = "Grad",
        ["ConfirmNewPassword"] = "Potvrda nove lozinke",
        ["Content"] = "Sadržaj",
        ["CourseId"] = "Kurs",
        ["CurrentPassword"] = "Trenutna lozinka",
        ["DateOfBirth"] = "Datum rođenja",
        ["Description"] = "Opis",
        ["DueAt"] = "Rok",
        ["Duration"] = "Trajanje",
        ["Email"] = "E-mail",
        ["EndDate"] = "Datum završetka",
        ["EndsAt"] = "Vrijeme završetka",
        ["Feedback"] = "Povratna informacija",
        ["FirstName"] = "Ime",
        ["Grade"] = "Ocjena",
        ["InstrumentId"] = "Instrument",
        ["InstrumentTypeId"] = "Vrsta instrumenta",
        ["LastName"] = "Prezime",
        ["LectureTime"] = "Vrijeme predavanja",
        ["LectureType"] = "Vrsta predavanja",
        ["Location"] = "Lokacija",
        ["Manufacturer"] = "Proizvođač",
        ["Message"] = "Poruka",
        ["Model"] = "Model",
        ["MonthlyFee"] = "Mjesečna naknada",
        ["Months"] = "Broj mjeseci",
        ["MusicStoreId"] = "Prodavnica",
        ["Name"] = "Naziv",
        ["NewPassword"] = "Nova lozinka",
        ["Note"] = "Napomena",
        ["Number"] = "Broj",
        ["PaidAt"] = "Datum uplate",
        ["Password"] = "Lozinka",
        ["PeriodEnd"] = "Kraj perioda",
        ["PhoneNumber"] = "Broj telefona",
        ["Price"] = "Cijena",
        ["Reason"] = "Razlog",
        ["ReceiptNumber"] = "Broj priznanice",
        ["Role"] = "Uloga",
        ["StartsAt"] = "Vrijeme početka",
        ["StoreName"] = "Naziv prodavnice",
        ["Street"] = "Ulica",
        ["StudentId"] = "Student",
        ["Title"] = "Naslov",
        ["Token"] = "Token",
        ["Type"] = "Tip",
        ["Username"] = "Korisničko ime"
    };

    public static IReadOnlyDictionary<string, string> DisplayNames => Names;

    public static string? Resolve(Type type, MemberInfo? member, LambdaExpression? expression) =>
        member is not null && Names.TryGetValue(member.Name, out var name) ? name : null;
}
