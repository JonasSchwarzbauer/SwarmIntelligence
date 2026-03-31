using Webserver.Interfaces;

namespace Webserver.DataCache
{
    public sealed class SingletonCacheAdapter<T>(ICacheTypeHandler<T> handler, string key) : ISingletonCache<T> where T : class
    {
        private readonly ICacheTypeHandler<T> _handler = handler;
        private readonly string _key = key;

        public T? Get() => _handler.Get(_key);

        public void Update(T value) => _handler.Update(_key, value);

        public void Remove() => _handler.Remove(_key);
    }
}
