namespace Quay27.Application.Common.Exceptions;

public sealed class UpstreamDependencyException : Exception
{
    public string ErrorCode { get; }

    public UpstreamDependencyException(
        string message,
        string errorCode = "upstream_dependency_failure",
        Exception? innerException = null) : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
