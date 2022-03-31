using System;
using RE = RulesEngine;
using RulesEngine.Models;
using System.Threading.Tasks;
using RulesEngine.Interfaces;
using Microsoft.FeatureFlighting.Common;
using Microsoft.FeatureFlighting.Core.Spec;
using Microsoft.FeatureFlighting.Common.Cache;
using Microsoft.FeatureFlighting.Common.Config;
using Microsoft.FeatureFlighting.Common.Caching;
using Microsoft.FeatureFlighting.Common.Storage;
using Microsoft.FeatureFlighting.Common.AppExceptions;

namespace Microsoft.FeatureFlighting.Core.RulesEngine
{   
    /// <inheritdoc>/>
    public class RulesEngineManager: IRulesEngineManager, IBackgroundCacheableService<IRulesEngineEvaluator>
    {   
        private readonly ITenantConfigurationProvider _tenantConfigurationProvider;
        private readonly IBlobProviderFactory _blobProviderFactory;
        private readonly ICacheFactory _cacheFactory;

        public string CacheableServiceId => nameof(RulesEngineManager);

        public event EventHandler<CacheParameters> ObjectCached;

        public RulesEngineManager(IOperatorStrategy operatorEvaluatorStrategy, ITenantConfigurationProvider tenantConfigurationProvider, IBlobProviderFactory blobProviderFactory, ICacheFactory cacheFactory)
        {
            if (operatorEvaluatorStrategy == null)
                throw new ArgumentNullException(nameof(operatorEvaluatorStrategy));

            Operator.Initialize(operatorEvaluatorStrategy);
            _tenantConfigurationProvider = tenantConfigurationProvider ?? throw new ArgumentNullException(nameof(tenantConfigurationProvider));
            _blobProviderFactory = blobProviderFactory ?? throw new ArgumentNullException(nameof(blobProviderFactory));
            _cacheFactory = cacheFactory ?? throw new ArgumentNullException(nameof(cacheFactory));
        }

        /// <inheritdoc>/>
        public async Task<IRulesEngineEvaluator> Build(string tenant, string workflowName, LoggerTrackingIds trackingIds)
        {
            TenantConfiguration tenantConfiguration = await _tenantConfigurationProvider.Get(tenant);
            if (!tenantConfiguration.IsBusinessRuleEngineEnabled())
                return null;

            IRulesEngineEvaluator cachedRuleEngine = await GetCachedRuleEvaluator(tenant, workflowName, trackingIds);
            if (cachedRuleEngine != null)
                return cachedRuleEngine;

            IRulesEngineEvaluator evaluator = await CreateRulesEvaluator(workflowName, tenant, trackingIds, setCache: true);
            return evaluator;
        }

        private Task<IRulesEngineEvaluator> GetCachedRuleEvaluator(string tenant, string workflowName, LoggerTrackingIds trackingIds)
        {
            CacheParameters cacheParameters = new()
            {
                CacheKey = $"{tenant}_{workflowName}",
                ObjectId = workflowName,
                Tenant = tenant
            };
            return GetCachedObject(cacheParameters, trackingIds);
        }

        private async Task<IRulesEngineEvaluator> CreateRulesEvaluator(string workflowName, string tenant, LoggerTrackingIds trackingIds, bool setCache)
        {
            TenantConfiguration tenantConfiguration = await _tenantConfigurationProvider.Get(tenant);
            CacheParameters cacheParameters = new()
            {
                CacheKey = $"{tenant}_{workflowName}",
                ObjectId = workflowName,
                Tenant = tenant,
                CacheDuration = tenantConfiguration.BusinessRuleEngine.CacheDuration
            };

            return (await CreateCacheableObject(cacheParameters, setCache, trackingIds)).Object;
        }

        public async Task<IRulesEngineEvaluator> GetCachedObject(CacheParameters cacheParameters, LoggerTrackingIds trackingIds)
        {
            string workflowName = cacheParameters.ObjectId;
            ICache breCache = _cacheFactory.Create(cacheParameters.Tenant, nameof(TenantConfiguration.Cache.RulesEngine), trackingIds.CorrelationId, trackingIds.TransactionId);
            if (breCache == null)
                return null;

            IRulesEngineEvaluator cachedRuleEngine = await breCache.Get<RulesEngineEvaluator>(workflowName, trackingIds.CorrelationId, trackingIds.TransactionId);
            return cachedRuleEngine;
        }

        public async Task SetCacheObject(CacheableObject<IRulesEngineEvaluator> cacheableObject, LoggerTrackingIds trackingIds)
        {
            string workflowName = cacheableObject.CacheParameters.ObjectId;
            TenantConfiguration tenantConfiguration = await _tenantConfigurationProvider.Get(cacheableObject.CacheParameters.Tenant);
            ICache breCache = _cacheFactory.Create(cacheableObject.CacheParameters.Tenant, nameof(TenantConfiguration.Cache.RulesEngine), trackingIds.CorrelationId, trackingIds.TransactionId);
            if (breCache == null)
                return;

            await breCache.Set(workflowName, cacheableObject.Object, trackingIds.CorrelationId, trackingIds.TransactionId, relativeExpirationMins: tenantConfiguration.BusinessRuleEngine.CacheDuration);
            ObjectCached?.Invoke(this, cacheableObject.CacheParameters);
        }

        public async Task<CacheableObject<IRulesEngineEvaluator>> CreateCacheableObject(CacheParameters cacheParameters, bool setCache, LoggerTrackingIds trackingIds)
        {
            string workflowName = cacheParameters.ObjectId;
            TenantConfiguration tenantConfiguration = await _tenantConfigurationProvider.Get(cacheParameters.Tenant);
            cacheParameters.CacheDuration = tenantConfiguration.BusinessRuleEngine.CacheDuration;

            IBlobProvider blobProvider = await _blobProviderFactory.CreateBreWorkflowProvider(cacheParameters.Tenant);
            string workflowJson = await blobProvider.Get($"{workflowName}.json", trackingIds);
            if (string.IsNullOrWhiteSpace(workflowJson))
                throw new RuleEngineException(workflowName, cacheParameters.Tenant, "Rule engine not found in the configured storage location", "FeatureFlighting.RuleEngineManager.Build", trackingIds.CorrelationId, trackingIds.TransactionId);

            IRulesEngine ruleEngine = new RE.RulesEngine(
                jsonConfig: new string[] { workflowJson },
                reSettings: new ReSettings() { CustomTypes = new Type[] { typeof(Operator) } },
                logger: null);

            
            IRulesEngineEvaluator evaluator = new RulesEngineEvaluator(ruleEngine, workflowName, tenantConfiguration);
            CacheableObject<IRulesEngineEvaluator> cacheableRulesEngineEvaluator = new()
            {
                Object = evaluator,
                CacheParameters = cacheParameters
            };

            if (setCache)
                await SetCacheObject(cacheableRulesEngineEvaluator, trackingIds);

            return cacheableRulesEngineEvaluator;
        }

        public Task Recache(CacheParameters cacheParameters, LoggerTrackingIds trackingIds)
        {
            return CreateCacheableObject(cacheParameters, true, trackingIds);
        }
    }
}
