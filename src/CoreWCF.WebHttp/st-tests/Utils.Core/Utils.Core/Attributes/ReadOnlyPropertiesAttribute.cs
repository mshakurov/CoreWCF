using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Dependencies;
using PostSharp.Aspects.Serialization;
using PostSharp.Extensibility;
using PostSharp.Reflection;

using System.Diagnostics;

namespace ST.Utils.Attributes
{
  /// <summary>
  /// Атрибут реализует интерфейс IReadOnlyProperties у помеченного типа и всех его потомков.
  /// </summary>
  [PSerializable]
  [AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = true )]
  [MulticastAttributeUsage( MulticastTargets.Class, AllowMultiple = false, Inheritance = MulticastInheritance.Strict )]
  [AspectConfiguration( SerializerType = typeof( MsilAspectSerializer ) )]
  [IntroduceInterface( typeof( IReadOnlyProperties ), OverrideAction = InterfaceOverrideAction.Ignore )]
  [AspectRoleDependency( AspectDependencyAction.Order, AspectDependencyPosition.Before, AspectRoles.DataBinding )]
  [AspectRoleDependency( AspectDependencyAction.Order, AspectDependencyPosition.Before, AspectRoles.Modification )]
  [AspectRoleDependency( AspectDependencyAction.Order, AspectDependencyPosition.Before, AspectRoles.Persistence )]
  [AspectRoleDependency( AspectDependencyAction.Order, AspectDependencyPosition.Before, AspectRoles.Validation )]
  [AspectRoleDependency( AspectDependencyAction.Order, AspectDependencyPosition.After, AspectRoles.Security )]
  public sealed class ReadOnlyPropertiesAttribute : InstanceLevelAspect, IReadOnlyProperties
  {
    #region .Fields
    [PNonSerialized]
    private bool _arePropertiesReadOnly;

    [ImportMember( "ArePropertiesReadOnly", IsRequired = true, Order = ImportMemberOrder.AfterIntroductions )]
    public Property<bool> ArePropertiesReadOnlyProperty;
    #endregion

    #region .Properties
    [IntroduceMember( Visibility = Visibility.Family, OverrideAction = MemberOverrideAction.Ignore )]
    public bool ArePropertiesReadOnly
    {
      get { return _arePropertiesReadOnly; }
    }
    #endregion

    #region MakePropertiesAsReadOnly
    public void MakePropertiesAsReadOnly()
    {
      _arePropertiesReadOnly = true;
    }
    #endregion

    #region OnPropertySet
    [DebuggerStepThrough]
    [OnLocationSetValueAdvice, MulticastPointcut( Targets = MulticastTargets.Property, Attributes = MulticastAttributes.NonAbstract )]
    public void OnPropertySet( LocationInterceptionArgs args )
    {
      if( !ArePropertiesReadOnlyProperty.Get() )
        args.ProceedSetValue();
    }
    #endregion
  }

  /// <summary>
  /// Интерфейс, предоставляющий возможность запрещать устанавливать свойства объекта.
  /// </summary>
  public interface IReadOnlyProperties
  {
    /// <summary>
    /// Признак того, что свойства объекта доступны только для чтения.
    /// </summary>
    bool ArePropertiesReadOnly { get; }

    /// <summary>
    /// Делает свойства объекта доступными только на чтение.
    /// </summary>
    void MakePropertiesAsReadOnly();
  }
}
