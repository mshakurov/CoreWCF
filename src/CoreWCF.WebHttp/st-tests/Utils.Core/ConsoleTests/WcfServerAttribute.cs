using CoreWCF;

using PostSharp.Aspects;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Serialization;
using PostSharp.Extensibility;
using PostSharp.Serialization;

using ST.Utils;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTests
{
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
      //AspectHelper.Fail(1, "CompileTimeValidate('{0}')", target);

      return target.GetAttribute<ServiceBehaviorAttribute>(true) == null ? true : AspectHelper.Fail(1, "Attribute WcfServerAttribute can't be applied to type '{0}'. The type must not be marked with ServiceBehaviorAttribute.", (target as Type).FullName);
    }
    #endregion

    #region ProvideAspects
    public IEnumerable<AspectInstance> ProvideAspects( object targetElement )
    {
      //AspectHelper.Fail(1, "ProvideAspects('{0}')", targetElement);

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

  [WcfServer("address", "nameSpace")]
  public class WcfServer
  {
    public int MyProperty { get; set; }
  }
}
