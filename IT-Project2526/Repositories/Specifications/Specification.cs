using System.Linq.Expressions;

namespace IT_Project2526.Repositories.Specifications;

/// <summary>
/// Specification pattern interface for flexible, composable query criteria.
/// </summary>
public interface ISpecification<T>
{
    /// <summary>
    /// The criteria expression that defines the filter.
    /// </summary>
    Expression<Func<T, bool>> Criteria { get; }

    /// <summary>
    /// Include expressions for eager loading related entities.
    /// </summary>
    List<Expression<Func<T, object>>> Includes { get; }

    /// <summary>
    /// String-based includes for nested properties.
    /// </summary>
    List<string> IncludeStrings { get; }

    /// <summary>
    /// Order by expression (ascending).
    /// </summary>
    Expression<Func<T, object>>? OrderBy { get; }

    /// <summary>
    /// Order by expression (descending).
    /// </summary>
    Expression<Func<T, object>>? OrderByDescending { get; }

    /// <summary>
    /// Number of records to take.
    /// </summary>
    int? Take { get; }

    /// <summary>
    /// Number of records to skip.
    /// </summary>
    int? Skip { get; }

    /// <summary>
    /// Whether pagination is enabled.
    /// </summary>
    bool IsPagingEnabled { get; }
}

/// <summary>
/// Base specification implementation with fluent builder pattern.
/// </summary>
public abstract class Specification<T> : ISpecification<T>
{
    public Expression<Func<T, bool>> Criteria { get; private set; } = x => true;
    public List<Expression<Func<T, object>>> Includes { get; } = new();
    public List<string> IncludeStrings { get; } = new();
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }
    public int? Take { get; private set; }
    public int? Skip { get; private set; }
    public bool IsPagingEnabled { get; private set; }

    protected void SetCriteria(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria;
    }

    protected void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        Includes.Add(includeExpression);
    }

    protected void AddInclude(string includeString)
    {
        IncludeStrings.Add(includeString);
    }

    protected void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
    {
        OrderBy = orderByExpression;
    }

    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescExpression)
    {
        OrderByDescending = orderByDescExpression;
    }

    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }
}

/// <summary>
/// Combined specification using AND logic.
/// </summary>
public class AndSpecification<T> : Specification<T>
{
    public AndSpecification(ISpecification<T> left, ISpecification<T> right)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        
        var leftBody = Expression.Invoke(left.Criteria, parameter);
        var rightBody = Expression.Invoke(right.Criteria, parameter);
        var andExpression = Expression.AndAlso(leftBody, rightBody);
        
        SetCriteria(Expression.Lambda<Func<T, bool>>(andExpression, parameter));
        
        // Merge includes
        foreach (var include in left.Includes.Concat(right.Includes))
            AddInclude(include);
        foreach (var include in left.IncludeStrings.Concat(right.IncludeStrings).Distinct())
            AddInclude(include);
    }
}

/// <summary>
/// Combined specification using OR logic.
/// </summary>
public class OrSpecification<T> : Specification<T>
{
    public OrSpecification(ISpecification<T> left, ISpecification<T> right)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        
        var leftBody = Expression.Invoke(left.Criteria, parameter);
        var rightBody = Expression.Invoke(right.Criteria, parameter);
        var orExpression = Expression.OrElse(leftBody, rightBody);
        
        SetCriteria(Expression.Lambda<Func<T, bool>>(orExpression, parameter));
    }
}

/// <summary>
/// Extension methods for specification composition.
/// </summary>
public static class SpecificationExtensions
{
    public static ISpecification<T> And<T>(this ISpecification<T> left, ISpecification<T> right)
    {
        return new AndSpecification<T>(left, right);
    }

    public static ISpecification<T> Or<T>(this ISpecification<T> left, ISpecification<T> right)
    {
        return new OrSpecification<T>(left, right);
    }
}
