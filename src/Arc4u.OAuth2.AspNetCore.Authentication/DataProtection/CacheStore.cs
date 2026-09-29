using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using Arc4u.Caching;
using Arc4u.Diagnostics;
using Arc4u.Serializer;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Logging;

namespace Arc4u.OAuth2.DataProtection
{
    /// <summary>An <see cref="IXmlRepository"/> that persists the ASP.NET Core data protection keys in an Arc4u cache, so they are shared by all the instances of an application.</summary>
    public class CacheStore : IXmlRepository
    {
        /// <summary>Initializes a new instance of the <see cref="CacheStore"/> class.</summary>
        /// <param name="cacheContext">The context giving access to the registered caches.</param>
        /// <param name="loggerFactory">The factory of loggers.</param>
        /// <param name="serialization">The serializer of the list of keys.</param>
        /// <param name="cacheKey">The key under which the list of keys is stored.</param>
        /// <param name="cacheName">The name of the cache. When empty, the default cache is used.</param>
        /// <exception cref="ArgumentNullException">An argument other than <paramref name="cacheName"/> is <see langword="null"/>.</exception>
        public CacheStore(ICacheContext cacheContext, ILoggerFactory loggerFactory, IObjectSerialization serialization, [DisallowNull] string cacheKey, string? cacheName = null)
        {
            ArgumentNullException.ThrowIfNull(serialization);
            ArgumentNullException.ThrowIfNull(cacheContext);
            ArgumentNullException.ThrowIfNull(loggerFactory);
            ArgumentNullException.ThrowIfNull(cacheKey);

            _logger = loggerFactory.CreateLogger<CacheStore>();
            _cacheContext = cacheContext;
            _cacheName = cacheName;
            _cacheKey = cacheKey;
            _serialization = serialization;

            _cache = new Lazy<ICache>(() =>
            {
                // Check if I give a wrong cache name => Exception with clear context!
                return string.IsNullOrWhiteSpace(_cacheName) ? cacheContext.Default : cacheContext[_cacheName];
            });
        }

        private readonly ICacheContext _cacheContext;
        private readonly ILogger<CacheStore> _logger;
        private readonly string _cacheKey;
        private readonly string? _cacheName;
        private readonly Lazy<ICache> _cache;
        private readonly IObjectSerialization _serialization;

        /// <inheritdoc/>
        public IReadOnlyCollection<XElement> GetAllElements()
        {
            return GetElements().AsReadOnly();
        }

        private List<XElement> GetElements()
        {
            var result = new List<XElement>();

            var content = _cache.Value.Get<byte[]>(_cacheKey);

            if (content is not null)
            {
                var list = _serialization.Deserialize<List<string>>(content);

                if (list is null)
                {
                    _logger.Technical().LogError("The deserialization of the Data Protection xml element failed.");
                    return result;
                }

                result.AddRange(list.Where(e => !string.IsNullOrWhiteSpace(e)).Select(XElement.Parse));
            }

            return result;
        }

        /// <inheritdoc/>
        public void StoreElement(XElement element, string friendlyName)
        {
            var result = GetElements();

            result.Insert(0, element);

            var content = _serialization.Serialize<List<string>>(result.Select(e => e.ToString(SaveOptions.DisableFormatting)).ToList());

            _cache.Value.Put(_cacheKey, content);
        }
    }
}
