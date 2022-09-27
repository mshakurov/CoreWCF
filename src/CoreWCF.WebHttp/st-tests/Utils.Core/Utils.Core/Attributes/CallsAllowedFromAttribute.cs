using PostSharp.Aspects;
using PostSharp.Aspects.Dependencies;
using PostSharp.Extensibility;

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ST.Utils.Attributes
{
  /// <summary>
  /// Атрибут указывает, что помеченный им метод будет доступен для вызова только из собственной и указанных сборок.
  /// </summary>
  [PSerializable]
  [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
  [MulticastAttributeUsage(MulticastTargets.Method, AllowMultiple = false, Inheritance = MulticastInheritance.None)]
  [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.Before, AspectRoles.DataBinding)]
  [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.Before, AspectRoles.Modification)]
  [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.Before, AspectRoles.Persistence)]
  [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.Before, AspectRoles.Validation)]
  [ProvideAspectRole(AspectRoles.Security)]
  public sealed class CallsAllowedFromAttribute : OnMethodBoundaryAspect
  {
    #region .Fields
    private string[] _assemblies;
    #endregion

    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="assemblies">Простые названия сборок (исключая сборку, объявляющюю метод), из которых позволяется вызывать метод.</param>
    public CallsAllowedFromAttribute(params string[] assemblies)
    {
      _assemblies = assemblies;
    }
    #endregion

    #region CompileTimeValidate
    /// <summary>
    /// См. базовый класс.
    /// </summary>
    /// <param name="method">См. базовый класс.</param>
    /// <returns>См. базовый класс.</returns>
    public override bool CompileTimeValidate(MethodBase method)
    {
      if ((method.GetMethodImplementationFlags() & MethodImplAttributes.NoInlining) == MethodImplAttributes.NoInlining)
        return true;

      return AspectHelper.Fail(1, "CallsAllowedFromAttribute can't be applied to method {0}.{1}: the method isn't marked with MethodImplOptions.NoInlining.", method.DeclaringType, method.Name);
    }
    #endregion

    #region OnEntry
    /// <summary>
    /// См. базовый класс.
    /// </summary>
    /// <param name="args">См. базовый класс.</param>
    [DebuggerStepThrough]
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public override void OnEntry(MethodExecutionArgs args)
    {
      var st = new StackTrace();

      var callingAssembly = st.GetFrame(1).GetMethod().DeclaringType.Assembly.GetName().Name;
      var callerAssembly = st.GetFrame(2).GetMethod().DeclaringType.Assembly.GetName().Name;

      var frame = st.GetFrame(7);

      string callerAssemblyThroughCore;

      if (frame != null)
      {
        var declType = frame.GetMethod().DeclaringType;

        if (declType != null)
        {
          var assembly = declType.Assembly;

          callerAssemblyThroughCore = assembly != null ? assembly.GetName().Name : Guid.NewGuid().ToString();
        }
        else
          callerAssemblyThroughCore = Guid.NewGuid().ToString();
      }
      else
        callerAssemblyThroughCore = Guid.NewGuid().ToString();

      if (callingAssembly == callerAssembly ||
        (_assemblies != null && _assemblies.Length > 0 && callerAssembly.In(_assemblies)) ||
        (_assemblies != null && _assemblies.Length > 0 && callerAssemblyThroughCore.In(_assemblies)))
        base.OnEntry(args);
      else
        throw new MethodAccessException(string.Format("The method '{0}' is not accessible from the assembly '{1}'.", args.Method, callerAssembly));
    }
    #endregion
  }
}
