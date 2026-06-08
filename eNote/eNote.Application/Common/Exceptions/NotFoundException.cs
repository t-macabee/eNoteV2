namespace eNote.Application.Common.Exceptions
{
    public class NotFoundException : Exception, IHasErrorCode
    {
        public const string DefaultMessage = "Resource not found.";
        public const string DefaultCode = "error.not_found";

        public string Code => DefaultCode;

        public NotFoundException() : base(DefaultMessage) { }
        public NotFoundException(string message) : base(message) { }
    }
}
