class Validators {
  static String? Function(String?) required(String? fieldName) =>
      (String? value) {
        if (value == null || value.trim().isEmpty) {
          return '${fieldName ?? 'Field'} je obavezan.';
        }
        return null;
      };

  static String? email(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'Email adresa je obavezna.';
    }
    final emailRegex = RegExp(r'^[\w-\.]+@([\w-]+\.)+[\w-]{2,}$');
    if (!emailRegex.hasMatch(value.trim())) {
      return 'Unesite važeću email adresu.';
    }
    return null;
  }

  static String? password(String? value) {
    if (value == null || value.isEmpty) {
      return 'Lozinka je obavezna.';
    }
    if (value.length < 8) {
      return 'Lozinka mora imati najmanje 8 karaktera.';
    }
    if (!RegExp(r'[A-Z]').hasMatch(value)) {
      return 'Lozinka mora sadržati najmanje jedno veliko slovo.';
    }
    if (!RegExp(r'[a-z]').hasMatch(value)) {
      return 'Lozinka mora sadržati najmanje jedno malo slovo.';
    }
    if (!RegExp(r'[0-9]').hasMatch(value)) {
      return 'Lozinka mora sadržati najmanje jednu cifru.';
    }
    if (!RegExp(r'[!@#$%^&*(),.?":{}|<>]').hasMatch(value)) {
      return 'Lozinka mora sadržati najmanje jedan specijalni karakter.';
    }
    return null;
  }

  static String? Function(String?) confirmPassword(
    String? password,
  ) =>
      (String? value) {
        if (value == null || value.isEmpty) {
          return 'Potvrdite lozinku.';
        }
        if (value != password) {
          return 'Lozinka i potvrda se ne poklapaju.';
        }
        return null;
      };

  static String? phone(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'Telefon je obavezan.';
    }
    final phoneRegex = RegExp(r'^\+?\d{6,15}$');
    if (!phoneRegex.hasMatch(value.trim())) {
      return 'Unesite važeći broj telefona.';
    }
    return null;
  }

  static String? nonNegativeDecimal(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'Vrednost je obavezna.';
    }
    final parsed = double.tryParse(value.trim().replaceAll(',', '.'));
    if (parsed == null) {
      return 'Unesite važeći broj.';
    }
    if (parsed < 0) {
      return 'Vrednost ne sme biti negativna.';
    }
    return null;
  }

  static String? Function(String?) grade(int min, int max) => (String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'Ocjena je obavezna.';
    }
    final parsed = int.tryParse(value.trim());
    if (parsed == null) {
      return 'Unesite brojčanu ocjenu.';
    }
    if (parsed < min || parsed > max) {
      return 'Ocjena mora biti između $min i $max.';
    }
    return null;
  };

}
