using System;

namespace Maliev.CountryService.Api.Exceptions;

public class CountryServiceException : Exception
{
    public CountryServiceException(string message) : base(message)
    {
    }

    public CountryServiceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class DuplicateCountryException : CountryServiceException
{
    public DuplicateCountryException(string message) : base(message)
    {
    }
}

public class DatabaseUnavailableException : CountryServiceException
{
    public DatabaseUnavailableException(string message) : base(message)
    {
    }
    
    public DatabaseUnavailableException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class ConcurrencyConflictException : CountryServiceException
{
    public ConcurrencyConflictException(string message) : base(message)
    {
    }
    
    public ConcurrencyConflictException(string message, Exception innerException) : base(message, innerException)
    {
    }
}