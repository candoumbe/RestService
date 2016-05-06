namespace RestService.Rest
{
    /// <summary>
    /// Options to pass alongside the request
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TEntry"></typeparam>
    public class RestClientOptions
    {
        public string Action { get; set; }

        /// <summary>
        /// Timeout in miliseconds
        /// </summary>
        public uint Timeout { get; set; } = 30000;


        public string BaseAdress { get; set; }


    }
}
