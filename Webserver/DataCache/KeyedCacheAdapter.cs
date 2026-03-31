using System.Collections.Generic;
using Webserver.Interfaces;

namespace Webserver.DataCache
{
    public sealed class KeyedCacheAdapter<T>(ICacheTypeHandler<T> handler) : IKeyedCache<T> where T : class
    {
        private readonly ICacheTypeHandler<T> _handler = handler;

        public T? Get(string key) => _handler.Get(key);

        public IEnumerable<T> GetAll() => _handler.GetAll();

        public void Update(string key, T value) => _handler.Update(key, value);

        public void Remove(string key) => _handler.Remove(key);
    }
}
