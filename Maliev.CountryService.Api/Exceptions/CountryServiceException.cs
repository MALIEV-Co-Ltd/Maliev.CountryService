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