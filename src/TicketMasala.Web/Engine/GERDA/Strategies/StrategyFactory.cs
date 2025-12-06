namespace TicketMasala.Web.Engine.GERDA.Strategies;
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
                // Fallback: try to locate a concrete type by name in loaded assemblies and instantiate it
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var asm in assemblies)
                {
                    var candidate = asm.GetTypes()
                        .Where(t => typeof(TStrategy).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                        .FirstOrDefault(t => string.Equals(t.Name, strategyName, StringComparison.OrdinalIgnoreCase)
                                             || string.Equals(t.Name, strategyName + "Strategy", StringComparison.OrdinalIgnoreCase));

                    if (candidate != null)
                    {
                        var instance = (TStrategy)ActivatorUtilities.CreateInstance(_serviceProvider, candidate);
                        return instance;
                    }
                }

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
