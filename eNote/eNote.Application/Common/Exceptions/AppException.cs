namespace eNote.Application.Common.Exceptions
{
    public abstract class AppException(int statusCode, string errorCode, string? message = null) : Exception(message ?? GetDefaultMessage(statusCode))
    {
        public int StatusCode { get; } = statusCode;
        public string ErrorCode { get; } = errorCode;

        private static string GetDefaultMessage(int statusCode) => statusCode switch
        {
            400 => "Bad request.",
            401 => "Niste autorizovani.",
            403 => "Nemate pristup ovom resursu.",
            404 => "ID nije pronađen.",
            409 => "Sukob resursa.",
            _ => "Došlo je do greške na serveru."
        };
    }
}   