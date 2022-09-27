using System.Diagnostics;

namespace ST.Utils.Attributes
{
  /// <summary>
  /// Валидатор, гарантирующий невозможность нулового значения параметра/свойства.
  /// При попытке присвоить Null параметру/свойству выбрасывается исключение ArgumentNullException.
  /// </summary>
  [PSerializable]
  public class NotNullAttribute : ValidatorAspect
  {
    #region GetValidationException
    [DebuggerStepThrough]
    protected override Exception GetValidationException( object value, string name )
    {
      return value == null ? new ArgumentNullException( name ) : null;
    }
    #endregion
  }
}
