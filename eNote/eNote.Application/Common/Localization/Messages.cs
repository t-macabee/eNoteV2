namespace eNote.Application.Common.Localization;

public static class Messages
{
    public const string NotFound = "ID nije pronađen.";
    public const string BadRequest = "Neispravan zahtjev.";
    public const string InternalError = "Došlo je do greške na serveru.";
    public const string Unauthorized = "Niste autorizovani.";
    public const string Forbidden = "Nemate pristup ovom resursu.";
    public const string Conflict = "Sukob resursa.";

    public const string InvalidCredentials = "Pogrešno korisničko ime ili lozinka.";
    public const string AccountLocked = "Nalog je privremeno zaključan. Pokušajte ponovo kasnije.";
    public const string UsernameTaken = "Korisničko ime je već zauzeto.";
    public const string EmailTaken = "Email adresa je već registrovana.";
    public const string TokenRevoked = "Token je opozvan.";
    public const string TokenInvalid = "Token više nije važeći.";
    public const string InvalidUserClaim = "Autentificirani korisnik nema važeći identifikator.";
    public const string RoleMisconfigured = "Korisnički račun nije ispravno konfigurisan (uloge). Kontaktirajte administratora.";
    public const string UserSingleRoleRequired = "Korisnik mora imati tačno jednu ulogu.";
    public const string UserDeleteBlocked = "Korisnik ima evidentiranu historiju i ne može biti obrisan. Deaktivirajte ga umjesto toga.";
    public const string CannotModifyOwnAccount = "Ne možete mijenjati vlastiti nalog.";
    public const string UnknownRole = "Nepoznata uloga.";
    public const string StoreNotFound = "Radnja nije pronađena.";
    public const string MusicStoreRequiredForEmployee = "Prodavnica je obavezna za uposlenika radnje.";

    public const string StudentProfileNotFound = "Student profil nije pronađen.";
    public const string MembershipPaidUntilFuture = "PaidUntil mora biti u budućnosti.";
    public const string InstructorProfileNotFound = "Instruktor profil nije pronađen.";
    public const string EmployeeProfileNotFound = "Profil uposlenika radnje nije pronađen.";
    public const string ActiveEmployeeStoreNotFound = "Profil uposlenika radnje nije pronađen ili nije aktivan.";
    public const string ManagerRoleRequired = "Samo voditelj radnje može upravljati zaposlenicima.";

    public const string CourseNotFound = "Kurs nije pronađen.";
    public const string CourseIdRequired = "Kurs je obavezan.";
    public const string CourseNotOwned = "Niste vlasnik navedenog kursa.";

    public const string LectureNotFound = "Predavanje nije pronađeno.";
    public const string LectureCancelled = "Predavanje je otkazano.";
    public const string LectureFull = "Predavanje je popunjeno.";
    public const string LectureRsvpConflict = "Sukob pri rezervaciji predavanja. Pokušajte ponovo.";
    public const string AttendanceAlreadyMarked = "Prisustvo za ovog studenta je već evidentirano.";
    public const string LectureTimeConflict = "Termin predavanja se preklapa s postojećim predavanjem.";
    public const string LectureCapacityBelowConfirmed = "Kapacitet ne može biti manji od broja već potvrđenih prisustava.";

    public const string InstrumentTypeNotFound = "Vrsta instrumenta ne postoji.";
    public const string InstrumentNotFound = "Instrument nije pronađen.";
    public const string InstrumentReservedOrRented = "Instrument je rezervisan ili već iznajmljen.";
    public const string InstrumentDeleteBlocked = "Instrument se ne može obrisati jer je trenutno rezervisan ili iznajmljen.";

    public const string RentalNotFound = "Zahtjev nije pronađen.";
    public const string RentalNotFoundAfterUpdate = "Zahtjev nije pronađen nakon ažuriranja.";
    public const string RentalPendingRequired = "Već imate zahtjev na čekanju za ovaj instrument.";
    public const string RentalCancelPendingOrApprovedOnly = "Samo zahtjev na čekanju ili odobren zahtjev se može otkazati.";
    public const string RentalAccessDenied = "Nemate pravo nad ovim zahtjevom.";

