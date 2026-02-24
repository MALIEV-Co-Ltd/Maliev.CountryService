namespace Maliev.CountryService.Api.Exceptions;

/// <summary>
/// Base exception class for Country Service specific errors.
/// </summary>
public class CountryServiceException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CountryServiceException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public CountryServiceException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CountryServiceException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public CountryServiceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when attempting to create a duplicate country.
/// </summary>
public class DuplicateCountryException : CountryServiceException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DuplicateCountryException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DuplicateCountryException(string message) : base(message)
    {
    }
}

/// <summary>
/// Exception thrown when the database is unavailable.
/// </summary>
public class DatabaseUnavailableException : CountryServiceException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseUnavailableException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DatabaseUnavailableException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseUnavailableException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public DatabaseUnavailableException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a concurrency conflict occurs.
/// </summary>
public class ConcurrencyConflictException : CountryServiceException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyConflictException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ConcurrencyConflictException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyConflictException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ConcurrencyConflictException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
