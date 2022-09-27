using PostSharp.Aspects;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Dependencies;
using PostSharp.Aspects.Serialization;
using PostSharp.Extensibility;
using PostSharp.Reflection;

using System.Diagnostics;
using System.Reflection;

namespace ST.Utils.Attributes
{
  /// <summary>
  /// Атрибут для локализации свойств классов и параметров методов.
  /// Строковое свойство, содержащее идентификатор ресурса, будет возвращать локализованное значение ресурса по данному идентификатору.
  /// Свойство другого типа будет возвращать локализованное значение ресурса по идентификатору, содержащемуся в свойстве с названием {Название данного свойства}Id.
  /// Строковый параметр метода будет принимать локализованное значение ресурса, если при вызове метода этот параметр содержит идентификатор ресурса.
  /// </summary>
  // Из методов этого класса нельзя обращаться к элементам, к которым применен PostSharp!
  [PSerializable]
  [AttributeUsage( AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true )]
  [MulticastAttributeUsage( MulticastTargets.Property | MulticastTargets.Parameter, Inheritance = MulticastInheritance.None )]
  [AspectConfiguration( SerializerType = typeof( MsilAspectSerializer ) )]
  [AspectRoleDependency( AspectDependencyAction.Order, AspectDependencyPosition.Before, AspectRoles.Validation )]
  [ProvideAspectRole( AspectRoles.Modification )]
  public sealed class LocalizedAttribute : LocationLevelAspect, IAspectProvider
  {
    #region .Static Fields
    private static readonly Dictionary<MethodBase, LocalizedParameterAttribute> _parameterLocalizers = new Dictionary<MethodBase, LocalizedParameterAttribute>();
    #endregion

    #region CompileTimeValidate
    public override bool CompileTimeValidate( LocationInfo locationInfo )
    {
      if( locationInfo.LocationKind == LocationKind.Parameter )
      {
        if( locationInfo.ParameterInfo.ParameterType != typeof( string ) )
          return AspectHelper.Fail( 1, "{0} is uncompatible with type {1} for parameter {2}.{3}.", typeof( LocalizedAttribute ).Name, locationInfo.LocationType.Name,
                                    locationInfo.ParameterInfo.Member.Name, locationInfo.ParameterInfo.Name );

        if( (locationInfo.ParameterInfo.Member as MethodBase).IsConstructor )
          return AspectHelper.Fail( 2, "Can't apply the attribute LocalizedAttribute to the constructor of type {0}. Manually call to the method SR.GetString should be used.", (locationInfo.ParameterInfo.Member as MethodBase).DeclaringType );
      }

      if( locationInfo.LocationKind == LocationKind.Property && locationInfo.LocationType.IsValueType )
        return AspectHelper.Fail( 3, "{0} is uncompatible with type {1} for property {2}.{3}.", typeof( LocalizedAttribute ).Name, locationInfo.LocationType.Name,
                                  locationInfo.PropertyInfo.DeclaringType.Name, locationInfo.PropertyInfo.Name );

      return true;
    }
    #endregion

    #region ProvideAspects
    public IEnumerable<AspectInstance> ProvideAspects( object targetElement )
    {
      var li = targetElement as LocationInfo;

      if( li.LocationKind == LocationKind.Property )
      {
        var localizer = new LocalizedPropertyAttribute();

        yield return new AspectInstance( li.PropertyInfo, localizer, localizer.GetAspectConfiguration( targetElement ) );
      }
      else
        if( li.LocationKind == LocationKind.Parameter )
        {
          var parameterInfo = li.ParameterInfo;

          var methodBase = parameterInfo.Member as MethodBase;

          LocalizedParameterAttribute localizer;

          if( !_parameterLocalizers.TryGetValue( methodBase, out localizer ) )
          {
            localizer = new LocalizedParameterAttribute( methodBase );

            _parameterLocalizers.Add( methodBase, localizer );
          }

          if( localizer.Add( parameterInfo ) )
            yield return new AspectInstance( methodBase, localizer, localizer.GetAspectConfiguration( methodBase ) );
        }
    }
    #endregion

