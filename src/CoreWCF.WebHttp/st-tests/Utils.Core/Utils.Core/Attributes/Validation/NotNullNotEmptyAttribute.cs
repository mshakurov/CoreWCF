using PostSharp.Serialization;

using System;
using System.Collections;
using System.Diagnostics;

namespace ST.Utils.Attributes
{
  /// <summary>
  /// Валидатор, гарантирующий невозможность нулового или пустого значения параметра/свойства. Применим только к строкам и коллекциям.
  /// При попытке присвоить Null параметру/свойству выбрасывается исключение ArgumentNullException.
  /// При попытке присвоить параметру/свойству пустой строки или коллекции выбрасывается исключение ArgumentException.
  /// </summary>
  [PSerializable]
  public class NotNullNotEmptyAttribute : NotNullAttribute
  {
    #region GetValidationException
    [DebuggerStepThrough]
    protected override Exception GetValidationException( object value, string name )
    {
      var exception = base.GetValidationException( value, name );

      if( exception != null )
        return exception;

      if( value is string str && string.IsNullOrWhiteSpace( str ) )
        return new ArgumentException( "The string can't be empty.", name );

      if( value is ICollection coll && coll.Count == 0 )
        return new ArgumentException( "The collection can't be empty.", name );

      return null;
    }
    #endregion
  }
}
