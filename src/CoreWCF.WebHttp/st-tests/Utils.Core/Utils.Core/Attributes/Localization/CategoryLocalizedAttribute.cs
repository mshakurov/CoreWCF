using System.ComponentModel;

namespace ST.Utils.Attributes
{
  /// <summary>
  /// Атрибут применяется аналогично CategoryAttribute и вызывает те же последствия, что применение CategoryAttribute,
  /// за исключением того, что имя категории принимает значение в зависимости от текущей культуры.
  /// </summary>
  [PSerializable]
  public sealed class CategoryLocalizedAttribute : LocalizationAspect
  {
    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="resourceId">Идентификатор ресурсной строки.</param>
    public CategoryLocalizedAttribute( string resourceId ) : base( typeof( CategoryLocalizedAttributeImpl ), resourceId )
    {
    }
    #endregion

    /// <summary>
    /// Класс только для внутреннего использования.
    /// </summary>
    [Serializable]
    public class CategoryLocalizedAttributeImpl : CategoryAttribute
    {
      #region .Ctor
      public CategoryLocalizedAttributeImpl( string resourceId ) : base( resourceId )
      {
      }
      #endregion

      #region GetLocalizedString
      protected override string GetLocalizedString( string value )
      {
        return SR.GetString( value );
      }
      #endregion
    }
  }
}
