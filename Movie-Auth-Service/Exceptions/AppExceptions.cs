namespace Movie_Auth_Service.Exceptions
{
    // 400 Bad Request
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }

    // 401 Unauthorized
    public class UnauthorizedException : Exception
    {
        public UnauthorizedException(string message) : base(message) { }
    }

    // 404 Not Found
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }

    // 409 Conflict
    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message) { }
    }
}