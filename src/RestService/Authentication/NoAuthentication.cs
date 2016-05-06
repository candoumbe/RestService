using System.Net;
using System.Net.Http.Headers;

namespace RestService.Authentication
{
    /// <summary>
    /// Defines no authentication
    /// </summary>
    public class NoAuthentication : IAuthentication
    {
        /// <summary>
        /// Not necessary for this kind of authentication
        /// </summary>
        public AuthenticationHeaderValue Authenticate()
        {
            return null;
        }

        /// <summary>
        /// Not used
        /// </summary>
        public ICredentials Credentials
        {
            get { return null; }
        }
    }
}
