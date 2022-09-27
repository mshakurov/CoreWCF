using PostSharp.Reflection;

using System.Text.RegularExpressions;

namespace ST.Utils.Attributes
{
  /// <summary>
  /// Валидатор, гарантирующий соответствие значения строкового параметра/свойства регулярному выражению.
  /// Если значение параметра/свойства не соответствует регулярному выражению, то выбрасывается исключение ArgumentOutOfRangeException.
  /// </summary>
  [PSerializable]
  public class MatchAttribute : NotNullNotEmptyAttribute
  {
    #region .Fields
    private string _pattern;
    #endregion

    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="pattern">Регулярное выражение.</param>
    public MatchAttribute( string pattern )
    {
      if( string.IsNullOrEmpty( pattern ) )
        throw new ArgumentException( "Null or empty.", "pattern" );

      _pattern = pattern;
    }
    #endregion

    #region CompileTimeValidate
    public override bool CompileTimeValidate( LocationInfo locationInfo )
    {
      return GetElementType( locationInfo ) != typeof( string ) ? AspectHelper.Fail( 1, "Attribute {0} is applicable for string elements only.", GetType().Name ) : base.CompileTimeValidate( locationInfo );
    }
    #endregion

    #region GetValidationException
    protected override Exception GetValidationException( object value, string name )
    {
      var exception = base.GetValidationException( value, name );

      if( exception != null )
        return exception;

      if( !new Regex( _pattern, RegexOptions.Compiled ).IsMatch( (string) value ) )
        return new ArgumentOutOfRangeException( "Invalid value.", name );

      return null;
    }
    #endregion
  }
}
