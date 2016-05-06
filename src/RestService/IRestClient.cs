using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace RestService
{
    public interface IRestClient<T> : IDisposable where T : class
    {
        HttpClientHandler Handler { get; }
        HttpRequestHeaders Headers { get; }
        WebApiClientOptions Options { get; }

        void AddCookie(CookieCollection cookies);
        void AddCookie(Cookie cookie);
        void AddCookie(string name, string value, string path, string domain);
        Task<T> CreateAsync(T obj);
        Task<T> CreateAsync(T obj, string action);
        Task<T> CreateAsync(T obj, CancellationToken token);
        Task<T> CreateAsync(T obj, string action, CancellationToken token);
        Task DeleteAsync(object param);
        Task DeleteAsync(object param, string action);
        Task DeleteAsync(object param, CancellationToken token);
        Task DeleteAsync(object param, string action, CancellationToken token);
        
        
        Task<T> EditAsync(T obj);
        Task<T> EditAsync(T obj, string action);
        Task<T> EditAsync(T obj, CancellationToken token);
        Task<T> EditAsync(T obj, string action, CancellationToken token);
        Task<IEnumerable<T>> GetManyAsync();
        Task<IEnumerable<T>> GetManyAsync(string action);
        Task<IEnumerable<T>> GetManyAsync(object param);
        Task<IEnumerable<T>> GetManyAsync(CancellationToken token);
        Task<IEnumerable<T>> GetManyAsync(string action, CancellationToken token);
        Task<IEnumerable<T>> GetManyAsync(object param, string action);
        Task<IEnumerable<T>> GetManyAsync(object param, CancellationToken token);
        Task<IEnumerable<T>> GetManyAsync(object param, string action, CancellationToken token);
        Task<T> GetOneAsync();
        Task<T> GetOneAsync(object param);
        Task<T> GetOneAsync(CancellationToken token);
        Task<T> GetOneAsync(object param, string action);
        Task<T> GetOneAsync(object param, CancellationToken token);
        Task<T> GetOneAsync(object param, string action, CancellationToken token);
    }
}