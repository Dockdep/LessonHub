namespace LessonsHub.Application.Abstractions;

public enum ServiceErrorKind
{
    None,
    NotFound,
    BadRequest,
    Unauthorized,
    Forbidden,
    Conflict,
    Timeout,
    Internal
}

public sealed record ServiceResult<T>(T? Value, ServiceErrorKind Error, string? Message)
{
    public bool IsSuccess => Error == ServiceErrorKind.None;

    public static ServiceResult<T> Ok(T value) => new(value, ServiceErrorKind.None, null);
    public static ServiceResult<T> NotFound(string? message = null) => new(default, ServiceErrorKind.NotFound, message);
    public static ServiceResult<T> BadRequest(string message) => new(default, ServiceErrorKind.BadRequest, message);
    public static ServiceResult<T> Unauthorized(string? message = null) => new(default, ServiceErrorKind.Unauthorized, message);
    public static ServiceResult<T> Forbidden(string? message = null) => new(default, ServiceErrorKind.Forbidden, message);
    public static ServiceResult<T> Conflict(string message) => new(default, ServiceErrorKind.Conflict, message);
    public static ServiceResult<T> Timeout(string? message = null) => new(default, ServiceErrorKind.Timeout, message);
    public static ServiceResult<T> Internal(string message) => new(default, ServiceErrorKind.Internal, message);
}

public sealed record ServiceResult(ServiceErrorKind Error, string? Message)
{
    public bool IsSuccess => Error == ServiceErrorKind.None;

    public static ServiceResult Ok() => new(ServiceErrorKind.None, null);
    public static ServiceResult NotFound(string? message = null) => new(ServiceErrorKind.NotFound, message);
    public static ServiceResult BadRequest(string message) => new(ServiceErrorKind.BadRequest, message);
    public static ServiceResult Unauthorized(string? message = null) => new(ServiceErrorKind.Unauthorized, message);
    public static ServiceResult Forbidden(string? message = null) => new(ServiceErrorKind.Forbidden, message);
    public static ServiceResult Conflict(string message) => new(ServiceErrorKind.Conflict, message);
    public static ServiceResult Timeout(string? message = null) => new(ServiceErrorKind.Timeout, message);
    public static ServiceResult Internal(string message) => new(ServiceErrorKind.Internal, message);
}
