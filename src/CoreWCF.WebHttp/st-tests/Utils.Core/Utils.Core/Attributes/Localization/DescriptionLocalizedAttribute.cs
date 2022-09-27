using System.ComponentModel;

namespace ST.Utils.Attributes
{
  /// <summary>
  /// Атрибут применяется аналогично DescriptionAttribute и вызывает те же последствия, что применение DescriptionAttribute,
  /// за исключением того, что описание элемента принимает значение в зависимости от текущей культуры.
  /// </summary>
  [PSerializable]
  public sealed class DescriptionLocalizedAttribute : LocalizationAspect
  {
    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="resourceId">Идентификатор ресурсной строки.</param>
    public DescriptionLocalizedAttribute( string resourceId ) : base( typeof( DescriptionLocalizedAttributeImpl ), resourceId )
    {
    }
    #endregion

    /// <summary>
    /// Класс только для внутреннего использования.
    /// </summary>
    [Serializable]
    public class DescriptionLocalizedAttributeImpl : DescriptionAttribute
    {
      #region .Properties
      public override string Description
      {
        get { return SR.GetString( base.Description ); }
      }
      #endregion

      #region .Ctor
      public DescriptionLocalizedAttributeImpl( string resourceId ) : base( resourceId )
      {
      }
      #endregion
    }
  }
}
