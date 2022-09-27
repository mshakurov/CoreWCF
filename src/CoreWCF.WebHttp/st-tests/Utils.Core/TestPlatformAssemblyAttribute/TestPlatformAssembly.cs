using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTests
{
  /// <summary>
  /// Атрибут, указывающий, что сборка к которой он применен, содержит типы, относящиеся к функционированию модулей и ядра.
  /// Только в сборках помеченных данным атрибутом сервер будет искать классы модулей, типы сообщений, разрешения доступа и т.п.
  /// </summary>
  [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
  public sealed class TestPlatformAssemblyAttribute : Attribute
  {
  }
}
