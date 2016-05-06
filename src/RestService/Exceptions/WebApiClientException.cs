using System;
using System.Net;

namespace RestService
{
    /// <summary>
    /// Represents an exception thrown by WebApiClient
    /// </summary>
    public class WebApiClientException : Exception
	{
        /// <summary>
		/// Error code
		/// </summary>
		public HttpStatusCode StatusCode { get; }

        /// <summary>
        /// Complete exception details
        /// </summary>
        public WebApiClientExceptionDetails Details { get; }

		/// <summary>
        /// Creates a new object of WebApiClientException
		/// </summary>
		/// <param name="statusCode">The error code</param>
		public WebApiClientException(HttpStatusCode statusCode) : base()
		{
            StatusCode = statusCode;
		}

		/// <summary>
        /// Creates a new object of WebApiClientException
		/// </summary>
		/// <param name="statusCode">The error code</param>
		/// <param name="message">The message</param>
		public WebApiClientException(HttpStatusCode statusCode, string message) : base(message)
		{
            StatusCode = statusCode;
		}

        /// <summary>
        /// Creates a new object of WebApiClientException
        /// </summary>
        /// <param name="statusCode">The error code</param>
        /// <param name="e">The inner exception</param>
        public WebApiClientException(HttpStatusCode statusCode, Exception e) : base("Error when trying to call the WebApi. See InnerException for more information", e)
        {
            StatusCode = statusCode;
        }

        /// <summary>
        /// Creates a new object of WebApiClientException
        /// </summary>
        /// <param name="statusCode">The error code</param>
        /// <param name="details">The object with further exception details</param>
        public WebApiClientException(HttpStatusCode statusCode, WebApiClientExceptionDetails details) : base("Error when trying to call the WebApi. See Details for more information")
        {
            StatusCode = statusCode;
            Details = details;
        }
    }
}
