namespace Webserver.Interfaces
{
    public interface ISingletonCache<T> where T : class
    {
        T? Get();
        void Update(T value);
        void Remove();
    }
}
