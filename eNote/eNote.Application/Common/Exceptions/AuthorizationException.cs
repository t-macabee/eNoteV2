namespace eNote.Application.Common.Exceptions
{
    public class AuthorizationException : Exception, IHasErrorCode
    {
        public const string DefaultMessage = "Forbidden.";
        public const string DefaultCode = "error.forbidden";

        public string Code => DefaultCode;

        public AuthorizationException() : base(DefaultMessage) { }
        public AuthorizationException(string message) : base(message) { }
    }
}
