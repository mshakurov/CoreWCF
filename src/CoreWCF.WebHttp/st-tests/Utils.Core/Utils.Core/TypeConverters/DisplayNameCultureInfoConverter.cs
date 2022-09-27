using System.ComponentModel;
using System.Globalization;

namespace ST.Utils.TypeConverters
{
  /// <summary>
  /// Преобразователь культур на основе свойства DisplayName.
  /// Список возможных значений содержит культуры, используемые приложением -
  /// информация извлекается на основании названий каталогов ресурсных (satellite) сборок, а также учитывается культура по умолчанию (SR.DefaultCulture).
  /// </summary>
  public class DisplayNameCultureInfoConverter : CultureInfoConverter
  {
    #region .Fields
    private TypeConverter.StandardValuesCollection _values;
    #endregion

    #region GetCultureName
    protected override string GetCultureName( CultureInfo culture )
    {
      return culture.GetDisplayName();
    }
    #endregion

    #region GetStandardValues
    public override StandardValuesCollection GetStandardValues( ITypeDescriptorContext context )
    {
      return _values ?? (_values = new TypeConverter.StandardValuesCollection( EnvironmentHelper.GetApplicationCultures() ));
    }
    #endregion

    #region GetStandardValuesExclusive
    public override bool GetStandardValuesExclusive( ITypeDescriptorContext context )
    {
      return true;
    }
    #endregion
  }
}
