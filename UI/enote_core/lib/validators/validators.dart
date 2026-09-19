class Validators {
  static String? Function(String?) required(String? fieldName) =>
      (String? value) {
        if (value == null || value.trim().isEmpty) {
          return '${fieldName ?? 'Field'} je obavezan.';
        }
        return null;
      };

  static final RegExp _emailRegex = RegExp(
    r"^[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+)*@"
    r"[A-Za-z0-9](?:[A-Za-z0-9-]*[A-Za-z0-9])?"
    r"(?:\.[A-Za-z0-9](?:[A-Za-z0-9-]*[A-Za-z0-9])?)*"
    r"\.[A-Za-z](?:[A-Za-z0-9-]*[A-Za-z0-9])?$",
  );

  static String? email(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'Email adresa je obavezna.';
    }
    if (!_emailRegex.hasMatch(value.trim())) {
      return 'Unesite važeću email adresu.';
    }
    return null;
  }

  static String? password(String? value) {
    if (value == null || value.isEmpty) {
      return 'Lozinka je obavezna.';
    }
    if (value.length < 8) {
      return 'Lozinka mora imati najmanje 8 znakova.';
    }
    if (!RegExp(r'\p{Lu}', unicode: true).hasMatch(value)) {
      return 'Lozinka mora sadržavati najmanje jedno veliko slovo.';
    }
    if (!RegExp(r'\p{Ll}', unicode: true).hasMatch(value)) {
      return 'Lozinka mora sadržavati najmanje jedno malo slovo.';
    }
    if (!RegExp(r'\d').hasMatch(value)) {
      return 'Lozinka mora sadržavati najmanje jednu cifru.';
    }
    if (!RegExp(r'[^\p{L}\p{Nd}]', unicode: true).hasMatch(value)) {
      return 'Lozinka mora sadržavati najmanje jedan specijalni znak.';
    }
    return null;
  }

  static String? Function(String?) confirmPassword(
    String? Function() password,
  ) =>
      (String? value) {
        if (value == null || value.isEmpty) {
          return 'Potvrdite lozinku.';
        }
        if (value != password()) {
          return 'Lozinke se ne podudaraju.';
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

  static String? nonNegativeDecimal(String? value, {double? max}) {
    if (value == null || value.trim().isEmpty) {
      return 'Vrijednost je obavezna.';
    }
    final parsed = double.tryParse(value.trim().replaceAll(',', '.'));
    if (parsed == null) {
      return 'Unesite važeći broj.';
    }
    if (parsed < 0) {
      return 'Vrijednost ne smije biti negativna.';
    }
    if (max != null && parsed > max) {
      return 'Vrijednost ne smije biti veća od ${max.toStringAsFixed(0)}.';
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
