using PostSharp.Aspects;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Serialization;
using PostSharp.Extensibility;
using PostSharp.Reflection;

using System.ComponentModel;
using System.Reflection;

namespace ST.Utils.Attributes
{
  /// <summary>
  /// Базовый класс для локализованных атрибутов. 
  /// </summary>
  // Из методов этого класса нельзя обращаться к элементам, к которым применен PostSharp!
  [PSerializable]
  [AttributeUsage( AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Enum, AllowMultiple = false, Inherited = true )]
  [MulticastAttributeUsage( MulticastTargets.Class | MulticastTargets.Property | MulticastTargets.Field | MulticastTargets.Enum, AllowMultiple = false, Inheritance = MulticastInheritance.None )]
  [AspectConfiguration( SerializerType = typeof( MsilAspectSerializer ) )]
  public abstract class LocalizationAspect : Aspect, IAspectProvider
  {
    #region .Fields
    private Type _attributeType;

    private string _resourceId;

    private Type _typeConverterType;
    #endregion

    #region .Ctor
    protected LocalizationAspect( Type attributeType, string resourceId, Type typeConverterType = null )
    {
      _attributeType = attributeType;

      _resourceId = resourceId;

      _typeConverterType = typeConverterType;
    }
    #endregion

    #region CompileTimeValidate
    public override bool CompileTimeValidate( object target )
    {
      // Проверка необходима для того, чтобы данный аспект не применялся ко внутренним элементам.
      return (target as MemberInfo).IsDefined( typeof( LocalizationAspect ), true );
    }
    #endregion

    #region GetCustomAttribute
    private static AspectInstance GetCustomAttribute( object target, Type type, object param )
    {
      return new AspectInstance( target, new CustomAttributeIntroductionAspect( new ObjectConstruction( type.GetConstructor( new[] { param.GetType() } ), param ) ) );
    }
    #endregion

    #region ProvideAspects
    public IEnumerable<AspectInstance> ProvideAspects( object targetElement )
    {
      yield return GetCustomAttribute( targetElement, _attributeType, _resourceId );

      if( _typeConverterType != null )
        yield return GetCustomAttribute( targetElement, typeof( TypeConverterAttribute ), _typeConverterType );
    }
    #endregion
  }
}
