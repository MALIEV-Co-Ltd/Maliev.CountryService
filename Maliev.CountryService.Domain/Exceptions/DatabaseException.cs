using System;

namespace Maliev.CountryService.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when a database operation fails.
    /// </summary>
    public class DatabaseException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the DatabaseException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public DatabaseException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the DatabaseException class with a reference to the inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public DatabaseException(string message, Exception innerException) : base(message, innerException) { }
    }
}
