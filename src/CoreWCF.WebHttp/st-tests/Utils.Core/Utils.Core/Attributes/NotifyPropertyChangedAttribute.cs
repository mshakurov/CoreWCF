using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Dependencies;
using PostSharp.Aspects.Serialization;
using PostSharp.Extensibility;
using PostSharp.Reflection;

using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace ST.Utils.Attributes
{
  /// <summary>
  /// Атрибут реализует интерфейс INotifyPropertyChanged и IChangedPropertiesInfo у помеченного типа и всех его потомков.
  /// </summary>
  [PSerializable]
  [AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = true )]
  [MulticastAttributeUsage( MulticastTargets.Class, AllowMultiple = false, Inheritance = MulticastInheritance.Strict )]
  [AspectConfiguration( SerializerType = typeof( MsilAspectSerializer ) )]
  [IntroduceInterface( typeof( INotifyPropertyChanged ), OverrideAction = InterfaceOverrideAction.Ignore )]
  [IntroduceInterface( typeof( IChangedPropertiesInfo ), OverrideAction = InterfaceOverrideAction.Ignore )]
  [AspectRoleDependency( AspectDependencyAction.Order, AspectDependencyPosition.After, AspectRoles.Modification )]
  [AspectRoleDependency( AspectDependencyAction.Order, AspectDependencyPosition.After, AspectRoles.Validation )]
  [ProvideAspectRole( AspectRoles.DataBinding )]
  public sealed class NotifyPropertyChangedAttribute : InstanceLevelAspect, INotifyPropertyChanged, IChangedPropertiesInfo
  {
    #region .Fields
    // Поля ссылочного типа нельзя инициализировать через конструктор, т.к. аспекты создаются в разрезе типов по шаблону "прототип"
    [PNonSerialized]
    private HashSet<string> _changedProperties;

    [ImportMember( "OnPropertyChanged", IsRequired = true, Order = ImportMemberOrder.AfterIntroductions )]
    public Action<string> OnPropertyChangedMethod;
    #endregion

    #region .Properties
    private HashSet<string> ChangedPropertiesImpl
    {
      get { return _changedProperties ?? (_changedProperties = new HashSet<string>()); }
    }

    public string[] ChangedProperties
    {
      get { return ChangedPropertiesImpl.ToArray(); }
    }
    #endregion

    #region .Events
    public event PropertyChangedEventHandler PropertyChanged;
    #endregion

    #region ClearChangedProperties
    public void ClearChangedProperties()
    {
      ChangedPropertiesImpl.Clear();
    }
    #endregion

    #region OnPropertyChanged
    [DebuggerStepThrough]
    [IntroduceMember( Visibility = Visibility.Family, OverrideAction = MemberOverrideAction.Ignore )]
    public void OnPropertyChanged( string propertyName )
    {
      ChangedPropertiesImpl.Add( propertyName );

      PropertyChanged.IfNotNull( h => h( Instance, new PropertyChangedEventArgs( propertyName ) ) );
    }
    #endregion

    #region OnPropertySet
    [DebuggerStepThrough]
    [OnLocationSetValueAdvice, MethodPointcut( "SelectProperties" )]
    public void OnPropertySet( LocationInterceptionArgs args )
    {
      if( !object.Equals( args.Value, args.GetCurrentValue() ) )
      {
        args.ProceedSetValue();

        OnPropertyChangedMethod( args.Location.Name );
      }
    }
    #endregion

    #region SelectProperties
    private IEnumerable<PropertyInfo> SelectProperties( Type type )
    {
      return from property in type.GetProperties( BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public )
             where property.CanWrite && !property.GetSetMethod( true ).IsAbstract && !property.IsDefined( typeof( IgnorePropertyChangingAttribute ), true )
             select property;
    }
    #endregion
  }

  /// <summary>
  /// Свойство, помеченное данным атрибутом, будет игнорироваться при использовании атрибута NotifyPropertyChangedAttribute.
  /// </summary>
  [PSerializable]
  [AttributeUsage( AttributeTargets.Property )]
  public sealed class IgnorePropertyChangingAttribute : Attribute
  {
  }

  /// <summary>
  /// Интерфейс, позволяющий получать информацию об измененных свойствах.
  /// </summary>
  public interface IChangedPropertiesInfo
  {
    string[] ChangedProperties { get; }

    void ClearChangedProperties();
  }
}
