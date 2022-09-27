namespace ST.Utils.Attributes
{
  /// <summary>
  /// Класс только для внутреннего использования.
  /// </summary>
  [Serializable]
  public class StringLocalizedAttribute : Attribute
  {
    #region .Fields
    private readonly string _resourceId;
    #endregion

    #region .Properties
    public string LocalizedString
    {
      get { return SR.GetString( _resourceId ); }
    }
    #endregion

    #region .Ctor
    public StringLocalizedAttribute( string resourceId )
    {
      _resourceId = resourceId;
    }
    #endregion
  }
}
