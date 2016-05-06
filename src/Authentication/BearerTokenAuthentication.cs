using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace RestService.Authentication
{
    /// <summary>
    /// Defines authentication using bearer tokens
    /// </summary>
    public class BearerTokenAuthentication : IAuthentication
    {
        private NetworkCredential credential = null;
        private string grantType = "password";

        /// <summary>
        /// Bearer Token Authentication doesn't use the Request's Authentication header to authenticate the user
        /// </summary>
        public ICredentials Credentials
        {
            get
            {
                return credential;
            }
        }

        /// <summary>
        /// Indicates whether token is present
        /// </summary>
        public bool HasToken
        {
            get;
            private set;
        }

        /// <summary>
        /// The generated token
        /// </summary>
        public string Token
        {
            get;
            private set;
        }

        /// <summary>
        /// The absolute uri that serves the token
        /// </summary>
        public string TokenUri { get; }

        /// <summary>
        /// Creates the object using a token
        /// </summary>
        /// <remarks>
        /// No calls will be made to the api to get the token once it is provided by user
        /// </remarks>
        /// <param name="token">A bearer token</param>
        public BearerTokenAuthentication(string token)
        {
            if (string.IsNullOrEmpty(token)) throw new ArgumentNullException(nameof(token));

            Token = token;
            HasToken = true;
        }

        /// <summary>
        /// Creates the object
        /// </summary>
        /// <param name="username">Username to use when getting the token</param>
        /// <param name="password">Password to use when getting the token</param>
        /// <param name="tokenUri">The token uri the service must call to get the token</param>
        public BearerTokenAuthentication(string username, string password, string tokenUri)
        {
            if (string.IsNullOrEmpty(username)) throw new ArgumentNullException(nameof(username));
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));
            if (string.IsNullOrEmpty(tokenUri)) throw new ArgumentNullException(nameof(tokenUri));

            credential = new NetworkCredential(username, password);
            TokenUri = tokenUri;
        }

        /// <summary>
        /// If the bearer token is already present, just return it. If not, makes the call to <c>TokenUri</c> to get the token
        /// </summary>
        /// <returns>The <see cref="System.Net.Http.Headers.AuthenticationHeaderValue"/> authentication header</returns>
        public AuthenticationHeaderValue Authenticate()
        {
            string token = null;

            if (HasToken)
            {
                //Token was provided by user
                token = Token;
            }
            else
            {
                HttpResponseMessage response = null;
                
                //Gets the parameters that should be sent in the content body part of the request in a queryString format 
                string contentBody = GetParams().ParseAnonymousObject().ToQueryString().Replace("?", string.Empty);

                //Loads the Content object
                HttpContent content = new StringContent(contentBody);
                content.LoadIntoBufferAsync();

                using (HttpClient client = new HttpClient())
                {
                    //Makes the call to get the token
                    response = client.PostAsync(TokenUri, content).Result;


                    if (response.IsSuccessStatusCode)
                    {
                        //Gets the result in json format
                        var result = response.Content.ReadAsStringAsync().Result;

                        //Parses the result in a JObject
                        JObject json = JObject.Parse(result);

                        //Gets the token part
                        token = json["access_token"].ToString();
                    }
                    else
                    {
                        if (response.StatusCode == HttpStatusCode.NotFound)
                        {
                            //TokenUri is invalid
                            throw new WebApiClientException(HttpStatusCode.ServiceUnavailable, "TokenUri not found");
                        }

                        //Throws a Exception because errors have happend
                        throw new WebApiClientException(HttpStatusCode.Unauthorized, "Unauthorized access");
                    }
                }
            }

            //Stores the current token for subsequent calls
            Token = token;
            HasToken = true;

            //Returns the authentication header
            return new AuthenticationHeaderValue("Bearer", token);
        }

        /// <summary>
        /// Gets the parameters that should be sent in the content body
        /// </summary>
        /// <returns>The parameters as an anonymous type object</returns>
        private object GetParams()
        {
            return new { username = credential.UserName, password = credential.Password, grant_type = grantType };
        }
    }
}
