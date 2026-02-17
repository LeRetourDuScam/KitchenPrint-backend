using System;

namespace TIBG.API.Core.DataAccess
{
    /// <summary>
    /// Represents errors when communicating with the API.
    /// </summary>
    public class AiApiException : Exception
    {
        public AiApiException(string message) : base(message)
        {
        }

        public AiApiException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
