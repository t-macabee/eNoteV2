class Validators {
  static String? Function(String?) required(String? fieldName) =>
      (String? value) {
        if (value == null || value.trim().isEmpty) {
          return '${fieldName ?? 'Polje'} je obavezan.';
        }
        return null;
      };

  static String? firstName(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'Ime je obavezno.';
    }
    return null;
  }

  static String? lastName(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'Prezime je obavezno.';
    }
    return null;
  }

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
      return 'Unesite e-mail adresu u obliku ime@domena.ba.';
    }
    return null;
  }

  static final RegExp _usernameRegex = RegExp(
    r'^[a-zA-Z0-9\-._@+čćšžđČĆŠŽĐ]+$',
  );

  static String? username(String? value) {
    final missing = required('Korisničko ime')(value);
    if (missing != null) return missing;
    if (!_usernameRegex.hasMatch(value!.trim())) {
      return 'Korisničko ime smije sadržavati samo slova, cifre i znakove - . _ @ +';
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
  ) => (String? value) {
    if (value == null || value.isEmpty) {
      return 'Potvrdite lozinku.';
    }
    if (value != password()) {
      return 'Lozinke se ne podudaraju.';
    }
    return null;
  };

  static String? optionalPhone(String? value) {
    final digits = value?.replaceAll(' ', '') ?? '';
    if (digits.isEmpty) return null;
    if (!RegExp(r'^\+?\d{6,15}$').hasMatch(digits)) {
      return 'Unesite broj telefona sa 6 do 15 cifara, opcionalno sa + na '
          'početku; razmaci su dozvoljeni (npr. +387 61 123 456).';
    }
    return null;
  }

  static final RegExp _businessHoursRegex = RegExp(
    r'^([01]\d|2[0-3]):[0-5]\d\s*-\s*([01]\d|2[0-3]):[0-5]\d$',
  );

  static String? businessHours(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'Radno vrijeme je obavezno (npr. 09:00-17:00).';
    }
    if (!_businessHoursRegex.hasMatch(value.trim())) {
      return 'Radno vrijeme unesite u obliku 09:00-17:00.';
    }
    return null;
  }

  static String? nonNegativeDecimal(
    String? value, {
    required String requiredMessage,
    double? min,
    double? max,
    int? maxDecimals,
  }) {
    if (value == null || value.trim().isEmpty) {
      return requiredMessage;
    }
    final raw = value.trim().replaceAll(',', '.');
    final parsed = double.tryParse(raw);
    if (parsed == null) {
      return 'Unesite važeći broj, npr. 25.00.';
    }
    if (parsed < 0) {
      return 'Vrijednost ne smije biti negativna.';
    }
    if (min != null && parsed < min) {
      return 'Vrijednost mora biti najmanje ${min.toStringAsFixed(0)}.';
    }
    if (max != null && parsed > max) {
      return 'Vrijednost ne smije biti veća od ${max.toStringAsFixed(0)}.';
    }
    if (maxDecimals != null) {
      final separator = raw.indexOf('.');
      if (separator >= 0 && raw.length - separator - 1 > maxDecimals) {
        return 'Vrijednost može imati najviše $maxDecimals decimale.';
      }
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
