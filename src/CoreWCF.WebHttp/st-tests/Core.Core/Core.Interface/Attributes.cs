using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using ST.Utils;
using ST.Utils.Attributes;
using PostSharp.Aspects;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Serialization;
using PostSharp.Extensibility;
using PostSharp.Reflection;
using PostSharp.Serialization;
using CoreWCF;

namespace ST.Core
{
  /// <summary>
  /// Атрибут, указывающий, что сборка к которой он применен, содержит типы, относящиеся к функционированию модулей и ядра.
  /// Только в сборках помеченных данным атрибутом сервер будет искать классы модулей, типы сообщений, разрешения доступа и т.п.
  /// </summary>
  [AttributeUsage( AttributeTargets.Assembly, AllowMultiple = false, Inherited = false )]
  public sealed class PlatformAssemblyAttribute : Attribute
  {
  }

  /// <summary>
  /// Атрибут задает для модуля приоритет загрузки сервером.
  /// </summary>
  [AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = false )]
  public sealed class ModuleLoadingPriorityAttribute : Attribute
  {
    #region .Static Fields
    internal static readonly ModuleLoadingPriorityAttribute Default = new ModuleLoadingPriorityAttribute( ModuleLoadingPriority.Normal );
    #endregion

    #region .Properties
    /// <summary>
    /// Приоритет загрузки.
    /// </summary>
    public byte Priority { get; private set; }
    #endregion

    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="priority">Приоритет загрузки. Чем больше значение, тем больший приоритет загрузки.</param>
    public ModuleLoadingPriorityAttribute( byte priority )
    {
      Priority = priority;
    }

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="priority">Предустановленный приоритет загрузки.</param>
    public ModuleLoadingPriorityAttribute( ModuleLoadingPriority priority )
    {
      Priority = (byte) priority;
    }
    #endregion
  }

  /// <summary>
  /// Атрибут, описывающий конфигурацию модуля, которая может настраиваться пользователем.
  /// </summary>
  [AttributeUsage( AttributeTargets.Class, AllowMultiple = false, Inherited = true )]
  public class ConfigurableAttribute : Attribute
  {
    #region .Properties
    /// <summary>
    /// Тип, содержащий конфигурацию модуля.
    /// </summary>
    public Type ConfigType { get; private set; }
    #endregion

    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="type">Тип, содержащий конфигурацию модуля. Должен быть унаследован от ModuleConfig и иметь открытый конструктор без параметров.</param>
    public ConfigurableAttribute( [InheritedFrom( typeof( ModuleConfig ) )] Type type )
    {
      if( type.GetConstructor( BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, new ParameterModifier[0] ) == null )
        throw new ArgumentException( "Type should have a public instance parameterless constructor." );

      ConfigType = type;
    }
    #endregion
  }

  /// <summary>
  /// Интерфейс модуля (интерфейс, реализуемый модулем), к которому применен данный атрибут, не будет доступен другим модулям при попытке получить его через сервер.
  /// </summary>
  [AttributeUsage( AttributeTargets.Interface, AllowMultiple = false, Inherited = false )]
  public sealed class NotServiceInterfaceAttribute : Attribute
  {
  }

  /// <summary>
  /// Интерфейс сервера (интерфейс, реализуемый сервером), к которому применен данный атрибут, будет доступен модулям.
  /// Интерфейсы сервера не помеченные данным атрибутом модулям доступны не будут.
  /// </summary>
  [AttributeUsage( AttributeTargets.Interface, AllowMultiple = false, Inherited = false )]
  public sealed class ServerInterfaceAttribute : Attribute
  {
  }

  /// <summary>
  /// Класс предназначен для внутреннего использования.
  /// </summary>
  [Serializable]
  [AttributeUsage( AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = true )]
  [MulticastAttributeUsage( MulticastTargets.Property | MulticastTargets.Parameter | MulticastTargets.ReturnValue, Inheritance = MulticastInheritance.None, PersistMetaData = true )]
  [AspectConfiguration( SerializerType = typeof( MsilAspectSerializer ) )]
  public abstract class ContractElementAttribute : Aspect
  {
    #region CompileTimeValidate
    public override bool CompileTimeValidate( object target )
    {
      var propInfo = target as PropertyInfo;
      var paramInfo = target as ParameterInfo;

      var name = propInfo != null ? "property '" + propInfo.Name + "'" : (string.IsNullOrWhiteSpace( paramInfo.Name ) ? "return value" : "parameter '" + paramInfo.Name + "'");
      var declaringName = propInfo != null ? "of the type '" + propInfo.DeclaringType.FullName + "'": "of the method '" + paramInfo.Member.Name + "'";

      var type = propInfo != null ? propInfo.PropertyType : paramInfo.ParameterType;

      if( type.IsArray )
        type = type.GetElementType() ?? type;

      if( (propInfo == null || (propInfo.IsDefined( typeof( DataMemberAttribute ), true ))) && IsValidType( type ) )
        return true;

      return AspectHelper.Fail( 1, "Attribute '" + GetType().Name + "' can't be applied to the " + name + " " + declaringName + "." );
    }
    #endregion

    #region IsValidType
    protected virtual bool IsValidType( Type type )
    {
      return type.IsClass && type.IsDefined( typeof( DataContractAttribute ), true );
    }
    #endregion
  }

  /// <summary>
  /// Атрибут указывает, что помеченный им элемент не будет подвергаться UTC-преобразованию.
  /// </summary>
  public class SkipTimeConvertationAttribute : ContractElementAttribute
  {
    #region IsDateTime
    protected bool IsDateTime( Type type )
    {
      return type == typeof( DateTime ) || type == typeof( DateTime? );
    }
    #endregion

    #region IsValidType
    protected override bool IsValidType( Type type )
    {
      return base.IsValidType( type ) || IsDateTime( type );
    }
    #endregion
  }

  /// <summary>
  /// Атрибут указывает, что помеченный им элемент может оперировать объектами, тип которых унаследован от типа элемента и содержит свойства типа DateTime(?).
  /// Используется для того, чтобы UTC-преобразование выполнялось для объектов, которыми элементы оперируют неявно.
  /// </summary>
  public sealed class HasInheritedDateTimesAttribute : ContractElementAttribute
  {
  }

  /// <summary>
  /// Атрибут указывает, что помеченный им элемент типа DateTime(?) будет содержать только дату и не должен подвергаться UTC-преобразованию.
  /// </summary>
  public sealed class DateOnlyAttribute : SkipTimeConvertationAttribute
  {
    #region IsValidType
    protected override bool IsValidType( Type type )
    {
      return IsDateTime( type );
    }
    #endregion
  }

  /// <summary>
  /// Атрибут, указывающий, что помеченный им класс является WCF-сервером.
  /// </summary>
  // Из методов этого класса нельзя обращаться к элементам, к которым применен PostSharp!
  [PSerializable]
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
  [MulticastAttributeUsage(MulticastTargets.Class, AllowMultiple = false, Inheritance = MulticastInheritance.None, PersistMetaData = true)]
  [AspectConfiguration(SerializerType = typeof(MsilAspectSerializer))]
  public sealed class WcfServerAttribute : Aspect, IAspectProvider
  {
    #region .Properties
    /// <summary>
    /// Относительный адрес сервера.
    /// </summary>
    public string Address { get; private set; }

    /// <summary>
    /// Пространство имен сервера.
    /// </summary>
    public string Namespace { get; private set; }
    #endregion

    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="address">Относительный адрес сервера.</param>
    /// <param name="nameSpace">Пространство имен сервера.</param>
    public WcfServerAttribute( string address, string nameSpace )
    {
      Address = address;
      Namespace = nameSpace;
    }
    #endregion

    #region CompileTimeValidate
    public override bool CompileTimeValidate( object target )
    {
      return target.GetAttribute<ServiceBehaviorAttribute>(true) == null ? true : AspectHelper.Fail(1, "Attribute WcfServerAttribute can't be applied to type '{0}'. The type must not be marked with ServiceBehaviorAttribute.", (target as Type).FullName);
    }
    #endregion

    #region ProvideAspects
    public IEnumerable<AspectInstance> ProvideAspects( object targetElement )
    {
      yield return new AspectInstance(targetElement, new CustomAttributeIntroductionAspect(new ServiceBehaviorCustomAttributeData { Address = Address, Namespace = Namespace }));
    }
    #endregion

    private sealed class ServiceBehaviorCustomAttributeData : CustomAttributeData
    {
      #region .Static Fields
      private static readonly PropertyInfo _instanceContextMode = MemberHelper.GetProperty(( ServiceBehaviorAttribute obj ) => obj.InstanceContextMode);
      private static readonly PropertyInfo _concurrencyMode = MemberHelper.GetProperty(( ServiceBehaviorAttribute obj ) => obj.ConcurrencyMode);
      //private static readonly PropertyInfo _ignoreExtensionDataObject = MemberHelper.GetProperty( (ServiceBehaviorAttribute obj) => obj.IgnoreExtensionDataObject );
      private static readonly PropertyInfo _namespace = MemberHelper.GetProperty(( ServiceBehaviorAttribute obj ) => obj.Namespace);
      private static readonly PropertyInfo _name = MemberHelper.GetProperty(( ServiceBehaviorAttribute obj ) => obj.Name);

      private static readonly ConstructorInfo _constructor = typeof(ServiceBehaviorAttribute).GetConstructor(Type.EmptyTypes);
      #endregion

      #region .Fields
      public string Address;
      public string Namespace;
      #endregion

      #region .Properties
      public override ConstructorInfo Constructor
      {
        get { return _constructor; }
      }

      public override IList<CustomAttributeTypedArgument> ConstructorArguments
      {
        get { return new CustomAttributeTypedArgument[0]; }
      }

      public override IList<CustomAttributeNamedArgument> NamedArguments
      {
        get
        {
          return new CustomAttributeNamedArgument[]
          {
            new CustomAttributeNamedArgument( _instanceContextMode, InstanceContextMode.Single ),
            new CustomAttributeNamedArgument( _concurrencyMode, ConcurrencyMode.Multiple ),
            //new CustomAttributeNamedArgument( _ignoreExtensionDataObject, true ),
            new CustomAttributeNamedArgument( _namespace, Namespace ),
            new CustomAttributeNamedArgument( _name, Address )
          };
        }
      }
      #endregion
    }
  }
  /// <summary>
  /// Атрибут, указывающий, что помеченный им интерфейс является WCF-сервисом.
  /// </summary>
  // Из методов этого класса нельзя обращаться к элементам, к которым применен PostSharp!
  [Serializable]
  [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
  [MulticastAttributeUsage(MulticastTargets.Interface, AllowMultiple = false, Inheritance = MulticastInheritance.None, PersistMetaData = true)]
  [AspectConfiguration(SerializerType = typeof(MsilAspectSerializer))]
  public sealed class WcfServiceAttribute : Aspect, IAspectProvider
  {
    #region .Properties
    /// <summary>
    /// Относительный адрес сервера, на котором размещается сервис.
    /// </summary>
    public string Address { get; private set; }

    /// <summary>
    /// Пространство имен сервиса.
    /// </summary>
    public string Namespace { get; private set; }
    #endregion

    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="address">Относительный адрес сервера, на котором размещается сервис.</param>
    /// <param name="nameSpace">Пространство имен сервиса.</param>
    public WcfServiceAttribute( string address, string nameSpace )
    {
      Address = address;
      Namespace = nameSpace;
    }
    #endregion

    #region CompileTimeValidate
    public override bool CompileTimeValidate( object target )
    {
      var type = target as Type;

      if (type.GetAttribute<ServiceContractAttribute>(true) != null)
        return AspectHelper.Fail(1, "Attribute WcfServerAttribute can't be applied to type '{0}'. The type must not be marked with ServiceContractAttribute.", type.FullName);

      foreach (var mi in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
        if (mi.GetAttribute<OperationContractAttribute>(true) != null)
          return AspectHelper.Fail(2, "Attribute WcfServerAttribute can't be applied to type '{0}'. The methods of the type must not be marked with OperationContractAttribute.", type.FullName);
      return true;
    }
    #endregion

    #region ProvideAspects
    public IEnumerable<AspectInstance> ProvideAspects( object targetElement )
    {
      yield return new AspectInstance(targetElement, new CustomAttributeIntroductionAspect(new ServiceContractCustomAttributeData { Namespace = Namespace }));

      foreach (var mi in (targetElement as Type).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
        yield return new AspectInstance(mi, mi.IsDefined<OneWayAttribute>() ? new CustomAttributeIntroductionAspect(new OperationContractCustomAttributeData()) :
                                                                               new CustomAttributeIntroductionAspect(new ObjectConstruction(OperationContractCustomAttributeData.ConstructorInfo)));
    }
    #endregion

    private sealed class ServiceContractCustomAttributeData : CustomAttributeData
    {
      #region .Static Fields
      private static readonly PropertyInfo _namespace = MemberHelper.GetProperty(( ServiceContractAttribute obj ) => obj.Namespace);

      private static readonly ConstructorInfo _constructor = typeof(ServiceContractAttribute).GetConstructor(Type.EmptyTypes);
      #endregion

      #region .Fields
      public string Namespace;
      #endregion

      #region .Properties
      public override ConstructorInfo Constructor
      {
        get { return _constructor; }
      }

      public override IList<CustomAttributeTypedArgument> ConstructorArguments
      {
        get { return new CustomAttributeTypedArgument[0]; }
      }

      public override IList<CustomAttributeNamedArgument> NamedArguments
      {
        get { return new CustomAttributeNamedArgument[] { new CustomAttributeNamedArgument(_namespace, Namespace) }; }
      }
      #endregion
    }

    private sealed class OperationContractCustomAttributeData : CustomAttributeData
    {
      #region .Static Fields
      private static readonly PropertyInfo _isOneWay = MemberHelper.GetProperty(( OperationContractAttribute obj ) => obj.IsOneWay);

      public static readonly ConstructorInfo ConstructorInfo = typeof(OperationContractAttribute).GetConstructor(Type.EmptyTypes);
      #endregion

      #region .Properties
      public override ConstructorInfo Constructor
      {
        get { return ConstructorInfo; }
      }

      public override IList<CustomAttributeTypedArgument> ConstructorArguments
      {
        get { return new CustomAttributeTypedArgument[0]; }
      }

      public override IList<CustomAttributeNamedArgument> NamedArguments
      {
        get { return new CustomAttributeNamedArgument[] { new CustomAttributeNamedArgument(_isOneWay, true) }; }
      }
      #endregion
    }
  }

  /// <summary>
  /// Атрибут, указывающий, что методы, помеченного им интерфейса, будут помечены NotLoggedOnFault.
  /// </summary>
  // Из методов этого класса нельзя обращаться к элементам, к которым применен PostSharp!
  [Serializable]
  [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
  [MulticastAttributeUsage(MulticastTargets.Interface, AllowMultiple = false, Inheritance = MulticastInheritance.None, PersistMetaData = true)]
  [AspectConfiguration(SerializerType = typeof(MsilAspectSerializer))]
  public sealed class LogonFaultAttribute : Aspect, IAspectProvider
  {
    #region .Ctor
    public LogonFaultAttribute()
    {
    }
    #endregion

    #region CompileTimeValidate
    public override bool CompileTimeValidate( object target )
    {
      var type = target as Type;

      foreach (var mi in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
      {
        var attributes = mi.GetAttributes<FaultContractAttribute>(true);

        if (attributes != null && attributes.Where(attr => attr.DetailType == typeof(NotLoggedOnFault)).ToArray().Length > 0)
          return AspectHelper.Fail(2, "Attribute LogonFaultAttribute can't be applied to type '{0}'. The methods of the type must not be marked with FaultContractAttribute.", type.FullName);
      }

      return true;
    }
    #endregion

    #region ProvideAspects
    public IEnumerable<AspectInstance> ProvideAspects( object targetElement )
    {
      foreach (var mi in (targetElement as Type).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
      {
        if (!mi.IsDefined<OneWayAttribute>())
          yield return new AspectInstance(mi, new CustomAttributeIntroductionAspect(new ObjectConstruction(FaultContractCustomAttributeData.ConstructorInfo, typeof(NotLoggedOnFault))));
      }
    }
    #endregion

    private sealed class FaultContractCustomAttributeData : CustomAttributeData
    {
      #region .Static Fields
      public static readonly ConstructorInfo ConstructorInfo = typeof(FaultContractAttribute).GetConstructor(new Type[1] { typeof(Type) });
      #endregion

      #region .Properties
      public override ConstructorInfo Constructor
      {
        get { return ConstructorInfo; }
      }

      public override IList<CustomAttributeTypedArgument> ConstructorArguments
      {
        get { return new CustomAttributeTypedArgument[0]; }
      }
      #endregion
    }
  }

  /// <summary>
  /// Атрибут, указывающий на то, что метод WCF-сервиса не возвращает значений.
  /// </summary>
  [Serializable]
  [AttributeUsage( AttributeTargets.Method, AllowMultiple = false )]
  public sealed class OneWayAttribute : Attribute
  {
  }

  /// <summary>
  /// Только для внутреннего использования.
  /// </summary>
  // Из методов этого класса нельзя обращаться к элементам, к которым применен PostSharp!
  [Serializable]
  [AttributeUsage( AttributeTargets.Property, AllowMultiple = false )]
  [MulticastAttributeUsage( MulticastTargets.Property, AllowMultiple = false, Inheritance = MulticastInheritance.None )]
  [LocationInterceptionAspectConfiguration( SerializerType = typeof( MsilAspectSerializer ) )]
  public abstract class ImportServiceBaseAttribute : LocationInterceptionAspect
  {
    #region .Fields
    [NonSerialized]
    private bool _imported;
    #endregion

    #region CompileTimeValidate
    public override bool CompileTimeValidate( LocationInfo locationInfo )
    {
      if( !typeof( BaseModule ).IsAssignableFrom( locationInfo.PropertyInfo.DeclaringType ) )
        return AspectHelper.Fail( 1, "Attribute ImportServiceAttribute can't be applied to property '{0}'. The declaring type must be inherited from the type 'ST.Core.BaseModule'.", locationInfo.PropertyInfo.Name );

      if( !locationInfo.PropertyInfo.PropertyType.IsInterface )
        return AspectHelper.Fail( 2, "Attribute ImportServiceAttribute can't be applied to property '{0}' of type '{1}'. Property type is not an interface.", locationInfo.PropertyInfo.Name, locationInfo.DeclaringType.FullName );

      if( locationInfo.IsStatic )
        return AspectHelper.Fail( 3, "Attribute ImportServiceAttribute can't be applied to property '{0}' of type '{1}'. The property is static.", locationInfo.PropertyInfo.Name, locationInfo.DeclaringType.FullName );

      var m = locationInfo.PropertyInfo.GetGetMethod( true );

      if( m == null || m.GetAttribute<CompilerGeneratedAttribute>( true ) == null ||
          (m = locationInfo.PropertyInfo.GetSetMethod( true )) == null || m.GetAttribute<CompilerGeneratedAttribute>( true ) == null )
        return AspectHelper.Fail( 4, "Attribute ImportServiceAttribute can't be applied to property '{0}' of type '{1}'. The property is not automatic.", locationInfo.PropertyInfo.Name, locationInfo.DeclaringType.FullName );

      return true;
    }
    #endregion

    #region GetService
    protected abstract object GetService( Type serviceType, BaseModule module );
    #endregion

    #region OnGetValue
    public override sealed void OnGetValue( LocationInterceptionArgs args )
    {
      if( !_imported )
      {
        args.SetNewValue( GetService( args.Location.PropertyInfo.PropertyType, args.Instance as BaseModule ) );

        _imported = true;
      }

      args.Value = args.GetCurrentValue();
    }
    #endregion

    #region OnSetValue
    public override sealed void OnSetValue( LocationInterceptionArgs args )
    {
      if( args.Value == null )
      {
        args.SetNewValue( null );

        _imported = false;
      }
    }
    #endregion
  }

  /// <summary>
  /// Атрибут, позволяющий импортировать в помеченное им экземплярное автоматическое свойство сервис, имеющий тип данного свойства (тип должен быть интерфейсом).
  /// </summary>
  // Из методов этого класса нельзя обращаться к элементам, к которым применен PostSharp!
  [Serializable]
  public sealed class ImportServiceAttribute : ImportServiceBaseAttribute
  {
    #region .Static Fields
    private static readonly MethodInfo _getService = MemberHelper.GetMethod( (BaseModule obj) => obj.GetService<string>() ).GetGenericMethodDefinition();
    #endregion

    #region GetService
    protected override object GetService( Type serviceType, BaseModule module )
    {
      return _getService.MakeGenericMethod( serviceType ).Invoke( module, null );
    }
    #endregion
  }

  /// <summary>
  /// Атрибут, позволяющий импортировать в помеченное им экземплярное автоматическое свойство WCF-сервис, имеющий тип данного свойства (тип должен быть интерфейсом и этот интерфейс должен быть помечен атрибутом WcfServiceAttribute).
  /// </summary>
  // Из методов этого класса нельзя обращаться к элементам, к которым применен PostSharp!
  [Serializable]
  public sealed class ImportWcfServiceAttribute : ImportServiceBaseAttribute
  {
    #region .Static Fields
    private static readonly MethodInfo _getService = MemberHelper.GetMethod(( WcfModule obj ) => obj.GetWcfService<string>(null, null)).GetGenericMethodDefinition();
    #endregion

    #region CompileTimeValidate
    public override bool CompileTimeValidate( LocationInfo locationInfo )
    {
      if (!base.CompileTimeValidate(locationInfo))
        return false;

      if (!typeof(WcfModule).IsAssignableFrom(locationInfo.PropertyInfo.DeclaringType))
        return AspectHelper.Fail(5, "Attribute ImportWcfServiceAttribute can't be applied to property '{0}'. The declaring type must be inherited from the type 'ST.Core.WcfModule'.", locationInfo.PropertyInfo.Name);

      if (locationInfo.PropertyInfo.PropertyType.GetAttribute<WcfServiceAttribute>(true) == null)
        return AspectHelper.Fail(6, "Attribute ImportWcfServiceAttribute can't be applied to property '{0}' of type '{1}'. Required interface is not marked with WcfServiceAttribute.", locationInfo.PropertyInfo.Name, locationInfo.DeclaringType.FullName);

      return true;
    }
    #endregion

    #region GetService
    protected override object GetService( Type serviceType, BaseModule module )
    {
      if (module is WcfModule)
      {
        var info = serviceType.GetAttribute<WcfServiceAttribute>(true);

        return _getService.MakeGenericMethod(serviceType).Invoke(module, new[] { info.Address, info.Namespace });
      }

      return null;
    }
    #endregion
  }
}
