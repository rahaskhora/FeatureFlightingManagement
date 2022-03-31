using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.FeatureFlighting.Common.Cache
{
    /// <summary>
    /// Manages backgroudn caching
    /// </summary>
    public interface IBackgroundCacheManager
    {
        /// <summary>
        /// Initializes the background cache
        /// </summary>
        /// <param name="period"></param>
        void Init(int period);
        Task Recache(LoggerTrackingIds trackingIds, CancellationToken cancellationToken);
        void Cleanup();
    }
}
