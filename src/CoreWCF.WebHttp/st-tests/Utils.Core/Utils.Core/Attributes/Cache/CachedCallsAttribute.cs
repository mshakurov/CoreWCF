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
  /// Атрибут указывает, что вызовы методов интерфейсов в экземпляре класса, помеченного данным атрибутом, будут выполняться в рамках механизма кэширования.
  /// Это означает, что при вызове интерфейсного метода экземпляра непосредственно, данный вызов будет перенаправлен соответствующему интерфейсному методу кэша.
  /// При вызове интерфейсного метода экземпляра из соответствующего интерфейсного метода кэша, будет выполнен код интерфейсного метода экземпляра.
  /// Список интерфейсов, участвующих в данном механизме, определяется типом, реализующим кэш.
  /// </summary>
  [PSerializable]
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
  [MulticastAttributeUsage(MulticastTargets.Class, AllowMultiple = false)]
  [AspectConfiguration(SerializerType = typeof(MsilAspectSerializer))]
  [ProvideAspectRole(AspectRoles.Caching)]
  public sealed class CachedCallsAttribute : TypeLevelAspect
  {
    #region .Fields
    private Type _cacheType;
    private PropertyInfo _instanceProperty;
    private CacheContextAttribute _cacheContext;

    private Dictionary<MethodBase, MethodBase> _cachedMethods;
    #endregion

    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="cacheType">Тип, реализующий кэш. Должен быть помечен атрибутом CacheContextAttribute.</param>
    /// <param name="instanceProperty">Название открытого статического свойства типа cacheType, содержащего экземпляр кэша.</param>
    public CachedCallsAttribute( Type cacheType, string instanceProperty )
    {
      _cacheType = cacheType;
      _instanceProperty = cacheType.GetProperty( instanceProperty, BindingFlags.Static | BindingFlags.Public );

      if( _cacheType != null )
        _cacheContext = _cacheType.GetAttribute<CacheContextAttribute>();
    }
    #endregion

    #region CompileTimeInitialize
    public override void CompileTimeInitialize( Type type, AspectInfo aspectInfo )
    {
      RuntimeInitialize( type );
    }
    #endregion

    #region CompileTimeValidate
    public override bool CompileTimeValidate( Type type )
    {
      return _cacheType == null ? AspectHelper.Fail( 1, "Constructor parameter 'cacheType' of attribute CachedCallAttribute can't be null." ) :
             _instanceProperty == null ? AspectHelper.Fail( 2, "The property not found for constructor parameter 'instanceProperty' of attribute CachedCallAttribute." ) :
             _cacheContext == null ? AspectHelper.Fail( 3, "The type {0} is not marked with attribute CacheContextAttribute.", _cacheType ) :
             true;
    }
    #endregion

    #region OnEntry
    [OnMethodEntryAdvice, MethodPointcut( "SelectMethods" )]
    public void OnEntry( MethodExecutionArgs args )
    {
      var cacheMethod = _cachedMethods[args.Method];

      if( !CacheContextAttribute.IsCacheContext( cacheMethod ) )
      {
        var cache = _instanceProperty.GetValue( null, null );

        if( cache != null )
        {
          args.ReturnValue = cacheMethod.Invoke( cache, args.Arguments.ToArray() );
          args.FlowBehavior = FlowBehavior.Return;
        }
      }
    }
    #endregion

    #region RuntimeInitialize
    public override void RuntimeInitialize( Type type )
    {
      _cachedMethods = new Dictionary<MethodBase, MethodBase>();

      if( _cacheContext != null )
        foreach( var iface in _cacheType.GetInterfaces().Except( _cacheContext.ExcludeInterfaces ) )
        {
          if( !iface.IsAssignableFrom( type ) )
            continue;

          var cacheMap = _cacheType.GetInterfaceMap( iface );
          var typeMap = type.GetInterfaceMap( iface );

          for( int i = 0; i < cacheMap.TargetMethods.Length; i++ )
            for( int j = 0; j < typeMap.TargetMethods.Length; j++ )
              if( cacheMap.InterfaceMethods[i] == typeMap.InterfaceMethods[j] )
                _cachedMethods.Add( typeMap.TargetMethods[j], cacheMap.TargetMethods[i] );
        }
    }
    #endregion

    #region SelectMethods
    private IEnumerable<MethodBase> SelectMethods( Type type )
    {
      return _cachedMethods.Keys.ToList();
    }
    #endregion
  }
}
