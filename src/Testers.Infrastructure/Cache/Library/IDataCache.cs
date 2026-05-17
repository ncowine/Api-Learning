// Vendored from https://github.com/ncowine/CacheRepository (master) on 2026-05-17.
// Do not edit in place; update by re-fetching upstream. See NOTICE.md.
#nullable disable

namespace CacheRepository;

public interface IDataCache<TKey, TValue>
{
    Task<TValue> Get(TKey key);
    Task<IReadOnlyDictionary<TKey, TValue>> Get(HashSet<TKey> keys);
    Task<IReadOnlyDictionary<TKey, TValue>> GetAll();
}
