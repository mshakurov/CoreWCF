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
  /// Базовый класс для реализации валидаторов параметров и свойств.
  /// </summary>
  // Из методов этого класса нельзя обращаться к элементам, к которым применен PostSharp!
  [PSerializable]
  [AttributeUsage( AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true )]
  [MulticastAttributeUsage( MulticastTargets.Property | MulticastTargets.Parameter, AllowMultiple = false, Inheritance = MulticastInheritance.None, PersistMetaData = true )]
  [AspectConfiguration( SerializerType = typeof( MsilAspectSerializer ) )]
  [ProvideAspectRole( AspectRoles.Validation )]
  public abstract class ValidatorAspect : LocationLevelAspect, IAspectProvider
  {
    #region .Static Fields
    private static readonly Dictionary<PropertyInfo, PropertyValidator> _propertyValidators = new();
    private static readonly Dictionary<MethodBase, ParameterValidator> _parameterValidators = new();

    private static readonly Type[] _numericTypes = new Type[]
    {
      typeof( sbyte ), typeof( byte ), typeof( short ), typeof( ushort ), typeof( char ), typeof( int ),
      typeof( uint ), typeof( long ), typeof( ulong ), typeof( float ), typeof( double ), typeof( decimal )
    };
    #endregion

    #region .Properties
    /// <summary>
    /// Признак того, что в окончательном коде аспект-валидатор создаваться не будет. 
    /// Значение по умолчанию - false, т.е. по умолчанию аспект-валидатор в окончательном коде создаётся.
    /// </summary>
    public bool SuppressAspectGeneration { get; set; }
    #endregion

    #region GetElementType
    /// <summary>
    /// Возвращает тип элемента для указанного описателя элемента.
    /// </summary>
    /// <param name="locationInfo">Описатель элемента.</param>
    /// <returns>Тип элемента.</returns>
    protected static Type GetElementType( LocationInfo locationInfo )
    {
      var elementType = locationInfo.LocationKind == LocationKind.Property ? locationInfo.PropertyInfo.PropertyType :
                        locationInfo.LocationKind == LocationKind.Parameter ? locationInfo.ParameterInfo.ParameterType :
                        null;

      if( elementType == null )
        throw new InvalidOperationException( "Invalid LocationKind of locationInfo parameter." );

      return elementType;
    }
    #endregion

    #region GetValidationException
    /// <summary>
    /// Возвращает исключение, соответствующее нарушенному требованию на значение поля/свойства, или null, если требование соблюдено.
    /// </summary>
    /// <param name="value">Значение свойства или параметра.</param>
    /// <param name="name">Имя свойства или параметра.</param>
    /// <returns>Исключение, соответсвующее нарушенному требованию, или null, если значение допустимо.</returns>
    protected abstract Exception GetValidationException( object value, string name = null );
    #endregion

    #region IsNumericType
    /// <summary>
    /// Определяет, возможно ли указанный тип преобразовать в double.
    /// </summary>
    /// <param name="type">Тип.</param>
    /// <returns>True - указанный тип преобразуется в double, иначе - False.</returns>
    protected static bool IsNumericType( Type type )
    {
      return _numericTypes.Contains( type );
    }
    #endregion

    #region IsValueValid
    /// <summary>
    /// Проверяет допустимость значения свойства или параметра.
    /// </summary>
    /// <param name="value">Значение свойства или параметра.</param>
    /// <returns>True - значение допустимо, False - недопустимо.</returns>
    public bool IsValueValid( object value )
    {
      return GetValidationException( value ) == null;
    }
    #endregion

    #region ProvideAspects
    public IEnumerable<AspectInstance> ProvideAspects( object targetElement )
    {
      if( !SuppressAspectGeneration )
      {
        var li = targetElement as LocationInfo;

        if( li.LocationKind == LocationKind.Property )
        {
          var propertyInfo = li.PropertyInfo;

          PropertyValidator validator;

          if( !_propertyValidators.TryGetValue( propertyInfo, out validator ) )
          {
            validator = new PropertyValidator( propertyInfo );

            _propertyValidators.Add( propertyInfo, validator );
          }

          if( validator.Add( this ) )
            yield return new AspectInstance( propertyInfo, validator, validator.GetAspectConfiguration( targetElement ) );
        }
        else
          if( li.LocationKind == LocationKind.Parameter )
          {
            var parameterInfo = li.ParameterInfo;

            var methodBase = parameterInfo.Member as MethodBase;

            ParameterValidator validator;

            if( !_parameterValidators.TryGetValue( methodBase, out validator ) )
            {
              validator = new ParameterValidator( methodBase );

              _parameterValidators.Add( methodBase, validator );
            }

            if( validator.Add( parameterInfo, this ) )
              yield return new AspectInstance( methodBase, validator, validator.GetAspectConfiguration( methodBase ) );
          }
      }
    }
    #endregion

    #region Validate
    /// <summary>
    /// При недопустимом значении свойства или параметра выбрасывает исключение.
    /// </summary>
    /// <param name="name">Имя свойства или параметра.</param>
    /// <param name="value">Значение свойства или параметра.</param>
    [DebuggerStepThrough]
    protected void Validate( string name, object value )
    {
      var exception = GetValidationException( value, name );

      if( exception != null )
        throw exception;
    }
    #endregion

    /// <summary>
    /// Класс для внутреннего использования (сделан public из-за требования PostSharp).
    /// </summary>
    // Из методов этого класса нельзя обращаться к элементам, к которым применен PostSharp!
    [PSerializable]
    [ProvideAspectRole( AspectRoles.Validation )]
    [AspectTypeDependency( AspectDependencyAction.Commute, typeof( CacheContextAttribute) )]
    [AspectRoleDependency( AspectDependencyAction.Order, AspectDependencyPosition.After, StandardRoles.Caching )]
    public sealed class ParameterValidator : OnMethodBoundaryAspect  
    {
      #region .Fields
      private Dictionary<ParameterInfo, List<ValidatorAspect>> _validators = new();

      [PNonSerialized]
      private readonly int _validatorsCount;

      [PNonSerialized]
      private int _validatorNumber;
      #endregion

      #region .Ctor
      internal ParameterValidator( MethodBase methodBase )
      {
        _validatorsCount = methodBase.GetParameters().SelectMany( pi => Attribute.GetCustomAttributes( pi, typeof( ValidatorAspect ), true ) ).Count();
      }
      #endregion

      #region Add
      internal bool Add( ParameterInfo parameterInfo, ValidatorAspect validatorAspect )
      {
        List<ValidatorAspect> list;

        if( !_validators.TryGetValue( parameterInfo, out list ) )
        {
          list = new List<ValidatorAspect>();

          _validators.Add( parameterInfo, list );
        }

        list.Add( validatorAspect );

        _validatorNumber++;

        return _validatorNumber == _validatorsCount;
      }
      #endregion

      #region OnEntry
      [DebuggerStepThrough]
      public override void OnEntry( MethodExecutionArgs args )
      {
        foreach( var v in _validators )
          foreach( var va in v.Value )
            va.Validate( v.Key.Name, args.Arguments[v.Key.Position] );
      }
      #endregion
    }

    /// <summary>
    /// Класс для внутреннего использования (сделан public из-за требования PostSharp).
    /// </summary>
    // Из методов этого класса нельзя обращаться к элементам, к которым применен PostSharp!
    [PSerializable]
    [ProvideAspectRole( AspectRoles.Validation )]
    public sealed class PropertyValidator : LocationInterceptionAspect 
    {
      #region .Fields
      private List<ValidatorAspect> _validators = new();

      [PNonSerialized]
      private readonly int _validatorsCount;

      [PNonSerialized]
      private int _validatorNumber;
      #endregion

      #region .Ctor
      internal PropertyValidator( PropertyInfo propertyInfo )
      {
        _validatorsCount = Attribute.GetCustomAttributes( propertyInfo, typeof( ValidatorAspect ), true ).Length;
      }
      #endregion

      #region Add
      internal bool Add( ValidatorAspect validatorAspect )
      {
        _validators.Add( validatorAspect );

        _validatorNumber++;

        return _validatorNumber == _validatorsCount;
      }
      #endregion

      #region OnSetValue
      [DebuggerStepThrough]
      public override void OnSetValue( LocationInterceptionArgs args )
      {
        foreach( var va in _validators )
          va.Validate( args.LocationName, args.Value );

        base.OnSetValue( args );
      }
      #endregion
    }
  }
}
