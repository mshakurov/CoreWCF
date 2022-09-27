using System;

namespace ST.Utils.TypeConverters
{
  /// <summary>
  /// Преобразователь перечислений на основе атрибута DisplayNameAttribute.
  /// </summary>
  public class DisplayNameEnumConverter : BaseEnumConverter<string>
  {
    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="type">Тип перечисления.</param>
    public DisplayNameEnumConverter( Type type ) : base( type )
    {
    }
    #endregion

    #region GetValue
    protected override string GetValue( Enum enumValue )
    {
      return enumValue.GetDisplayName();
    }
    #endregion
  }
}
