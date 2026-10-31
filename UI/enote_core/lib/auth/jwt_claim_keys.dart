class JwtClaimKeys {
  const JwtClaimKeys._();

  static const subject = 'sub';
  static const userName = 'unique_name';
  static const role =
      'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
  static const isManager = 'is_manager';
}
