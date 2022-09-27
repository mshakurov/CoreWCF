using PostSharp.Aspects;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Serialization;
using PostSharp.Extensibility;
using PostSharp.Reflection;

using ST.Utils.Threading;

namespace ST.Utils.Attributes
{
  /// <summary>
  /// Статическое поле, помеченное данным атрибутом будет захватываться в контекст методом Capture класса ThreadStaticContext.
  /// Статическое поле должно быть помечено атрибутом ThreadStaticAttribute.
  /// </summary>
  [PSerializable]
  [AttributeUsage( AttributeTargets.Field, AllowMultiple = false, Inherited = false )]
  [MulticastAttributeUsage( MulticastTargets.Field, TargetMemberAttributes = MulticastAttributes.Static, AllowMultiple = false, Inheritance = MulticastInheritance.None )]
  [AspectConfiguration( SerializerType = typeof( MsilAspectSerializer ) )]
  public sealed class ThreadStaticContextAttribute : LocationLevelAspect
  {
    #region CompileTimeValidate
    public override bool CompileTimeValidate( LocationInfo locationInfo )
    {
      return locationInfo.FieldInfo.IsDefined( typeof( ThreadStaticAttribute ), false );
    }
    #endregion

    #region RuntimeInitialize
    public override void RuntimeInitialize( LocationInfo locationInfo )
    {
      ThreadStaticContext.Register( locationInfo.FieldInfo );
    }
    #endregion
  }
}
