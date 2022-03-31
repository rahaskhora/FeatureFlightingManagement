namespace Microsoft.FeatureFlighting.Common.Cache
{
    /// <summary>
    /// Represents an object that can be cached
    /// </summary>
    /// <typeparam name="TCacheObject">Type of the object being cached</typeparam>
    public class CacheableObject<TCacheObject>
    {   
        /// <summary>
        /// Object being cached
        /// </summary>
        public TCacheObject Object { get; set; }
        
        /// <summary>
        /// Cache parameters
        /// </summary>
        public CacheParameters CacheParameters { get; set; }
    }
}
