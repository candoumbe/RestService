using System;
using RestService.Authentication;
using System.Net.Http.Formatting;

namespace RestService
{
    /// <summary>
    /// Represents the options to initiate a <see cref="T:WebApiRestService.WebApiClient`1"/> object
    /// </summary>
    public class WebApiClientOptions
    {
        private string _baseAddress;

        private ContentType _contentType;

        /// <summary>
        /// An object representing the desired type of authentication
        /// </summary>
        public IAuthentication Authentication { get; set; }

        /// <summary>
        /// Gets or sets the controller name that will be called from the client
        /// </summary>
        public string Controller { get; set; }

        /// <summary>
        /// Gets the content type used in request and response calls
        /// </summary>
        /// <remarks>
        /// If not defined, uses the default value <see cref="RestService.ContentType.Json"/>
        /// </remarks>
        /// <seealso cref="RestService.ContentType"/>
        public ContentType ContentType
        {
            get
            {
                return _contentType;
            }
            set
            {
                if (value == ContentType.Json)
                {
                    Formatter = new JsonMediaTypeFormatter();
                }
                else
                {
                    Formatter = new XmlMediaTypeFormatter();
                }

                _contentType = value;
            }
        }

        /// <summary>
        /// Gets the <see cref="System.Net.Http.Formatting.MediaTypeFormatter"/> associated to this instance
        /// </summary>
        public MediaTypeFormatter Formatter { get; private set; }

        /// <summary>
        /// Gets or sets the base address that will be used in request calls
        /// </summary>
        /// <remarks>
        /// If not defined, uses the default value http://localhost/
        /// </remarks>
        public string BaseAddress
        {
            get
            {
                return _baseAddress;
            }
            set
            {
                _baseAddress = value.EndsWith("/") ? value : $"{value}/";
            }
        }

        /// <summary>
        /// Gets or sets the default timeout limit for all client calls
        /// </summary>
        /// <remarks>
        /// If not defined, uses the default value 30000
        /// </remarks>
        public uint Timeout { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public WebApiClientOptions() : this("http://localhost/", null)
        {
        }

        /// <summary>
        /// Initializes the object that represents the options for the <see cref="T:WebApiRestService.WebApiClient`1"/> service
        /// </summary>
        /// <param name="baseAddress">The base address that will be used in request calls</param>
        /// <param name="controller">The controller name that will be called from the client</param>
        public WebApiClientOptions(string baseAddress, string controller)
        {
            if (string.IsNullOrEmpty(baseAddress)) throw new ArgumentNullException(nameof(baseAddress));
            //if (string.IsNullOrEmpty(controller)) throw new ArgumentNullException(nameof(controller));

            BaseAddress = baseAddress;
            Controller = controller;
            Authentication = new NoAuthentication();
            ContentType = ContentType.Json;
            Timeout = 30000;
            Formatter = new JsonMediaTypeFormatter();

        }
    }
}
