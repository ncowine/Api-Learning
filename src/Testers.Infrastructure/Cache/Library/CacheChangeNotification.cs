// Vendored from https://github.com/ncowine/CacheRepository (master) on 2026-05-17.
// Do not edit in place; update by re-fetching upstream. See NOTICE.md.
#nullable disable

namespace CacheRepository;

public enum ChangeType { Updated, Deleted }

public readonly record struct CacheChangeNotification<TKey>(
    IReadOnlyCollection<TKey> Keys,
    ChangeType ChangeType
);
