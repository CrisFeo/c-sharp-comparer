using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Comparer = System.Func<object, object, bool>;

public static class DeepCompare {

  // Internal vars
  //////////////////////

  static Dictionary<Type, Comparer> comparers = new Dictionary<Type, Comparer>();

  // Public methods
  //////////////////////

  public static new bool Equals(object a, object b) {
    if (ReferenceEquals(a, b)) return true;
    if (a == null || b == null) return false;
    if (a.GetType() != b.GetType()) return false;
    var type = a.GetType();
    if (!comparers.ContainsKey(type)) {
      comparers.Add(type, Create(type));
    }
    return comparers[type](a, b);
  }

  // Internal methods
  //////////////////////

  static Comparer Create(Type type) {
    if (type.IsPrimitive) return (a, b) => a.Equals(b);
    if (typeof(IEnumerable).IsAssignableFrom(type)) {
      return (a, b) => {
        var itA = (a as IEnumerable).GetEnumerator();
        var itB = (b as IEnumerable).GetEnumerator();
        while (true) {
          var hasNextA = itA.MoveNext();
          var hasNextB = itB.MoveNext();
          if (hasNextA != hasNextB) return false;
          if (!hasNextA) break;
          if (!Equals(itA.Current, itB.Current)) return false;
        }
        return true;
      };
    } else {
      var checks = new List<Comparer>();
      foreach (var field in type.GetFields()) {
        checks.Add((a, b) => Equals(
          field.GetValue(a),
          field.GetValue(b)
        ));
      }
      foreach (var prop in type.GetProperties()) {
        checks.Add((a, b) => Equals(
          prop.GetValue(a),
          prop.GetValue(b)
        ));
      }
      return (a, b) => checks.All(c => c(a, b));
    }
  }

}
