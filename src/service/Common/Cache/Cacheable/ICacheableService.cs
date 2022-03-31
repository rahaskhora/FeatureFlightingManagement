using System.Threading.Tasks;

namespace Microsoft.FeatureFlighting.Common.Cache
{
    /// <summary>
    /// Service that can cache objects
    /// </summary>
    /// <typeparam name="TCacheObject">Type of the object that can be cached</typeparam>
    public interface ICacheableService<TCacheObject>
    {
        /// <summary>
        /// Gets a cached object
        /// </summary>
        /// <param name="parameters" cref="CacheParameters">Cache parameters</param>
        /// <param name="trackingIds">Tracking ID</param>
        /// <returns>Cached object</returns>
        Task<TCacheObject> GetCachedObject(CacheParameters parameters, LoggerTrackingIds trackingIds);
        
        /// <summary>
        /// Sets an object in the cache
        /// </summary>
        /// <param name="cacheableObject" cref="CacheableObject{TCacheObject}">Object to be cached</param>
        /// <param name="trackingIds">Tracking ID</param>
        Task SetCacheObject(CacheableObject<TCacheObject> cacheableObject, LoggerTrackingIds trackingIds);
        
        /// <summary>
        /// Creates an object (that will be cached later)
        /// </summary>
        /// <param name="cacheParameters" cref="CacheParameters">Cache parameters</param>
        /// <param name="setCache">Flag to indicate if the cache shoould be set (False would imply, that the object will be created and not cached)</param>
        /// <param name="trackingIds">Tracking ID</param>
        /// <returns cref="CacheableObject{TCacheObject}">Object</returns>
        Task<CacheableObject<TCacheObject>> CreateCacheableObject(CacheParameters cacheParameters, bool setCache, LoggerTrackingIds trackingIds);
    }
}
