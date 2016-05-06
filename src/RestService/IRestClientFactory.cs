namespace RestService.Rest
{
    /// <summary>
    /// Interface for factory that can create rest clients
    /// </summary>
    public interface IRestClientFactory
    {
        /// <summary>
        /// Creates a new rest client instance
        /// </summary>
        /// <typeparam name="TEntry">type of the resouce</typeparam>
        /// <returns>a new <see cref="IRestClient{TEntry}"/></returns>
        IRestClient<TEntry> New<TEntry>() where TEntry : class;

        IRestClient<TEntry> New<TEntry>(WebApiClientOptions options) where TEntry : class;
    }
}
