using System;
using System.Collections.Generic;

namespace RestService.Rest
{
    public class RestClientFactory : IRestClientFactory
    {
        
        private readonly IDictionary<Type, object> _cache;

        public RestClientFactory()
        {
            
            _cache = new Dictionary<Type, object>();
        }


        public IRestClient<TEntry> New<TEntry>() where TEntry : class => New<TEntry>(new WebApiClientOptions());
        

        public IRestClient<TEntry> New<TEntry>(WebApiClientOptions options) where TEntry : class
        {
            IRestClient<TEntry> client;

            if (_cache.ContainsKey(typeof(TEntry)))
            {
                client = _cache[typeof(TEntry)] as IRestClient<TEntry>;
            }
            else
            {
                client = new WebApiClient<TEntry>(options);
                _cache.Add(typeof(TEntry), client);
            }

            return client;
        }
    }
}
