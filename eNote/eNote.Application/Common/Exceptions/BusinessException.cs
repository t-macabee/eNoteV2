namespace eNote.Application.Common.Exceptions
{
    public class BusinessException : Exception, IHasErrorCode
    {
        public const string DefaultMessage = "Business rule violation.";
        public const string DefaultCode = "error.business";

        public string Code => DefaultCode;

        public BusinessException() : base(DefaultMessage) { }
        public BusinessException(string message) : base(message) { }
    }
}

