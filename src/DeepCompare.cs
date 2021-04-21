using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

public static class DeepCompare<T> {

  // Internal vars
  //////////////////////

#if DEBUG
  internal static Expression expression;
#endif

  // Public vars
  //////////////////////

  public static Func<T, T, bool> Comparer { get; private set; }

  // Static constructor
  //////////////////////

  static DeepCompare() {
    var type = typeof(T);
    var a = Expression.Parameter(type, "a");
    var b = Expression.Parameter(type, "b");
    var lambda = Expression.Lambda<Func<T, T, bool>>(
      Compare(type, a, b),
      new ParameterExpression[] { a, b }
    );
#if DEBUG
    expression = lambda;
#endif
    Comparer = lambda.Compile();
  }

  // Public methods
  //////////////////////

  public static bool Compare(T a, T b) => Comparer(a, b);

  // Internal methods
  //////////////////////

  static Type GetEnumerableType(Type type) {
    if (type.IsInterface && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)) {
      return type.GenericTypeArguments[0];
    }
    foreach (Type i in type.GetInterfaces()) {
      if (!i.IsGenericType) continue;
      if (i.GetGenericTypeDefinition() != typeof(IEnumerable<>)) continue;
      return i.GenericTypeArguments[0];

    }
    return null;
  }

  static V AssertNotNull<V>(V v) => v != null ? v : throw new Exception("failed assert");

  static Type MustMakeGenericType(Type type, params Type[] parameters) => AssertNotNull(type.MakeGenericType(parameters));

  static MethodInfo MustGetMethod(Type type, string name) => AssertNotNull(type.GetMethod(name));

  static PropertyInfo MustGetProperty(Type type, string name) => AssertNotNull(type.GetProperty(name));

  static MethodInfo MustGetDeepCompareMethod(Type type) {
    var deepCompareType = MustMakeGenericType(typeof(DeepCompare<>), type);
    System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(deepCompareType.TypeHandle);
    return MustGetMethod(deepCompareType, "Compare");
  }

  static Expression Compare(Type type, Expression a, Expression b) {
    if (type.IsPrimitive) return Expression.Equal(a, b);
    var comparison = default(Expression);
    var elementType = GetEnumerableType(type);
    if (elementType != null) {
      var enumerableType = MustMakeGenericType(typeof(IEnumerable<>), elementType);
      var enumerableGetEnumerator = MustGetMethod(enumerableType, "GetEnumerator");
      var enumeratorMoveNext = MustGetMethod(typeof(IEnumerator), "MoveNext");
      var enumeratorType = MustMakeGenericType(typeof(IEnumerator<>), elementType);
      var enumeratorCurrent = MustGetProperty(enumeratorType, "Current");
      var elementCompare = MustGetDeepCompareMethod(elementType);
      var itA = Expression.Variable(enumeratorType, "itA");
      var itB = Expression.Variable(enumeratorType, "itB");
      var result = Expression.Variable(typeof(bool), "result");
      var hasNextA = Expression.Variable(typeof(bool), "hasNextA");
      var hasNextB = Expression.Variable(typeof(bool), "hasNextB");
      var breakLoop = Expression.Label();
      comparison = Expression.Block(
        new ParameterExpression[] { itA, itB, result},
        new Expression[] {
          Expression.Assign(itA, Expression.Call(a, enumerableGetEnumerator)),
          Expression.Assign(itB, Expression.Call(b, enumerableGetEnumerator)),
          Expression.Assign(result, Expression.Constant(true)),
          Expression.Loop(
            Expression.Block(
              new ParameterExpression[] { hasNextA, hasNextB },
              new Expression[] {
                Expression.Assign(hasNextA, Expression.Call(itA, enumeratorMoveNext)),
                Expression.Assign(hasNextB, Expression.Call(itB, enumeratorMoveNext)),
                Expression.IfThen(
                  Expression.NotEqual(hasNextA, hasNextB),
                  Expression.Block(
                    new Expression[] {
                      Expression.Assign(result, Expression.Constant(false)),
                      Expression.Break(breakLoop)
                    }
                  )
                ),
                Expression.IfThen(
                  Expression.IsFalse(hasNextA),
                  Expression.Break(breakLoop)
                ),
                Expression.IfThen(
                  Expression.IsFalse(
                    Expression.Call(
                      elementCompare,
                      Expression.Property(itA, enumeratorCurrent),
                      Expression.Property(itB, enumeratorCurrent)
                    )
                  ),
                  Expression.Block(
                    new Expression[] {
                      Expression.Assign(result, Expression.Constant(false)),
                      Expression.Break(breakLoop)
                    }
                  )
                )
              }
            )
          ),
          Expression.Label(breakLoop),
          result,
        }
      );
    } else {
      foreach (var field in type.GetFields()) {
        var fieldCompare = MustGetDeepCompareMethod(field.FieldType);
        var fieldComparison = Expression.Call(
          fieldCompare,
          Expression.Field(a, field),
          Expression.Field(b, field)
        );
        if (comparison == null) {
          comparison = fieldComparison;
        } else {
          comparison = Expression.AndAlso(
            comparison,
            fieldComparison
          );
        }
      }
      foreach (var prop in type.GetProperties()) {
        var propCompare = MustGetDeepCompareMethod(prop.PropertyType);
        var propComparison = Expression.Call(
          propCompare,
          Expression.Property(a, prop),
          Expression.Property(b, prop)
        );
        if (comparison == null) {
          comparison = propComparison;
        } else {
          comparison = Expression.AndAlso(
            comparison,
            propComparison
          );
        }
      }
    }
    if (type.IsValueType) return comparison;
    return Expression.OrElse(
      Expression.ReferenceEqual(a, b),
      Expression.AndAlso(
        Expression.AndAlso(
          Expression.NotEqual(a, Expression.Constant(null)),
          Expression.NotEqual(b, Expression.Constant(null))
        ),
        comparison
      )
    );
  }

}
