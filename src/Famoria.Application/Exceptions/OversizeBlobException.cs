namespace Famoria.Application.Exceptions;

/// <summary>
/// Thrown when the total size of attachments in an email exceeds the configured maximum threshold.
/// </summary>
public class OversizeBlobException : Exception
{
    private const string DefaultMessage = "Total attachment size exceeded the maximum allowed limit.";

    /// <summary>
    /// Initializes a new instance of the <see cref="OversizeBlobException"/> class with the default message.
    /// </summary>
    public OversizeBlobException()
        : base(DefaultMessage)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OversizeBlobException"/> class with a custom message.
    /// </summary>
    /// <param name="message">The custom exception message.</param>
    public OversizeBlobException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OversizeBlobException"/> class with a custom message and inner exception.
    /// </summary>
    /// <param name="message">The custom exception message.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public OversizeBlobException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
