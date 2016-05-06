﻿using System.Collections.Generic;

namespace RestService
{
    /// <summary>
    /// Represents details of a exception
    /// </summary>
    public class WebApiClientExceptionDetails
    {
        /// <summary>
        /// The exception message
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// The reason why the http request didn't complete with success
        /// </summary>
        public string Reason { get; set; }
        
        /// <summary>
        /// The exception stack trace
        /// </summary>
        public string StackTrace { get; set; }
        
        /// <summary>
        /// The ModelState containing the model validation errors
        /// </summary>
        public IDictionary<string, IList<string>> ModelState { get; set; }
        
        /// <summary>
        /// The type of the exception
        /// </summary>
        public string ExceptionType { get; set; }


        public WebApiClientExceptionDetails()
        {
            ModelState = new Dictionary<string, IList<string>>();
        }
    }

}
