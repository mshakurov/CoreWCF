using PostSharp.Aspects;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Serialization;
using PostSharp.Extensibility;
using PostSharp.Reflection;
using PostSharp.Serialization;

using ST.Utils;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTests
{
  [PSerializable]
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
  [MulticastAttributeUsage(MulticastTargets.Class, AllowMultiple = false, Inheritance = MulticastInheritance.None, PersistMetaData = true)]
  [AspectConfiguration(SerializerType = typeof(MsilAspectSerializer))]
  public class AutoDataContractAttribute : Aspect, IAspectProvider
  {
    public override bool CompileTimeValidate( object target )
    {
      //AspectHelper.Fail(1, "CompileTimeValidate('{0}')", target);

      return target.GetAttribute<DataContractAttribute>(true) == null ? true : AspectHelper.Fail(1, "Attribute WcfServerAttribute can't be applied to type '{0}'. The type must not be marked with ServiceBehaviorAttribute.", (target as Type).FullName);
    }


    public IEnumerable<AspectInstance> ProvideAspects( object targetElement )
    {
      CustomAttributeIntroductionAspect introduceDataContractAspect =
            new CustomAttributeIntroductionAspect(
                new ObjectConstruction(typeof(DataContractAttribute).GetConstructor(Type.EmptyTypes)));

      yield return new AspectInstance(targetElement, introduceDataContractAspect);
    }
  }


  [AutoDataContract]
  //[DataContract]
  public class AutoDataContractClass
  {
    public int MyProperty { get; set; }
  }

}
