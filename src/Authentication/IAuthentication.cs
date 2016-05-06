using System.Net;
using System.Net.Http.Headers;

namespace RestService.Authentication
{
    /// <summary>
    /// Defines the interface for authentication
    /// </summary>
    public interface IAuthentication
    {
        /// <summary>
        /// Provides the credentials used to request the WebApi
        /// </summary>
        ICredentials Credentials { get; }

        /// <summary>
        /// When implemented, authenticates the client to use server resources
        /// </summary>
        AuthenticationHeaderValue Authenticate();
    }
}
