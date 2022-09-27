using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Dependencies;
using PostSharp.Aspects.Serialization;
using PostSharp.Extensibility;

using System.Reflection;

namespace ST.Utils.Attributes
{
  /// <summary>
  /// Атрибут указывает, что класс является кэшем в рамках механизма кэширования.
  /// Интерфейсные методы класса, помеченного данным атрибутом, будут выполняться в контексте кэширования.
  /// </summary>
  [PSerializable]
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
  [MulticastAttributeUsage(MulticastTargets.Class, PersistMetaData = true)]
  [AspectConfiguration(SerializerType = typeof(MsilAspectSerializer))]
  [ProvideAspectRole(AspectRoles.Caching)]
  [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.Any, StandardRoles.Caching)]
  public sealed class CacheContextAttribute : TypeLevelAspect
  {
    #region .Static Fields
    [ThreadStatic]
    private static Stack<MethodBase> _callStack;
    #endregion

    #region .Fields
    private List<MethodInfo> _methods = new List<MethodInfo>(0);

    internal Type[] ExcludeInterfaces = new Type[0];
    #endregion

    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    public CacheContextAttribute() : base()
    {
    }

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="excludeInterfaces">Список интерфейсов, которые необходимо исключить из механизма кэширования.</param>
    public CacheContextAttribute( params Type[] excludeInterfaces ) : this()
    {
      if( excludeInterfaces != null )
        ExcludeInterfaces = excludeInterfaces;
    }
    #endregion

    #region CompileTimeInitialize
    public override void CompileTimeInitialize( Type type, AspectInfo aspectInfo )
    {
      _methods = (from i in type.GetInterfaces()
                  from m in type.GetInterfaceMap( i ).TargetMethods
                  where !ExcludeInterfaces.Contains( i )
                  select m).ToList();
    }
    #endregion

    #region IsCacheContext
    internal static bool IsCacheContext( MethodBase method )
    {
      return _callStack != null && _callStack.Count > 0 && _callStack.Peek() == method;
    }
    #endregion

    #region OnEntry
    [OnMethodEntryAdvice, MethodPointcut( "SelectMethods" )]
    public void OnEntry( MethodExecutionArgs args )
    {
      if( _callStack == null )
        _callStack = new Stack<MethodBase>();

      _callStack.Push( args.Method );
    }
    #endregion

    #region OnExit
    [OnMethodExitAdvice( Master = "OnEntry" ), MethodPointcut( "SelectMethods" )]
    public void OnExit( MethodExecutionArgs args )
    {
      _callStack.Pop();
    }
    #endregion

    #region SelectMethods
    private IEnumerable<MethodInfo> SelectMethods( Type type )
    {
      return _methods;
    }
    #endregion
  }
}
