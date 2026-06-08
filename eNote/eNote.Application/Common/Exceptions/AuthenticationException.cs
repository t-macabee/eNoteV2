namespace eNote.Application.Common.Exceptions
{
    public class AuthenticationException : Exception, IHasErrorCode
    {
        public const string DefaultMessage = "Unauthorized.";
        public const string DefaultCode = "error.unauthorized";

        public string Code => DefaultCode;

        public AuthenticationException() : base(DefaultMessage) { }
        public AuthenticationException(string message) : base(message) { }
    }
}
