using System;
using System.Collections.Generic;

namespace Microsoft.FeatureFlighting.Common.Cache
{
    /// <summary>
    /// Parameters required for chacing
    /// </summary>
    public class CacheParameters
    {
        /// <summary>
        /// Cache Key (uniquely identifies a cached object)
        /// </summary>
        public string CacheKey { get; set; }
        
        /// <summary>
        /// ID of the object being cached (may not be same as cache key)
        /// </summary>
        public string ObjectId { get; set; }
        
        /// <summary>
        /// Tenant ID
        /// </summary>
        public string Tenant { get; set; }
        
        /// <summary>
        /// Duration to keep the object in cache (in mins)
        /// </summary>
        public int CacheDuration { get; set; }
        
        /// <summary>
        /// Additional parameters needed for caschcing
        /// </summary>
        public Dictionary<string, string> AdditionalParmeters { get; set; } = null;
        
        /// <summary>
        /// Timestamp when the cache needs to be updated
        /// </summary>
        public DateTime NextRecacheTimestamp { get; private set; }

        /// <summary>
        /// Updates the timestamp for next re-caching
        /// </summary>
        public void UpdateNextRecacheTimestamp()
        {
            if (CacheDuration > 0)
                NextRecacheTimestamp = DateTime.UtcNow.AddMinutes(CacheDuration);
            else
                NextRecacheTimestamp = DateTime.MaxValue;
        }

        /// <summary>
        /// Indicates if the object should be conisidered for re-caching
        /// </summary>
        /// <param name="gracePeriod">Flexible periopd (in mins)</param>
        /// <returns></returns>
        public bool ShouldRecache(int gracePeriod)
        {
            DateTime graceTimestamp = DateTime.UtcNow.AddMinutes(gracePeriod);
            return NextRecacheTimestamp <= graceTimestamp;
        }
    }
}
