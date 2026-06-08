namespace eNote.Application.Common.Exceptions
{
    public class ConflictException : Exception, IHasErrorCode
    {
        public const string DefaultMessage = "Conflict occurred.";
        public const string DefaultCode = "error.conflict";

        public string Code => DefaultCode;

        public ConflictException() : base(DefaultMessage) { }
        public ConflictException(string message) : base(message) { }
    }
}
