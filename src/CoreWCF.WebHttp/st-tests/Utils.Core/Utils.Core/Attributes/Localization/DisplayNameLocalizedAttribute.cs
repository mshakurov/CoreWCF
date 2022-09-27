using System.ComponentModel;

namespace ST.Utils.Attributes
{
  /// <summary>
  /// Атрибут применяется аналогично DisplayNameAttribute и вызывает те же последствия, что применение DisplayNameAttribute,
  /// за исключением того, что имя элемента принимает значение в зависимости от текущей культуры.
  /// </summary>
  [PSerializable]
  public sealed class DisplayNameLocalizedAttribute : LocalizationAspect
  {
    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="resourceId">Идентификатор ресурсной строки.</param>
    public DisplayNameLocalizedAttribute( string resourceId ) : base( typeof( DisplayNameLocalizedAttributeImpl ), resourceId )
    {
    }
    #endregion

    /// <summary>
    /// Класс только для внутреннего использования.
    /// </summary>
    [Serializable]
    public class DisplayNameLocalizedAttributeImpl : DisplayNameAttribute
    {
      #region .Properties
      public override string DisplayName
      {
        get { return SR.GetString( base.DisplayName ); }
      }
      #endregion

      #region .Ctor
      public DisplayNameLocalizedAttributeImpl( string resourceId ) : base( resourceId )
      {
      }
      #endregion
    }
  }
}
