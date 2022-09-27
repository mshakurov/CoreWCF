using PostSharp.Reflection;

namespace ST.Utils.Attributes
{
  /// <summary>
  /// Валидатор, гарантирующий соответствие значения параметра/свойства набору допустимых значений. Применим только числовых типов и строк.
  /// Если значение параметра/свойства не принадлежит набору допустимых значений, то выбрасывается исключение ArgumentOutOfRangeException.
  /// </summary>
  [PSerializable]
  public class OneOfAttribute : NotNullNotEmptyAttribute
  {
    #region .Fields
    private bool _isElementNumeric;

    private object[] _validValues;
    #endregion

    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="validValues">Набор допустимых значений.</param>
    public OneOfAttribute( params object[] validValues )
    {
      if( validValues == null || validValues.Length < 1 )
        throw new ArgumentException( "Null or empty array.", "validValues" );

      if( validValues[0] == null )
        throw new ArgumentException( "Null element.", "validValues" );

      if( validValues[0] is string )
      {
        // Проверяем, что все элементы массива это строки.
        for( var i = 0; i < validValues.Length; i++ )
        {
          var rval = validValues[i];

          if( rval == null )
            throw new ArgumentException( "Null element.", "validValues" );

          if( !(rval is string) )
            throw new ArgumentException( string.Format( "Incorrect type of element. {0}", rval ), "validValues" );
        }
        _validValues = validValues;
      }
      else
        if( IsNumericType( validValues[0].GetType() ) )
        {
          _isElementNumeric = true;

          _validValues = new object[validValues.Length];

          // Проверяем, что все элементы массива это числа и сохраняем их как double.
          for( var i = 0; i < validValues.Length; i++ )
          {
            var rval = validValues[i];

            if( rval == null )
              throw new ArgumentException( "Null element.", "validValues" );

            if( !IsNumericType( rval.GetType() ) )
              throw new ArgumentException( string.Format( "Incorrect type of element. {0}", rval ), "validValues" );

            _validValues[i] = Convert.ToDouble( rval );
          }
        }
        else
          throw new ArgumentException( "Incompatible type of elements.", "validValues" );
    }
    #endregion

    #region CompileTimeValidate
    public override bool CompileTimeValidate( LocationInfo locationInfo )
    {
      var elementType = GetElementType( locationInfo );

      return !_isElementNumeric && IsNumericType( elementType ) ? AspectHelper.Fail( 1, "Non numeric valid values attibute applied for numeric element." ) :
             _isElementNumeric && !IsNumericType( elementType ) ? AspectHelper.Fail( 2, "Numeric valid values applied for non numeric element." ) :
             base.CompileTimeValidate( locationInfo );
    }
    #endregion

    #region GetValidationException
    protected override Exception GetValidationException( object value, string name )
    {
      var exception = base.GetValidationException( value, name );

      if( exception != null )
        return exception;

      if( !_validValues.Contains( _isElementNumeric ? Convert.ToDouble( value ) : value ) )
        return new ArgumentOutOfRangeException( "Invalid value.", name );

      return null;
    }
    #endregion
  }
}
