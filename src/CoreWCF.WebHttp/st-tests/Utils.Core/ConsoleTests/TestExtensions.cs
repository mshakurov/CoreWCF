using ST.Utils;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTests
{
  public static class TestExtensions
  {
    public static Dictionary<Type, Func<object, Type, string?>> TypeDumpers = new Dictionary<Type, Func<object, Type, string?>>();

    public static string? DumpProps(this object? obj, string name, int maxLevel = 5, string delimiter = ", ")
    {
      return DumpPropsWithLevel(obj, name, 0, maxLevel, delimiter);
    }

    private static string? DumpPropsWithLevel(this object? obj, string name, int level, int maxLevel, string delimiter)
    {
      if (level == maxLevel || obj == null)
        return $"[{name} = {obj}]";

      var type = obj.GetType();
      Func<object, Type, string?>? dumper;
      if (TypeDumpers.TryGetValue(type, out dumper))
        return $"[{name} = {dumper(obj, type)}]";

      if (type == typeof(string) || type == typeof(DateTime) | type.IsValueType || type.IsPrimitive || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) && type.GetGenericArguments()[0].IfNotNull(gtype => gtype.IsValueType || gtype.IsPrimitive))
        return $"[{name} = {obj}]";

      if (obj is IEnumerable ienum)
        return $"[{name} = {{{ienum.OfType<object>().JoinAsStringsN(int.MaxValue, (o, i) => o.DumpPropsWithLevel($"{i + 1}", level + 1, maxLevel, delimiter) ?? String.Empty, delimiter)}}}]";

      return $"[{name} = {obj?.GetType().GetProperties().Where(p => p.CanRead && p.GetIndexParameters().Length == 0).Select(p => (p, v: Exec.Try(() => p.GetValue(obj, null), ex => $"# {ex.GetFullMessage()}"))).JoinAsStrings(int.MaxValue, o => $"{o.v.DumpPropsWithLevel(o.p.Name, level + 1, maxLevel, delimiter)}", delimiter)}]";
    }
  }
}
