namespace IT_Project2526.Services.GERDA.Strategies
{
    public interface IStrategyFactory
    {
        TReturn? ExecuteStrategy<TStrategy, TReturn>(string strategyName, Func<TStrategy, TReturn> execution) where TStrategy : IStrategy<TReturn>;
        
        TStrategy GetStrategy<TStrategy, TReturn>(string strategyName) where TStrategy : IStrategy<TReturn>;
    }
}
