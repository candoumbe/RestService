using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace RestService
{
    /// <summary>
    /// Represents a generic rest client able to make calls to a Web Api 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class WebApiClient<T> : IRestClient<T> where T : class
	{
		/// <summary>
		/// The <see cref="System.Net.Http.HttpClient"/> used in this instance
		/// </summary>
		protected HttpClient Client;

        /// <summary>
        /// Gets the HttpClient headers
        /// </summary>
        public HttpRequestHeaders Headers { get; private set; }

        /// <summary>
        /// Gets the <see cref="T:WebApiRestService.WebApiClientOptions"/> associated with this client
        /// </summary>
        /// <remarks>
        /// Just for reference purpose because any change made to this object won't have any effect because it is used
        /// only to create the client instance with default values
        /// </remarks>
        public WebApiClientOptions Options { get; private set; }
        
        /// <summary>
        /// Gets the <see cref="System.Net.Http.HttpClientHandler"/> associated with this client
        /// </summary>
        /// <remarks>
        /// Just for reference purpose because any change made to this handler won't have any effect once it is used 
        /// in the <see cref="System.Net.Http.HttpClient"/> constructor and cannot be changed
        /// </remarks>
        public HttpClientHandler Handler { get; private set; }

        /// <summary>
		/// Initializes a new instance of <see cref="T:WebApiRestService.WebApiClient`1"/> using default options
		/// </summary>
		public WebApiClient() : this(new WebApiClientOptions())
		{

		}

		/// <summary>
        /// Initializes a new instance of <see cref="T:WebApiRestService.WebApiClient`1"/> using custom options
		/// </summary>
		/// <param name="options">Custom options to be used</param>
		/// <exception cref="System.ArgumentNullException" />
        public WebApiClient(WebApiClientOptions options) : this(options, new HttpClientHandler())
        { 
        }
		
        /// <summary>
        /// Initializes a new instance of <see cref="T:WebApiRestService.WebApiClient`1"/> using custom options
		/// </summary>
		/// <param name="options">Custom options to be used</param>
        /// <param name="handler">The desired handler</param>
		/// <exception cref="System.ArgumentNullException" />
		public WebApiClient(WebApiClientOptions options, HttpClientHandler handler)
		{
			if (options == null) throw new ArgumentNullException(nameof(options), $"{nameof(options)} parameter is required");
            if (handler == null) throw new ArgumentNullException(nameof(handler), $"{nameof(handler)} parameter is required");

            Options = options;
            Handler = handler;

            //Set the credentials to access resources, if provided
            handler.Credentials = options.Authentication.Credentials;

            //Creates the httpClient and sets default properties
            Client = new HttpClient(handler);
            Client.BaseAddress = new Uri(Options.BaseAddress);

            //Sets the default request header properties
            Headers = Client.DefaultRequestHeaders;
            Headers.Accept.Clear();
            Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(Options.ContentType.ToMediaFormat()));
		}

		/// <summary>
		/// Generates a Uri string based on the parameters provided
		/// </summary>
		/// <remarks>
		/// The generated Uri can be formatted in one of the following types:
		/// <list type="bullet">
		/// <item><description>{controller}</description></item>
		/// <item><description>{controller}/{param}</description></item>
		/// <item><description>{controller}?{param.key1}={param.value1}&amp;{param.key2}={param.value2}</description></item>
		/// <item><description>{controller}/{action}</description></item>
		/// <item><description>{controller}/{action}/{param}</description></item>
		/// <item><description>{controller}/{action}?{param.key1}={param.value1}&amp;{param.key2}={param.value2}</description></item>
		/// </list>
		/// </remarks>
		/// <param name="controller">The controller that will be called. Required.</param>
		/// <param name="action">The action that will be called. Optional.</param>
		/// <param name="param">Can be a simple type like int, string, etc or an anonymous type defining many properties. Optional.</param>
		/// <returns>The complete Uri that will be used to call the Web Api</returns>
		/// <exception cref="System.ArgumentNullException" />
		protected string GenerateUri(string controller, string action = null, object param = null)
		{
            if (string.IsNullOrEmpty(controller))
            {
                throw new ArgumentNullException(nameof(controller), $"{nameof(controller)} parameter is required");
            }

			string uri = string.Empty;

            uri = controller;
                

			if (!string.IsNullOrEmpty(action))
			{
				//If action is provided, append it
				uri = $"{uri}/{action}";
			}

			if (param != null)
			{
				if (param.GetType().IsAnonymousType())
				{
					//Param is an anonymous type, so convert it to a dictionary
					uri += param.ParseAnonymousObject().ToQueryString();
				}
				else
				{
                    //Is not an anonymous type. Just append it
                    uri = $"{uri}/{param}";

                }
			}
			
			return uri;
		}

        /// <summary>
        /// Gets the Authorization header
        /// </summary>
        private void Authenticate()
        {
            Client.DefaultRequestHeaders.Authorization = Options.Authentication.Authenticate();
        }

        /// <summary>
        /// Encapsulates the error response into a WebApiClientException
        /// </summary>
        /// <param name="response">The response message containing the error</param>
        private WebApiClientException GetException(HttpResponseMessage response)
        {
            WebApiClientExceptionDetails details = new WebApiClientExceptionDetails();

            //Gets the content string where exception details are defined
            string content = response.Content.ReadAsStringAsync().Result;

            try
            {
                Dictionary<string, IList<string>> modelState = null;

                //Parses the content string into a json object
                var json = JObject.Parse(content);
                
                //If ModelState is informed, get it
                IDictionary<string, JToken> jModelState = (JObject)json["ModelState"];

                //Transforms the ModelState into a Dictionary
                if(jModelState != null)
                    modelState = ((JObject)json["ModelState"]).ToObject<Dictionary<string, IList<string>>>();

                string message = (string)json["Message"];
                string exceptionMessage = (string)json["ExceptionMessage"];

                //Creates the details object setting its values
                details.Message = exceptionMessage ?? message;
                details.ExceptionType = (string)json["ExceptionType"];
                details.StackTrace = (string)json["StackTrace"];
                details.ModelState = modelState;
            }
            catch
            {
                if (!string.IsNullOrEmpty(content))
                {
                    //Content isn't in json format but still contains useful information
                    details.Message = content;
                }
            }

            //Sets the reason phrase
            details.Reason = response.ReasonPhrase;
            
            //Return the exception
            return new WebApiClientException(response.StatusCode, details);
        }

        /// <summary>
		/// Disposes resources
		/// </summary>
		public void Dispose()
		{
			Client.Dispose();
		}


        /// <summary>
        /// Adds a cookie to the request to be sent to the WebApi
        /// </summary>
        /// <param name="name">Cookie name</param>
        /// <param name="value">Cookie value</param>
        /// <param name="path">Cookie path</param>
        /// <param name="domain">Cookie domain</param>
        /// <exception cref="System.ArgumentNullException" />
        public void AddCookie(string name, string value, string path, string domain)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrEmpty(domain)) throw new ArgumentNullException(nameof(domain));

            AddCookie(new Cookie(name, value, path, domain));
        }

        /// <summary>
        /// Adds a collection of cookies to the request to be sent to the WebApi
        /// </summary>
        /// <param name="cookies">The cookie collection</param>
        /// <exception cref="System.ArgumentNullException" />
        public void AddCookie(CookieCollection cookies)
        {
            if (cookies == null) throw new ArgumentNullException(nameof(cookies));

            foreach (Cookie cookie in cookies)
            {
                AddCookie(cookie);
            }
        }

        /// <summary>
        /// Adds a cookie to the request to be sent to the WebApi
        /// </summary>
        /// <param name="cookie">The Cookie object</param>
        /// <exception cref="System.ArgumentNullException" />
        public void AddCookie(Cookie cookie)
        {
            if (cookie == null) throw new ArgumentNullException(nameof(cookie));

            if (Handler.UseCookies)
            {
                //Handler uses CookieContainer so we can use it as well
                Handler.CookieContainer.SetCookies(new Uri(Options.BaseAddress), cookie.ToString());
            }
            else
            {
                //Handler isn't using CookieContainer so we can just insert the cookie into the header
                Headers.Add("Cookie", cookie.ToString());
            }
        }

        /// <summary>
		/// Send an object to the Web Api to be created
		/// </summary>
		/// <param name="obj">The object to be created</param>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> that will yield the created object of type T</returns>
        /// <remarks>
		/// Requests are sent using HttpVerb POST
		/// </remarks>
		/// <exception cref="RestService.WebApiClientException" />
		/// <exception cref="System.ArgumentNullException" />
		public async virtual Task<T> CreateAsync(T obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            CancellationTokenSource src = new CancellationTokenSource((int)Options.Timeout);

            return await CreateAsync(obj, Options.Controller, null, src.Token);
        }

        /// <summary>
        /// Send an object to the Web Api to be created
        /// </summary>
        /// <param name="obj">The object to be created</param>
        /// <param name="token">The <see cref="System.Threading.CancellationToken"/> to be used to cancel the call</param>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> that will yield the created object of type T</returns>
        /// <remarks>
        /// Requests are sent using HttpVerb POST
        /// </remarks>
        /// <exception cref="RestService.WebApiClientException" />
        /// <exception cref="System.ArgumentNullException" />
        public async virtual Task<T> CreateAsync(T obj, CancellationToken token)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (token == null) throw new ArgumentNullException(nameof(token));

            return await CreateAsync(obj, Options.Controller, null, token);
        }

        /// <summary>
        /// Send an object to the Web Api to be created
        /// </summary>
        /// <param name="obj">The object to be created</param>
        /// <param name="action">Explicit define the action that will be called</param>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> that will yield the created object of type T</returns>
        /// <remarks>
        /// Requests are sent using HttpVerb POST
        /// </remarks>
        /// <exception cref="RestService.WebApiClientException" />
        /// <exception cref="System.ArgumentNullException" />
        public async virtual Task<T> CreateAsync(T obj, string action)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (string.IsNullOrEmpty(action)) throw new ArgumentNullException(nameof(action));

            CancellationTokenSource src = new CancellationTokenSource((int)Options.Timeout);

            return await CreateAsync(obj, Options.Controller, action, src.Token);
        }

        /// <summary>
        /// Send an object to the Web Api to be created
        /// </summary>
        /// <param name="obj">The object to be created</param>
        /// <param name="action">Explicit define the action that will be called</param>
        /// <param name="token">The <see cref="System.Threading.CancellationToken"/> to be used to cancel the call</param>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> that will yield the created object of type T</returns>
        /// <remarks>
        /// Requests are sent using HttpVerb POST
        /// </remarks>
        /// <exception cref="RestService.WebApiClientException" />
        /// <exception cref="System.ArgumentNullException" />
        public async virtual Task<T> CreateAsync(T obj, string action, CancellationToken token)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (string.IsNullOrEmpty(action)) throw new ArgumentNullException(nameof(action));
            if (token == null) throw new ArgumentNullException(nameof(token));

            return await CreateAsync(obj, Options.Controller, action, token);
        }

        /// <summary>
        /// Send an object to the Web Api to be created
        /// </summary>
        /// <param name="obj">The object to be created</param>
        /// <param name="controller">The controller that will be called</param>
        /// <param name="action">Explicit define the action that will be called</param>
        /// <param name="token">The <see cref="System.Threading.CancellationToken"/> to be used to cancel the call</param>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> that will yield the created object of type T</returns>
        /// <remarks>
        /// Requests are sent using HttpVerb POST
        /// </remarks>
        /// <exception cref="RestService.WebApiClientException" />
        /// <exception cref="System.ArgumentNullException" />
        protected async virtual Task<T> CreateAsync(T obj, string controller, string action, CancellationToken token)
        {
            try
            {
#if DEBUG
                //When debugging timeouts, sometimes the operation completes so fast that is impossible to recreate the scenario
                await Task.Delay(50);
#endif

                //Authenticates if needed
                Authenticate();

                //Loads the object into the content's buffer. If we call PutAsync passing obj instead of content, an error occurs
                HttpContent content = new ObjectContent<T>(obj, Options.Formatter);
                await content.LoadIntoBufferAsync();

                //Makes the async call 
                var response = await Client.PostAsync(GenerateUri(controller, action, null), content, token);

                if (!response.IsSuccessStatusCode)
                {
                    //Packs the error response into a WebApiClientException
                    throw GetException(response);
                }

                try
                {
                    //Returns the object
                    return await response.Content.ReadAsAsync<T>(token);
                }
                catch
                {
                    return default(T);
                }
            }
            catch (OperationCanceledException)
            {
                //Timeout or operation canceled by user
                throw new WebApiClientException(HttpStatusCode.RequestTimeout, "Task canceled due to timeout or token cancellation");
            }
            catch (WebApiClientException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                //Generic exception
                throw new WebApiClientException(HttpStatusCode.InternalServerError, e);
            }
        }

        /// <summary>
		/// Calls the Web Api to delete the object of type T
		/// </summary>
		/// <param name="param">Can be a simple type like int, string, etc or an anonymous type defining many properties</param>
		/// <remarks>
		/// Requests are sent using HttpVerb DELETE
		/// </remarks>
		/// <exception cref="RestService.WebApiClientException" />
		/// <exception cref="System.ArgumentNullException" />
		public async virtual Task DeleteAsync(object param)
        {
            if (param == null) throw new ArgumentNullException(nameof(param));

            CancellationTokenSource src = new CancellationTokenSource((int)Options.Timeout);

            await DeleteAsync(param, Options.Controller, null, src.Token);
        }

        /// <summary>
        /// Calls the Web Api to delete the object of type T
        /// </summary>
        /// <param name="param">Can be a simple type like int, string, etc or an anonymous type defining many properties</param>
        /// <param name="token">The <see cref="System.Threading.CancellationToken"/> to be used to cancel the call</param>
        /// <remarks>
        /// Requests are sent using HttpVerb DELETE
        /// </remarks>
        /// <exception cref="RestService.WebApiClientException" />
        /// <exception cref="System.ArgumentNullException" />
        public async virtual Task DeleteAsync(object param, CancellationToken token)
        {
            if (param == null) throw new ArgumentNullException(nameof(param));
            if (token == null) throw new ArgumentNullException(nameof(token));

            await DeleteAsync(param, Options.Controller, null, token);
        }

        /// <summary>
        /// Calls the Web Api to delete the object of type T
        /// </summary>
        /// <param name="param">Can be a simple type like int, string, etc or an anonymous type defining many properties</param>
        /// <param name="action">Explicit define the action that will be called</param>
        /// <remarks>
        /// Requests are sent using HttpVerb DELETE
        /// </remarks>
        /// <exception cref="RestService.WebApiClientException" />
        /// <exception cref="System.ArgumentNullException" />
        public async virtual Task DeleteAsync(object param, string action)
        {
            if (param == null) throw new ArgumentNullException(nameof(param));
            if (string.IsNullOrEmpty(action)) throw new ArgumentNullException(nameof(action));

            CancellationTokenSource src = new CancellationTokenSource((int)Options.Timeout);

            await DeleteAsync(param, Options.Controller, action, src.Token);
        }

        /// <summary>
        /// Calls the Web Api to delete the object of type T
        /// </summary>
        /// <param name="param">Can be a simple type like int, string, etc or an anonymous type defining many properties</param>
        /// <param name="action">Explicit define the action that will be called</param>
        /// <param name="token">The <see cref="System.Threading.CancellationToken"/> to be used to cancel the call</param>
        /// <remarks>
        /// Requests are sent using HttpVerb DELETE
        /// </remarks>
        /// <exception cref="RestService.WebApiClientException" />
        /// <exception cref="System.ArgumentNullException" />
        public async virtual Task DeleteAsync(object param, string action, CancellationToken token)
        {
            if (param == null) throw new ArgumentNullException(nameof(param));
            if (string.IsNullOrEmpty(action)) throw new ArgumentNullException(nameof(action));
            if (token == null) throw new ArgumentNullException(nameof(token));

            await DeleteAsync(param, Options.Controller, action, token);
        }

        /// <summary>
        /// Calls the Web Api to delete the object of type T
        /// </summary>
        /// <param name="param">Can be a simple type like int, string, etc or an anonymous type defining many properties</param>
        /// <param name="controller">The controller that will be called</param>
        /// <param name="action">Explicit define the action that will be called</param>
        /// <param name="token">The <see cref="System.Threading.CancellationToken"/> to be used to cancel the call</param>
        /// <remarks>
        /// Requests are sent using HttpVerb DELETE
        /// </remarks>
        /// <exception cref="RestService.WebApiClientException" />
        /// <exception cref="System.ArgumentNullException" />
        protected async virtual Task DeleteAsync(object param, string controller, string action, CancellationToken token)
        {
            try
            {
#if DEBUG
                //When debugging timeouts, sometimes the operation completes so fast that is impossible to recreate the scenario
                await Task.Delay(50);
#endif

                //Authenticates if needed
                Authenticate();

                //Makes the async call 
                var response = await Client.DeleteAsync(GenerateUri(controller, action, param), token);

                if (!response.IsSuccessStatusCode)
                {
                    //Packs the error response into a WebApiClientException
                    throw GetException(response);
                }
            }
            catch (OperationCanceledException)
            {
                //Timeout or operation canceled by user
                throw new WebApiClientException(HttpStatusCode.RequestTimeout, "Task canceled due to timeout or token cancellation");
            }
            catch (WebApiClientException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                //Generic exception
                throw new WebApiClientException(HttpStatusCode.InternalServerError, e);
            }
        }



        /// <summary>
		/// Send an object to the Web Api to be edited
		/// </summary>
		/// <param name="obj">The object to be updated</param>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> that will yield the edited object of type T</returns>
        /// <remarks>
		/// Requests are sent using HttpVerb PUT
		/// </remarks>
		/// <exception cref="RestService.WebApiClientException" />
		/// <exception cref="System.ArgumentNullException" />
        public async virtual Task<T> EditAsync(T obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            CancellationTokenSource src = new CancellationTokenSource((int)Options.Timeout);

            return await EditAsync(obj, Options.Controller, null, src.Token);
        }

        /// <summary>
        /// Send an object to the Web Api to be edited
        /// </summary>
        /// <param name="obj">The object to be updated</param>
        /// <param name="token">The <see cref="System.Threading.CancellationToken"/> to be used to cancel the call</param>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> that will yield the edited object of type T</returns>
        /// <remarks>
        /// Requests are sent using HttpVerb PUT
        /// </remarks>
        /// <exception cref="RestService.WebApiClientException" />
        /// <exception cref="System.ArgumentNullException" />
        public async virtual Task<T> EditAsync(T obj, CancellationToken token)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (token == null) throw new ArgumentNullException(nameof(token));

            return await EditAsync(obj, Options.Controller, null, token);
        }

        /// <summary>
        /// Send an object to the Web Api to be edited
        /// </summary>
        /// <param name="obj">The object to be updated</param>
        /// <param name="action">Explicit define the action that will be called</param>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> that will yield the edited object of type T</returns>
        /// <remarks>
        /// Requests are sent using HttpVerb PUT
        /// </remarks>
        /// <exception cref="RestService.WebApiClientException" />
        /// <exception cref="System.ArgumentNullException" />
        public async virtual Task<T> EditAsync(T obj, string action)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (string.IsNullOrEmpty(action)) throw new ArgumentNullException(nameof(action));

            CancellationTokenSource src = new CancellationTokenSource((int)Options.Timeout);

            return await EditAsync(obj, Options.Controller, action, src.Token);
        }

        /// <summary>
        /// Send an object to the Web Api to be edited
        /// </summary>
        /// <param name="obj">The object to be updated</param>
        /// <param name="action">Explicit define the action that will be called</param>
        /// <param name="token">The <see cref="System.Threading.CancellationToken"/> to be used to cancel the call</param>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> that will yield the edited object of type T</returns>
        /// <remarks>
        /// Requests are sent using HttpVerb PUT
        /// </remarks>
        /// <exception cref="RestService.WebApiClientException" />
        /// <exception cref="System.ArgumentNullException" />
        public async virtual Task<T> EditAsync(T obj, string action, CancellationToken token)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (string.IsNullOrEmpty(action)) throw new ArgumentNullException(nameof(action));
            if (token == null) throw new ArgumentNullException(nameof(token));

            return await EditAsync(obj, Options.Controller, action, token);
        }

        /// <summary>
        /// Send an object to the Web Api to be edited
        /// </summary>
        /// <param name="obj">The object to be updated</param>
        /// <param name="controller">The controller that will be called</param>
        /// <param name="action">Explicit define the action that will be called</param>
        /// <param name="token">The <see cref="System.Threading.CancellationToken"/> to be used to cancel the call</param>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> that will yield the edited object of type T</returns>
        /// <remarks>
        /// Requests are sent using HttpVerb PUT
        /// </remarks>
        /// <exception cref="RestService.WebApiClientException" />
        /// <exception cref="System.ArgumentNullException" />
        protected async virtual Task<T> EditAsync(T obj, string controller, string action, CancellationToken token)
        {
            try
            {
#if DEBUG
                //When debugging timeouts, sometimes the operation completes so fast that is impossible to recreate the scenario
                await Task.Delay(50);
#endif

                //Authenticates if needed
                Authenticate();

                //Loads the object into the content's buffer. If we call PutAsync passing obj instead of content, an error occurs
                var content = new ObjectContent<T>(obj, Options.Formatter);
                await content.LoadIntoBufferAsync();

                //Makes the async call 
                var response = await Client.PutAsync(GenerateUri(controller, action, null), content, token);

                if (!response.IsSuccessStatusCode)
                {
                    //Packs the error response into a WebApiClientException
                    throw GetException(response);
                }

                try
                {
                    //Returns the object
                    return await response.Content.ReadAsAsync<T>(token);
                }
                catch
                {
                    return default(T);
                }
            }
            catch (OperationCanceledException)
            {
                //Timeout or operation canceled by user
                throw new WebApiClientException(HttpStatusCode.RequestTimeout, "Task canceled due to timeout or token cancellation");
            }
            catch (WebApiClientException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                //Generic exception
                throw new WebApiClientException(HttpStatusCode.InternalServerError, e);
            }
        }


        #region GetMany

        /// <summary>
		/// Returns a <see cref="System.Threading.Tasks.Task"/> that will yield a list of objects of the specified type T from the Web Api
		/// </summary>
		/// <remarks>
		/// Requests are sent using HttpVerb GET
		/// </remarks>
		/// <exception cref="RestService.WebApiClientException" />
		/// <exception cref="System.ArgumentNullException" />
		public async virtual Task<IEnumerable<T>> GetManyAsync()
        {
            CancellationTokenSource src = new CancellationTokenSource((int)Options.Timeout);

            return await GetManyAsync(null, Options.Controller, null, src.Token);
        }

        /// <summary>
        /// Returns a <see cref="System.Threading.Tasks.Task"/> that will yield a list of objects of the specified type T from the Web Api
        /// </summary>
        /// <param name="token">The <see cref="System.Threading.CancellationToken"/> to be used to cancel the call</param>
        /// <remarks>
        /// Requests are sent using HttpVerb GET
        /// </remarks>
        /// <exception cref="RestService.WebApiClientException" />
        /// <exception cref="System.ArgumentNullException" />
        public async virtual Task<IEnumerable<T>> GetManyAsync(CancellationToken token)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));

            return await GetManyAsync(null, Options.Controller, string.Empty, token);
        }

        /// <summary>
        /// Returns a <see cref="System.Threading.Tasks.Task"/> that will yield a list of objects of the specified type T from the Web Api
        /// </summary>
        /// <param name="param">Can be a simple type like int, string, etc or an anonymous type defining many properties</param>
        /// <remarks>
        /// Requests are sent using HttpVerb GET
        /// </remarks>
        /// <exception cref="RestService.WebApiClientException" />
        /// <exception cref="System.ArgumentNullException" />
        public async virtual Task<IEnumerable<T>> GetManyAsync(object param)
        {
            if (param == null) throw new ArgumentNullException(nameof(param));

            CancellationTokenSource src = new CancellationTokenSource((int)Options.Timeout);

            return await GetManyAsync(param, Options.Controller, string.Empty, src.Token);
        }

        /// <summary>
        /// Returns a <see cref="System.Threading.Tasks.Task"/> that will yield a list of objects of the specified type T from the Web Api
        /// </summary>
        /// <param name="param">Can be a simple type like int, string, etc or an anonymous type defining many properties</param>
        /// <param name="token">The <see cref="System.Threading.CancellationToken"/> to be used to cancel the call</param>
        /// <remarks>
        /// Requests are sent using HttpVerb GET
        /// </remarks>
        /// <exception cref="RestService.WebApiClientException" />
        /// <exception cref="System.ArgumentNullException" />
        public async virtual Task<IEnumerable<T>> GetManyAsync(object param, CancellationToken token)
        {
            if (param == null) throw new ArgumentNullException(nameof(param));
            if (token == null) throw new ArgumentNullException(nameof(token));

            return await GetManyAsync(param, Options.Controller, string.Empty, token);
        }

        /// <summary>
        /// Returns a <see cref="System.Threading.Tasks.Task"/> that will yield a list of objects of the specified type T from the Web Api
        /// </summary>
        /// <param name="action">Explicit define the action that will be called</param>
        /// <remarks>
        /// Requests are sent using HttpVerb GET
        /// </remarks>
        /// <exception cref="RestService.WebApiClientException" />
        /// <exception cref="System.ArgumentNullException" />
        public async virtual Task<IEnumerable<T>> GetManyAsync(string action)
        {
            if (string.IsNullOrEmpty(action)) throw new ArgumentNullException(nameof(action));

            CancellationTokenSource src = new CancellationTokenSource((int)Options.Timeout);

            return await GetManyAsync(null, Options.Controller, action, src.Token);
        }

        /// <summary>
        /// Returns a <see cref="System.Threading.Tasks.Task"/> that will yield a list of objects of the specified type T from the Web Api
        /// </summary>
        /// <param name="action">Explicit define the action that will be called</param>
        /// <param name="token">The <see cref="System.Threading.CancellationToken"/> to be used to cancel the call</param>
        /// <remarks>
        /// Requests are sent using HttpVerb GET
        /// </remarks>
        /// <exception cref="RestService.WebApiClientException" />
        /// <exception cref="System.ArgumentNullException" />
        public async virtual Task<IEnumerable<T>> GetManyAsync(string action, CancellationToken token)
        {
            if (string.IsNullOrEmpty(action)) throw new ArgumentNullException(nameof(action));
            if (token == null) throw new ArgumentNullException(nameof(token));

            return await GetManyAsync(null, Options.Controller, action, token);
        }

        /// <summary>
        /// Returns a <see cref="System.Threading.Tasks.Task"/> that will yield a list of objects of the specified type T from the Web Api
        /// </summary>
        /// <param name="param">Can be a simple type like int, string, etc or an anonymous type defining many properties</param>
        /// <param name="action">Explicit define the action that will be called</param>
        /// <remarks>
        /// Requests are sent using HttpVerb GET
        /// </remarks>
        /// <exception cref="RestService.WebApiClientException" />
        /// <exception cref="System.ArgumentNullException" />
        public async virtual Task<IEnumerable<T>> GetManyAsync(object param, string action)
        {
            if (param == null) throw new ArgumentNullException(nameof(param));
            if (string.IsNullOrEmpty(action)) throw new ArgumentNullException(nameof(action));

            CancellationTokenSource src = new CancellationTokenSource((int)Options.Timeout);

            return await GetManyAsync(param, Options.Controller, action, src.Token);
        }

        /// <summary>
        /// Returns a <see cref="System.Threading.Tasks.Task"/> that will yield a list of objects of the specified type T from the Web Api
        /// </summary>
        /// <param name="param">Can be a simple type like int, string, etc or an anonymous type defining many properties</param>
        /// <param name="action">Explicit define the action that will be called</param>
        /// <param name="token">The <see cref="System.Threading.CancellationToken"/> to be used to cancel the call</param>
        /// <remarks>
        /// Requests are sent using HttpVerb GET
        /// </remarks>
        /// <exception cref="RestService.WebApiClientException" />
        /// <exception cref="System.ArgumentNullException" />
        public async virtual Task<IEnumerable<T>> GetManyAsync(object param, string action, CancellationToken token)
        {
            if (param == null) throw new ArgumentNullException(nameof(param));
            if (string.IsNullOrEmpty(action)) throw new ArgumentNullException(nameof(action));
            if (token == null) throw new ArgumentNullException(nameof(token));

            return await GetManyAsync(param, Options.Controller, action, token);
        }

        /// <summary>
        /// Returns a <see cref="System.Threading.Tasks.Task"/> that will yield a list of objects of the specified type T from the Web Api
        /// </summary>
        /// <param name="param">Can be a simple type like int, string, etc or an anonymous type defining many properties</param>
        /// <param name="controller">The controller that will be called</param>
        /// <param name="action">Explicit define the action that will be called</param>
        /// <param name="token">The <see cref="System.Threading.CancellationToken"/> to be used to cancel the call</param>
        /// <remarks>
        /// Requests are sent using HttpVerb GET
        /// </remarks>
        /// <exception cref="RestService.WebApiClientException" />
        /// <exception cref="System.ArgumentNullException" />
        protected async virtual Task<IEnumerable<T>> GetManyAsync(object param, string controller, string action, CancellationToken token)
        {
            try
            {
#if DEBUG
                //When debugging timeouts, sometimes the operation completes so fast that is impossible to recreate the scenario
                await Task.Delay(50);
#endif

                //Authenticates if needed
                Authenticate();

                //Makes the async call 
                var response = await Client.GetAsync(GenerateUri(controller, action, param), token);

                if (!response.IsSuccessStatusCode)
                {
                    //Packs the error response into a WebApiClientException
                    throw GetException(response);
                }

                //All done, so returns the requested list of objects
                return await response.Content.ReadAsAsync<List<T>>(token);
            }
            catch (OperationCanceledException)
            {
                //Timeout or operation canceled by user
                throw new WebApiClientException(HttpStatusCode.RequestTimeout, "Task canceled due to timeout or token cancellation");
            }
            catch (WebApiClientException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                //Generic exception
                throw new WebApiClientException(HttpStatusCode.InternalServerError, e);
            }
        }
        #endregion

        #region GetOne

        /// <summary>
        /// Returns a <see cref="System.Threading.Tasks.Task"/> that will yield an object of the specified type T from the Web Api
        /// </summary>
        /// <remarks>
        /// Requests are sent using HttpVerb GET
        /// </remarks>
        /// <exception cref="RestService.WebApiClientException" />
        public async virtual Task<T> GetOneAsync()
        {
            CancellationTokenSource src = new CancellationTokenSource((int)Options.Timeout);

            return await GetOneAsync(null, Options.Controller, string.Empty, src.Token);
        }

        /// <summary>
        /// Returns a <see cref="System.Threading.Tasks.Task"/> that will yield an object of the specified type T from the Web Api
        /// </summary>
        /// <param name="token">The <see cref="System.Threading.CancellationToken"/> to be used to cancel the call</param>
        /// <remarks>
        /// Requests are sent using HttpVerb GET
        /// </remarks>
        /// <exception cref="RestService.WebApiClientException" />
        /// <exception cref="System.ArgumentNullException" />
        public async virtual Task<T> GetOneAsync(CancellationToken token)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));

            return await GetOneAsync(null, Options.Controller, string.Empty, token);
        }

        /// <summary>
		/// Returns a <see cref="System.Threading.Tasks.Task"/> that will yield an object of the specified type T from the Web Api
		/// </summary>
		/// <param name="param">Can be a simple type like int, string, etc or an anonymous type defining many properties</param>
		/// <remarks>
		/// Requests are sent using HttpVerb GET
		/// </remarks>
		/// <exception cref="RestService.WebApiClientException" />
		/// <exception cref="System.ArgumentNullException" />
		public async virtual Task<T> GetOneAsync(object param)
        {
            CancellationTokenSource src = new CancellationTokenSource((int)Options.Timeout);

            return await GetOneAsync(param, Options.Controller, string.Empty, src.Token);
        }

        /// <summary>
        /// Returns a <see cref="System.Threading.Tasks.Task"/> that will yield an object of the specified type T from the Web Api
        /// </summary>
        /// <param name="param">Can be a simple type like int, string, etc or an anonymous type defining many properties</param>
        /// <param name="token">The <see cref="System.Threading.CancellationToken"/> to be used to cancel the call</param>
        /// <remarks>
        /// Requests are sent using HttpVerb GET
        /// </remarks>
        /// <exception cref="RestService.WebApiClientException" />
        /// <exception cref="System.ArgumentNullException" />
        public async virtual Task<T> GetOneAsync(object param, CancellationToken token)
        {
            if (param == null) throw new ArgumentNullException(nameof(param));
            if (token == null) throw new ArgumentNullException(nameof(token));

            return await GetOneAsync(param, Options.Controller, string.Empty, token);
        }

        /// <summary>
        /// Returns a <see cref="System.Threading.Tasks.Task"/> that will yield an object of the specified type T from the Web Api
        /// </summary>
        /// <param name="param">Can be a simple type like int, string, etc or an anonymous type defining many properties</param>
        /// <param name="action">Explicit define the action that will be called</param>
        /// <remarks>
        /// Requests are sent using HttpVerb GET
        /// </remarks>
        /// <exception cref="RestService.WebApiClientException" />
        /// <exception cref="System.ArgumentNullException" />
        public async virtual Task<T> GetOneAsync(object param, string action)
        {
            if (param == null) throw new ArgumentNullException(nameof(param));
            if (string.IsNullOrEmpty(action)) throw new ArgumentNullException(nameof(action));

            CancellationTokenSource src = new CancellationTokenSource((int)Options.Timeout);

            return await GetOneAsync(param, Options.Controller, action, src.Token);
        }

        /// <summary>
        /// Returns a <see cref="System.Threading.Tasks.Task"/> that will yield an object of the specified type T from the Web Api
        /// </summary>
        /// <param name="param">Can be a simple type like int, string, etc or an anonymous type defining many properties</param>
        /// <param name="action">Explicit define the action that will be called</param>
        /// <param name="token">The <see cref="System.Threading.CancellationToken"/> to be used to cancel the call</param>
        /// <remarks>
        /// Requests are sent using HttpVerb GET
        /// </remarks>
        /// <exception cref="RestService.WebApiClientException" />
        /// <exception cref="System.ArgumentNullException" />
        public async virtual Task<T> GetOneAsync(object param, string action, CancellationToken token)
        {
            if (param == null) throw new ArgumentNullException(nameof(param));
            if (string.IsNullOrEmpty(action)) throw new ArgumentNullException(nameof(action));
            if (token == null) throw new ArgumentNullException(nameof(token));

            return await GetOneAsync(param, Options.Controller, action, token);
        }

        /// <summary>
        /// Returns a <see cref="System.Threading.Tasks.Task"/> that will yield an object of the specified type T from the Web Api
        /// </summary>
        /// <param name="param">Can be a simple type like int, string, etc or an anonymous type defining many properties</param>
        /// <param name="controller">The controller that will be called</param>
        /// <param name="action">Explicit define the action that will be called</param>
        /// <param name="token">The <see cref="System.Threading.CancellationToken"/> to be used to cancel the call</param>
        /// <remarks>
        /// Requests are sent using HttpVerb GET
        /// </remarks>
        /// <exception cref="RestService.WebApiClientException" />
        /// <exception cref="System.ArgumentNullException" />
        protected async virtual Task<T> GetOneAsync(object param, string controller, string action, CancellationToken token)
        {
            try
            {
#if DEBUG
                //When debugging timeouts, sometimes the operation completes so fast that is impossible to recreate the scenario
                await Task.Delay(50);
#endif

                //Authenticates if needed
                Authenticate();

                //Makes the async call 
                var response = await Client.GetAsync(GenerateUri(controller, action, param), token);

                if (!response.IsSuccessStatusCode)
                {
                    //Packs the error response into a WebApiClientException
                    throw GetException(response);
                }

                //All done, so returns the requested object
                return await response.Content.ReadAsAsync<T>(token);
            }
            catch (OperationCanceledException)
            {
                //Timeout or operation canceled by user
                throw new WebApiClientException(HttpStatusCode.RequestTimeout, "Task canceled due to timeout or token cancellation");
            }
            catch (WebApiClientException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                //Generic exception
                throw new WebApiClientException(HttpStatusCode.InternalServerError, e);
            }
        }
        #endregion
    }
}