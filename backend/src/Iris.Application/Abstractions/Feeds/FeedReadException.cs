namespace Iris.Application.Abstractions.Feeds;

public sealed class FeedReadException(string message, Exception innerException, bool isTimeout = false)
    : Exception(message, innerException)
{
    public bool IsTimeout { get; set; } = isTimeout;
}