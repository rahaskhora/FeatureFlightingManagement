using System;
using System.Threading.Tasks;

namespace Microsoft.FeatureFlighting.Common.Cache
{   
    /// <summary>
    /// Service that can cache/re-cache data in the background
    /// </summary>
    public interface IBackgroundCacheableService
    {
        /// <summary>
        /// ID of the service
        /// </summary>
        string CacheableServiceId { get; }
        
        /// <summary>
        /// Event when an object in cached
        /// </summary>
        event EventHandler<CacheParameters> ObjectCached;
        
        /// <summary>
        /// Recaches an already cached object with fresh data
        /// </summary>
        /// <param name="cacheParameters" cref="CacheParameters">Cache Parameters</param>
        /// <param name="trackingIds">Tracking ID</param>
        /// <returns></returns>
        Task Recache(CacheParameters cacheParameters, LoggerTrackingIds trackingIds);
    }

    /// <summary>
    /// Service that can cache cache data in the background
    /// </summary>
    /// <typeparam name="TCacheObject"></typeparam>
    public interface IBackgroundCacheableService<TCacheObject> : ICacheableService<TCacheObject>, IBackgroundCacheableService
    { }
}
