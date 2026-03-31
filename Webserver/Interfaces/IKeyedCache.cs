using System.Collections.Generic;

namespace Webserver.Interfaces
{
    public interface IKeyedCache<T> where T : class
    {
        T? Get(string key);
        IEnumerable<T> GetAll();
        void Update(string key, T value);
        void Remove(string key);
    }
}
