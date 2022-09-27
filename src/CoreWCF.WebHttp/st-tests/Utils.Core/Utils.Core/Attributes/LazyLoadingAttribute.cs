using PostSharp.Aspects;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Serialization;
using PostSharp.Extensibility;
using PostSharp.Reflection;

using System.Collections.Concurrent;

namespace ST.Utils.Attributes
{
  /// <summary>
  /// Атрибут реализует отложенную загрузку данных для помеченного свойства.
  /// Свойство должно быть ссылочного типа. Если тип переопределяет GetHashCode, то этот метод должен
  /// возвращать уникальные значения для каждого экземпляра, существующего на данный момент времени.
  /// </summary>
  [PSerializable]
  [AttributeUsage( AttributeTargets.Property, AllowMultiple = false )]
  [MulticastAttributeUsage( MulticastTargets.Property, AllowMultiple = false, Inheritance = MulticastInheritance.None )]
  [LocationInterceptionAspectConfiguration( SerializerType = typeof( MsilAspectSerializer ) )]
  public sealed class LazyLoadingAttribute : LocationInterceptionAspect
  {
    // !!! Необходим механизм очистки кэша.
    // Возможно, необходим универсальный механизм очистки списков, содержащих WeakReference.

    #region .Fields
    private ConcurrentDictionary<int, WeakReference> _values;
    #endregion

    #region CompileTimeValidate
    public override bool CompileTimeValidate( LocationInfo locationInfo )
    {
      return locationInfo.PropertyInfo.PropertyType.IsClass || AspectHelper.Fail( 1, "Attribute LazyLoadAttribute can't be applied to property '{0}' of type '{1}'.", locationInfo.PropertyInfo.Name, locationInfo.DeclaringType.Name );
    }
    #endregion

    #region OnGetValue
    public override void OnGetValue( LocationInterceptionArgs args )
    {
      int hashCode = args.Instance.GetHashCode();

      var reference = _values.GetOrAdd( hashCode, hc =>
      {
        args.ProceedGetValue();

        return args.Value == null ? null : new WeakReference( args.Value, false );
      } );

      if( reference == null || (args.Value = reference.Target) == null )
      {
        _values.TryRemove( hashCode, out reference );

        if( reference != null )
          OnGetValue( args );
      }
    }
    #endregion

    #region OnSetValue
    public override void OnSetValue( LocationInterceptionArgs args )
    {
      if( args.Value == null )
      {
        WeakReference reference;

        _values.TryRemove( args.Instance.GetHashCode(), out reference );

        args.SetNewValue( null );
      }
      else
        args.ProceedSetValue();
    }
    #endregion

    #region RuntimeInitialize
    public override void RuntimeInitialize( LocationInfo locationInfo )
    {
      _values = new ConcurrentDictionary<int, WeakReference>();
    }
    #endregion
  }
}
