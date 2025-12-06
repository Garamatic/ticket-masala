namespace IT_Project2526.Services.GERDA.Strategies
{
    public class StrategyFactory : IStrategyFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public StrategyFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public TStrategy GetStrategy<TStrategy, TReturn>(string strategyName) where TStrategy : IStrategy<TReturn>
        {
            // Resolve all implementations of TStrategy
            var strategies = _serviceProvider.GetServices<TStrategy>();
            
            var strategy = strategies.FirstOrDefault(s => s.Name.Equals(strategyName, StringComparison.OrdinalIgnoreCase));
            
            if (strategy == null)
            {
                throw new InvalidOperationException($"Strategy '{strategyName}' of type {typeof(TStrategy).Name} not found.");
            }

            return strategy;
        }

        public TReturn? ExecuteStrategy<TStrategy, TReturn>(string strategyName, Func<TStrategy, TReturn> execution) where TStrategy : IStrategy<TReturn>
        {
            var strategy = GetStrategy<TStrategy, TReturn>(strategyName);
            return execution(strategy);
        }
    }
}
