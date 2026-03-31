using Webserver.DataCache;

namespace Webserver.Interfaces
{
    /// <summary>
    /// Internal per-type handler used by the <see cref="Webserver.DataCache.SwarmDataCache"/>
    /// to provide storage and update observation for a specific cached type <typeparamref name="T"/>.
    /// </summary>
    public interface ICacheTypeHandler<T> where T : class
    {
        /// <summary>
        /// Add or update the item identified by <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Unique key for the item.</param>
        /// <param name="value">Item to store.</param>
        public void Update(string key, T value);

        /// <summary>
        /// Retrieve the item with the specified <paramref name="key"/>.
        /// Returns <c>null</c> when missing.
        /// </summary>
        public T? Get(string key);

        /// <summary>
        /// Enumerate all items stored for this type.
        /// </summary>
        public IEnumerable<T> GetAll();

        /// <summary>
        /// Remove the item with the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">Key identifying the item to remove.</param>
        public void Remove(string key);
    }
}