    /// <summary>
    /// Класс для внутреннего использования (сделан public из-за требования PostSharp).
    /// </summary>
    // Из методов этого класса нельзя обращаться к элементам, к которым применен PostSharp!
    [PSerializable]
    [AspectRoleDependency( AspectDependencyAction.Order, AspectDependencyPosition.Before, AspectRoles.Validation )]
    [ProvideAspectRole( AspectRoles.Modification )]
    public sealed class LocalizedParameterAttribute : MethodInterceptionAspect
    {
      #region .Fields
      private List<ParameterInfo> _parameters = new List<ParameterInfo>();

      [PNonSerialized]
      private readonly int _parametersCount;
      #endregion

      #region .Ctor
      internal LocalizedParameterAttribute( MethodBase methodBase )
      {
        _parametersCount = methodBase.GetParameters().SelectMany( pi => Attribute.GetCustomAttributes( pi, typeof( LocalizedAttribute ), true ) ).Count();
      }
      #endregion

      #region Add
      internal bool Add( ParameterInfo parameterInfo )
      {
        _parameters.Add( parameterInfo );

        return _parameters.Count == _parametersCount;
      }
      #endregion

      #region OnInvoke
      [DebuggerStepThrough]
      public override void OnInvoke( MethodInterceptionArgs args )
      {
        foreach( var p in _parameters )
        {
          var value = args.Arguments[p.Position] as string;

          if( SR.IsResourceId( value ) )
            args.Arguments[p.Position] = SR.GetString( value );
        }

        base.OnInvoke( args );
      }
      #endregion
    }

    /// <summary>
    /// Класс для внутреннего использования (сделан public из-за требования PostSharp).
    /// </summary>
    // Из методов этого класса нельзя обращаться к элементам, к которым применен PostSharp!
    [PSerializable]
    [LocationInterceptionAspectConfiguration( SerializerType = typeof( MsilAspectSerializer ) )]
    [AspectRoleDependency( AspectDependencyAction.Order, AspectDependencyPosition.Before, AspectRoles.Validation )]
    [ProvideAspectRole( AspectRoles.Modification )]
    public sealed class LocalizedPropertyAttribute : LocationInterceptionAspect
    {
      #region .Fields
      [PNonSerialized]
      private MethodInfo _getResourceId;
      #endregion

      #region .Properties
      /// <summary>
      /// Имя свойства, возвращающего идентификатор ресурса. Используется для только для не строковых локализуемых свойств.
      /// </summary>
      public string ResourceIdSource { get; set; }
      #endregion

      #region .Ctor
      internal LocalizedPropertyAttribute()
      {
      }
      #endregion

      #region GetResourceIdStub
      private static object GetResourceIdStub()
      {
        return null;
      }
      #endregion

      #region OnGetValue
      [DebuggerStepThrough]
      public override void OnGetValue( LocationInterceptionArgs args )
      {
        if( _getResourceId == null )
        {
          args.ProceedGetValue();

          var value = args.Value as string;

          if( SR.IsResourceId( value ) )
            args.Value = SR.GetString( value );
        }
        else
          args.Value = SR.GetObject( _getResourceId.Invoke( args.Instance, null ) as string );
      }
      #endregion

      #region RuntimeInitialize
      public override void RuntimeInitialize( LocationInfo locationInfo )
      {
        var p = locationInfo.DeclaringType.GetProperty( ResourceIdSource ?? locationInfo.Name + "Id" );

        _getResourceId = locationInfo.LocationType == typeof( string ) ? null :
                         p == null ? MemberHelper.GetMethod( () => LocalizedPropertyAttribute.GetResourceIdStub() ) :
                         p.GetGetMethod();
      }
      #endregion
    }
  }
}