using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

static class Program {

  struct Thing {
    public int a;
    public int[] b;
    public Dictionary<string, int> c;
  }

  static void Main(string[] args) {
    var a = new Thing{
      a = 1,
      b = new[] { 1, 2, 3 },
      c = new Dictionary<string, int> {
        { "key1", 5 },
        { "key2", 6 },
        { "key3", 7 },
      },
    };
    var b = new Thing{
      a = 1,
      b = new[] { 1, 2, 3 },
      c = new Dictionary<string, int> {
        { "key1", 5 },
        { "key2", 6 },
        { "key3", 7 },
      },
    };
    Console.WriteLine($"{DeepCompare.Equals(a, b)}");
  }

}
