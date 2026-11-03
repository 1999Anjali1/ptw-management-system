namespace PtwSystem.Api.Common;

/// <summary>Base for expected errors that should become a clean HTTP response instead of a 500.</summary>
public abstract class AppException(string message, int statusCode, IReadOnlyList<string>? errors = null) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public IReadOnlyList<string> Errors { get; } = errors ?? [message];
}

public class NotFoundException(string message) : AppException(message, StatusCodes.Status404NotFound);

public class ForbiddenException(string message) : AppException(message, StatusCodes.Status403Forbidden);

/// <summary>A PTW business rule blocked the operation (e.g. no gas test before issuing hot work).</summary>
public class BusinessRuleException : AppException
{
    public BusinessRuleException(string message) : base(message, StatusCodes.Status422UnprocessableEntity) { }

    public BusinessRuleException(IReadOnlyList<string> errors)
        : base(string.Join(" ", errors), StatusCodes.Status422UnprocessableEntity, errors) { }
}
