using System.Collections.Concurrent;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Webserver.Interfaces;

namespace Webserver.DataCache
{
    /// <summary>
    /// Concrete per-type cache handler that stores items in a thread-safe
    /// <see cref="ConcurrentDictionary{TKey,TValue}"/> and publishes changes
    /// through an Rx <see cref="ISubject{T}"/> so consumers can observe adds,
    /// updates and removals.
    /// </summary>
    /// <typeparam name="T">The cached item type.</typeparam>
    public class CacheTypeHandler<T> : ICacheTypeHandler<T> where T : class
    {
        private readonly ConcurrentDictionary<string, T> _store = new();

        /// <summary>
        /// Add or update the value for <paramref name="key"/> and publish a
        /// corresponding update event. If the key was not previously present an
        /// </summary>
        public void Update(string key, T value)
        {
            _store[key] = value;
        }

        /// <summary>
        /// Try to return the value stored at <paramref name="key"/> or null when
        /// the key is not present.
        /// </summary>
        public T? Get(string key)
            => _store.TryGetValue(key, out var value) ? value : null;

        /// <summary>
        /// Get an enumeration over all cached values for this type. The collection
        /// returned is a live view of the dictionary values and may change if the
        /// cache is modified concurrently.
        /// </summary>
        public IEnumerable<T> GetAll()
            => _store.Values;

        /// <summary>
        /// Remove the value for <paramref name="key"/> and publish a removal
        /// notification if the item was present.
        /// </summary>
        public void Remove(string key)
        {
            _store.TryRemove(key, out var value);
        }
    }
}