    public const string PaymentAlreadyCompleted = "Stavka je već plaćena.";
    public const string PaymentNotPayableInStatus = "Iznajmljivanje nije u stanju koje dozvoljava plaćanje.";
    public const string RefundExceedsCharged = "Iznos povrata premašuje naplaćeni iznos.";
    public const string RefundFailed = "Povrat sredstava nije uspio.";
    public const string PaymentNotFound = "Plaćanje nije pronađeno.";
    public const string PaymentProviderUnavailable = "Plaćanje trenutno nije dostupno. Pokušajte ponovo kasnije.";
    public const string RentalUnpaidDebt = "Imate neizmireno dugovanje od prethodnog iznajmljivanja. Izmirite ga prije novog zahtjeva.";
    public const string StripeWebhookSignatureInvalid = "Neispravan Stripe webhook potpis.";

    public const string NotificationNotFound = "Notifikacija nije pronađena.";

    public const string AnnouncementNotFound = "Obavijest nije pronađena.";
    public const string AnnouncementCourseForbidden = "Nemate pravo objavljivati obavijesti za ovaj kurs.";

    public const string AssignmentNotFound = "Zadatak nije pronađen.";
    public const string AssignmentAlreadySubmitted = "Zadatak je već predan.";
    public const string AssignmentNotSubmitted = "Zadatak nije predan.";
    public const string AssignmentSubmissionNotFound = "Predaja zadatka nije pronađena.";
    public const string AssignmentInvalidGrade = "Ocjena mora biti između 0 i 100.";
    public const string LectureNoteNotFound = "Bilješka predavanja nije pronađena.";
    public const string StudentNotEnrolled = "Student nije upisan na kurs.";
    public const string AlreadyEnrolled = "Student je već upisan na kurs.";
    public const string CourseIsFree = "Kurs je besplatan.";
    public const string CourseNotPayable = "Kurs nije objavljen ili je završen.";
    public const string UnenrollBlockedByPendingPayment = "Nije moguće odjaviti kurs dok je plaćanje u toku. Završite ili otkažite plaćanje pa pokušajte ponovo.";
    public const string EnrollmentNotFound = "Upis nije pronađen.";
    public const string TuitionPaymentNotFound = "Plaćanje školarine nije pronađeno.";
    public const string TuitionNotPaid = "Školarina za ovaj kurs nije plaćena.";
    public const string MembershipInactive = "Članarina nije aktivna. Kontaktirajte administratora.";
    public const string AssignmentPastDue = "Rok za predaju zadatka je istekao.";

    public const string AddressNotFound = "Adresa nije pronađena.";
    public const string CityNotFound = "Grad nije pronađen.";
    public const string AddressDeleteBlocked = "Adresa se ne može obrisati jer je povezana s korisnikom.";
    public const string InstrumentTypeDeleteBlocked = "Vrsta instrumenta se ne može obrisati jer je u upotrebi.";
    public const string MusicStoreDeleteBlocked = "Radnja se ne može obrisati jer sadrži instrumente ili zaposlenike.";

    public const string PasswordResetEmailSent = "Ako nalog postoji, poslat je token za reset lozinke.";
    public const string PasswordResetFailed = "Reset lozinke nije uspio. Provjerite token i pokušajte ponovo.";

    public const string FileNotProvided = "Fajl nije priložen.";
    public const string FileTooLarge = "Veličina fajla prelazi maksimalno dozvoljenih 5 MB.";
    public const string InvalidFileFormat = "Dozvoljeni formati su JPEG, PNG i WebP.";

    public const string EventNotFound = "Događaj nije pronađen.";
    public const string EventEndsBeforeStarts = "Vrijeme završetka mora biti nakon vremena početka.";
    public const string AdminEventPlatformWideOnly = "Administrator može upravljati samo događajima na nivou platforme.";

    // Reports
    public const string ReportRankingTitle = "Rang lista";
    public const string ReportRentalSummaryTitle = "Pregled iznajmljivanja";
    public const string ReportAttendanceTitle = "Prisustvo";
    public const string ReportStoreReportTitle = "Izvještaj — Muzičke prodavnice";
    public const string ReportGeneratedLabel = "Generisano";
    public const string ReportColumnRank = "Rang";
    public const string ReportColumnStudent = "Student";
    public const string ReportColumnAverage = "Prosjek";
    public const string ReportColumnGraded = "Ocijenjeno";
    public const string ReportColumnId = "ID";
    public const string ReportColumnInstrument = "Instrument";
    public const string ReportColumnStatus = "Status";
    public const string ReportColumnFee = "Naknada";
    public const string ReportColumnTotal = "Ukupno";
    public const string ReportColumnName = "Naziv";
    public const string ReportColumnBusinessHours = "Radno vrijeme";
    public const string ReportCourseFallback = "Kurs";
    public const string ReportStoreFallback = "Prodavnica";
    public const string ReportStudentFallback = "Student";

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
