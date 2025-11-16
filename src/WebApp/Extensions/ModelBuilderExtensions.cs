using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Query;

namespace WebApp.Extensions;

public static class ExpressionExtensions
{
    public static Expression<Func<TTarget, bool>> Convert<TSource, TTarget>(
        this Expression<Func<TSource, bool>> root)
    {
        var visitor = new ParameterTypeVisitor<TSource, TTarget>();
        return (Expression<Func<TTarget, bool>>)visitor.Visit(root)!;
    }

    private class ParameterTypeVisitor<TSource, TTarget> : ExpressionVisitor
    {
        private ReadOnlyCollection<ParameterExpression>? _parameters;

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return _parameters?.FirstOrDefault(p => p.Name == node.Name)
                   ?? (node.Type == typeof(TSource)
                       ? Expression.Parameter(typeof(TTarget), node.Name)
                       : node);
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            _parameters = VisitAndConvert(node.Parameters, nameof(VisitLambda));
            return Expression.Lambda(Visit(node.Body), _parameters);
        }
    }
}

public static class ModelBuilderExtensions
{
    private static readonly MethodInfo SetQueryFilterMethod = typeof(ModelBuilderExtensions)
        .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
        .Single(t => t is { IsGenericMethod: true, Name: nameof(SetQueryFilter) });

    public static void SetQueryFilterOnAllEntities<TEntityInterface>(
        this ModelBuilder builder,
        Expression<Func<TEntityInterface, bool>> filterExpression)
    {
        foreach (var clrType in builder.Model.GetEntityTypes()
                     .Where(t => t.BaseType == null)
                     .Select(t => t.ClrType)
                     .Where(t => typeof(TEntityInterface).IsAssignableFrom(t)))
        {
            builder.SetEntityQueryFilter(clrType, filterExpression);
        }
    }

    private static void SetEntityQueryFilter<TEntityInterface>(
        this ModelBuilder builder,
        Type entityType,
        Expression<Func<TEntityInterface, bool>> filterExpression)
    {
        SetQueryFilterMethod
            .MakeGenericMethod(entityType, typeof(TEntityInterface))
            .Invoke(null, [builder, filterExpression]);
    }

    private static void SetQueryFilter<TEntity, TEntityInterface>(
        ModelBuilder builder,
        Expression<Func<TEntityInterface, bool>> filterExpression)
        where TEntityInterface : class
        where TEntity : class, TEntityInterface
    {
        var concreteExpression = filterExpression.Convert<TEntityInterface, TEntity>();
        builder.Entity<TEntity>().AppendQueryFilter(concreteExpression);
    }

    private static void AppendQueryFilter<T>(
        this EntityTypeBuilder entityTypeBuilder,
        Expression<Func<T, bool>> expression)
        where T : class
    {
        var parameter = Expression.Parameter(entityTypeBuilder.Metadata.ClrType);

        Expression expressionFilter = ReplacingExpressionVisitor.Replace(
            expression.Parameters.Single(), parameter, expression.Body);

        var declaredFilters = entityTypeBuilder.Metadata.GetDeclaredQueryFilters();
        if (declaredFilters is { Count: > 0 })
        {
            var existingFilters =
                from queryFilter in declaredFilters
                select queryFilter.Expression into expr
                where expr != null
                let lambda = (LambdaExpression)expr
                let existingParam = lambda.Parameters.Single()
                select ReplacingExpressionVisitor.Replace(
                    existingParam,
                    parameter,
                    lambda.Body);

            expressionFilter = existingFilters.Aggregate(
                expressionFilter,
                (current, existingExpressionFilter) =>
                    Expression.AndAlso(existingExpressionFilter, current));
        }

        var lambdaExpression = Expression.Lambda(expressionFilter, parameter);
        entityTypeBuilder.HasQueryFilter(lambdaExpression);
    }
}