# CacheRepository (vendored)

These files in `Cache/Library/` are vendored from the user's own library:

- **Source**: <https://github.com/ncowine/CacheRepository>
- **Branch**: `master`
- **Vendored**: 2026-05-17
- **License**: None specified on the source repo (user's own code; used within their project).

## What it is

An in-memory, typed, read-through cache base class with concurrent-fetch coalescing
(multiple parallel `Get(key)` for the same missing key trigger exactly one fetch).
Each cache use case extends `DataCache<TKey, TValue>` and implements two abstract
`FetchAsync` methods (single + batch).

This is **distinct from** the generic `ICache` abstraction in
`Testers.Application.Abstractions` (which is backed by Redis via `RedisCache`):

| | Generic `ICache` (Redis)           | `DataCache<TKey, TValue>` (this folder)  |
|---|---|---|
| Surface | `Get`/`Set`/`Remove`/`GetOrAdd<T>` | `Get(key)` / `Get(keys)` / `GetAll`      |
| Backing | Redis (distributed)                | In-memory `ConcurrentDictionary`         |
| Pattern | Ad-hoc, string-keyed                | Typed per cache, one subclass per entity |
| Wins | Cross-instance, write-through        | Fetch coalescing, fully typed, no JSON   |

Use `ICache` for ad-hoc cross-instance values. Use `DataCache<,>` for hot per-entity
read paths where coalescing pays off (e.g. TaskDefinition lookups by id under load).

## How to update

Re-fetch each `.cs` file from
<https://github.com/ncowine/CacheRepository/tree/master/CacheRepository>, keeping the
`#nullable disable` line at the top of each vendored file. If the library ever gets
published as a NuGet package, replace this folder with a `<PackageReference>`.
