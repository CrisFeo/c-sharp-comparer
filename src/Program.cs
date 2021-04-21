using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AgileObjects.ReadableExpressions;

static class Program {

  struct Thing {
    public int a;
    public int[] b;
    public Dictionary<string, int> c;
  }

  static void Case<T>(T a, T b, bool expected) {
    var aStr = a?.ToString() ?? "<null>";
    var bStr = b?.ToString() ?? "<null>";
    var title = $"DeepCompare<{typeof(T).Name}>.Compare({aStr}, {bStr})";
    try {
      var actual = DeepCompare<T>.Compare(a, b);
      var status = actual == expected ? "PASS" : "FAIL";
      Console.WriteLine($"{status} {title}");
    } catch (Exception) {
      Console.WriteLine($"FAIL {title}");
      throw;
    }
  }

  static void PrintSource<T>() {
    var source = DeepCompare<T>.expression.ToReadableString(c => c
      .UseExplicitTypeNames
      .ShowLambdaParameterTypes
    ).Replace(Environment.NewLine, "  "+ Environment.NewLine);
    Console.WriteLine($"comparer for {typeof(T).Name}\n  {source}");
  }

  static void Main(string[] args) {
    Case<int>   (0,        0,        true );
    Case<int>   (0,        1,        false);
    Case<int>   (20,       300,      false);
    Case<int>   (44,       44,       true );
    Case<string>(null,     null,     true );
    Case<string>(null,     "",       false);
    Case<string>("",       "",       true );
    Case<string>("hello",  "hello",  true );
    Case<string>("hellos", "hel1os", false);
    Case<string>("hello", "hellos",  false);
    var a = new Thing {
      a = 1,
      b = new[] { 1, 2, 3 },
      c = new Dictionary<string, int> {
        { "key1", 5 },
        { "key2", 6 },
        { "key3", 7 },
      },
    };
    var b = a;
    Case<Thing>(a, b, true);
    b = new Thing {
      a = 1,
      b = new[] { 1, 2, 3 },
      c = new Dictionary<string, int> {
        { "key1", 5 },
        { "key2", 6 },
        { "key3", 7 },
      },
    };
    Case<Thing>(a, b, true);
    b.c["key4"] = 13;
    Case<Thing>(a, b, false);
    b.c = null;
    Case<Thing>(a, b, false);
    PrintSource<Dictionary<string, int>>();
    PrintSource<Thing>();
  }

}
