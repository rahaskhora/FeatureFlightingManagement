using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.FeatureFlighting.Common;
using Microsoft.FeatureFlighting.Common.Cache;

namespace Microsoft.FeatureFlighting.Infrastructure.Cache
{
    internal class BackgroundCacheManager : IBackgroundCacheManager
    {
        private readonly IList<IBackgroundCacheableService> _cacheableServices;
        private static readonly Dictionary<string, List<CacheParameters>> _backgroundCacheablesMap = new();
        private int _period = 5;

        public BackgroundCacheManager(IEnumerable<IBackgroundCacheableService> backgroundCacheableServices)
        {
            _cacheableServices = backgroundCacheableServices.ToList();
        }

        public void Init(int period = 5)
        {
            _period = period > 0 ? period : _period;
            foreach (IBackgroundCacheableService cacheableService in _cacheableServices)
            {
                if (!(_backgroundCacheablesMap.ContainsKey(cacheableService.CacheableServiceId)))
                    _backgroundCacheablesMap.Add(cacheableService.CacheableServiceId, new());
                cacheableService.ObjectCached += AddCacheParameter;
            }
        }

        private void AddCacheParameter(object sender, CacheParameters cacheParameters)
        {
            string cacheableServiceId = (sender as IBackgroundCacheableService)?.CacheableServiceId ?? string.Empty;
            if (!_backgroundCacheablesMap.ContainsKey(cacheableServiceId))
                return;

            cacheParameters.UpdateNextRecacheTimestamp();
            List<CacheParameters> cachedParameters = _backgroundCacheablesMap[cacheableServiceId];
            if (!cachedParameters.Any(param => param.CacheKey == cacheParameters.CacheKey))
            {
                _backgroundCacheablesMap[cacheableServiceId].Add(cacheParameters);
            }
        }

        public async Task Recache(LoggerTrackingIds trackingIds, CancellationToken cancellationToken = default)
        {
            if (_backgroundCacheablesMap == null || !_backgroundCacheablesMap.Any())
                return;

            foreach (IBackgroundCacheableService cacheableService in _cacheableServices)
            {
                if (_backgroundCacheablesMap.ContainsKey(cacheableService.CacheableServiceId))
                {
                    foreach (CacheParameters cacheParameters in
                        _backgroundCacheablesMap[cacheableService.CacheableServiceId].Where(param => param.ShouldRecache(_period)))
                    {

                        await cacheableService.Recache(cacheParameters, trackingIds).ConfigureAwait(false);
                        cacheParameters.UpdateNextRecacheTimestamp();
                    }
                }
            }
        }

        public void Cleanup()
        {
            _cacheableServices.Clear();
            _backgroundCacheablesMap.Clear();
        }
    }
}
