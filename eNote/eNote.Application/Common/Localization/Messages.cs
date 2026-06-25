namespace eNote.Application.Common.Localization;

public static class Messages
{
    public const string NotFound = "ID nije pronađen.";
    public const string BadRequest = "Neispravan zahtjev.";
    public const string InternalError = "Došlo je do greške na serveru.";

    public const string InvalidCredentials = "Pogrešno korisničko ime ili lozinka.";
    public const string AccountLocked = "Nalog je privremeno zaključan. Pokušajte ponovo kasnije.";
    public const string UsernameTaken = "Korisničko ime je već zauzeto.";
    public const string EmailTaken = "Email adresa je već registrovana.";
    public const string TokenRevoked = "Token je opozvan.";
    public const string InvalidUserClaim = "Autentificirani korisnik nema važeći identifikator.";
    public const string RoleMisconfigured = "Korisnički račun nije ispravno konfigurisan (uloge). Kontaktirajte administratora.";

    public const string UserSingleRoleRequired = "Korisnik mora imati tačno jednu ulogu.";
    public const string UnknownRole = "Nepoznata uloga.";
    public const string StoreNotFound = "Radnja nije pronađena.";

    public const string StudentProfileNotFound = "Student profil nije pronađen.";
    public const string InstructorProfileNotFound = "Instruktor profil nije pronađen.";
    public const string EmployeeProfileNotFound = "Profil uposlenika radnje nije pronađen.";
    public const string ActiveEmployeeStoreNotFound = "Profil uposlenika radnje nije pronađen ili nije aktivan.";

    public const string CourseNotFound = "Kurs nije pronađen.";
    public const string CourseIdRequired = "Kurs je obavezan.";
    public const string CourseNotOwned = "Niste vlasnik navedenog kursa.";

    public const string LectureNotFound = "Predavanje nije pronađeno.";
    public const string LectureCancelled = "Predavanje je otkazano.";
    public const string LectureFull = "Predavanje je popunjeno.";
    public const string LectureRsvpConflict = "Sukob pri rezervaciji predavanja. Pokušajte ponovo.";

    public const string InstrumentTypeNotFound = "Vrsta instrumenta ne postoji.";
    public const string InstrumentNotFound = "Instrument nije pronađen.";
    public const string InstrumentInactive = "Instrument nije aktivan.";
    public const string InstrumentReservedOrRented = "Instrument je rezervisan ili već iznajmljen.";
    public const string InstrumentDeleteBlocked = "Instrument se ne može obrisati jer je trenutno rezervisan ili iznajmljen.";

    public const string RentalNotFound = "Zahtjev nije pronađen.";
    public const string RentalNotFoundAfterUpdate = "Zahtjev nije pronađen nakon ažuriranja.";
    public const string RentalPendingRequired = "Već imate zahtjev na čekanju za ovaj instrument.";
    public const string RentalApprovePendingOnly = "Samo zahtjev na čekanju može biti odobren.";
    public const string RentalRejectPendingOnly = "Samo zahtjev na čekanju se može odbiti.";
    public const string RentalPickupApprovedOnly = "Samo odobreno iznajmljivanje se može preuzeti.";
    public const string RentalAlreadyPickedUp = "Instrument je već preuzet.";
    public const string RentalCompleteActiveOnly = "Samo aktivno iznajmljivanje se može završiti.";
    public const string RentalAlreadyCompleted = "Iznajmljivanje je već završeno.";
    public const string RentalCancelPendingOrApprovedOnly = "Samo zahtjev na čekanju ili odobren zahtjev se može otkazati.";
    public const string RentalCancelBlockedAfterPickup = "Instrument je već preuzet, otkazivanje nije moguće.";
    public const string RentalEarlyReturnActiveOnly = "Samo aktivno iznajmljivanje se može prijevremeno završiti.";
    public const string RentalNotPickedUp = "Instrument nije preuzet.";
    public const string RentalInstrumentMissing = "Instrument nije pronađen za ovaj zahtjev.";
    public const string RentalAccessDenied = "Nemate pravo nad ovim zahtjevom.";

    public const string NotificationNotFound = "Notifikacija nije pronađena.";

    public const string AnnouncementNotFound = "Obavijest nije pronađena.";
    public const string AnnouncementCourseForbidden = "Nemate pravo objavljivati obavijesti za ovaj kurs.";

    public const string AssignmentNotFound = "Zadatak nije pronađen.";
    public const string AssignmentAlreadySubmitted = "Zadatak je već predan.";
    public const string AssignmentSubmissionNotFound = "Predaja zadatka nije pronađena.";
    public const string AssignmentInvalidGrade = "Ocjena mora biti između 0 i 100.";
    public const string LectureNoteNotFound = "Bilješka predavanja nije pronađena.";
    public const string StudentNotEnrolled = "Student nije upisan na kurs.";
    public const string MembershipInactive = "Članarina nije aktivna. Kontaktirajte administratora.";
    public const string AssignmentPastDue = "Rok za predaju zadatka je istekao.";

    public const string AddressNotFound = "Adresa nije pronađena.";
    public const string AddressDeleteBlocked = "Adresa se ne može obrisati jer je povezana s korisnikom.";
    public const string InstrumentTypeDeleteBlocked = "Vrsta instrumenta se ne može obrisati jer je u upotrebi.";
    public const string MusicStoreDeleteBlocked = "Radnja se ne može obrisati jer sadrži instrumente ili zaposlenike.";

    public const string PasswordResetEmailSent = "Ako nalog postoji, poslat je token za reset lozinke.";
    public const string PasswordResetFailed = "Reset lozinke nije uspio. Provjerite token i pokušajte ponovo.";

    public const string FileNotProvided = "Fajl nije priložen.";
    public const string FileTooLarge = "Veličina fajla prelazi maksimalno dozvoljenih 5 MB.";
    public const string InvalidFileFormat = "Dozvoljeni formati su JPEG, PNG i WebP.";

    public static string RoleCreateFailed(string role, string errors) =>
        $"Greška pri kreiranju uloge {role}: {errors}";

    public static string UserCreateFailed(string username, string errors) =>
        $"Greška pri kreiranju korisnika {username}: {errors}";

    public static string UserUpdateFailed(string username, string errors) =>
        $"Greška pri ažuriranju korisnika {username}: {errors}";

    public static string UserRoleRemoveFailed(string username, string errors) =>
        $"Greška pri uklanjanju uloga korisnika {username}: {errors}";

    public static string UserRoleAssignFailed(string role, string username, string errors) =>
        $"Greška pri dodjeli uloge {role} korisniku {username}: {errors}";
}
