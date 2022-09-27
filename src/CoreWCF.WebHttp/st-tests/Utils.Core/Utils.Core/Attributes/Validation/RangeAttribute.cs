using PostSharp.Reflection;

namespace ST.Utils.Attributes
{
  /// <summary>
  /// Валидатор, гарантирующий попадание значения параметра/свойства в интервал(ы). Применим только числовых типов и DateTime.
  /// Если значение параметра/свойства не попадает (ни в один) в интервал, то выбрасывается исключение ArgumentOutOfRangeException. 
  /// Для числовых типов массив границ интервалов должен быть не пустым и содержать только числа.
  /// Для типа DateTime массив границ интервалов должен быть не пустым и содержать только строки в формате, который допускает преобразование в DateTime.
  /// </summary>
  [PSerializable]
  public class RangeAttribute : NotNullNotEmptyAttribute
  {
    #region .Fields
    private bool _isElementNumeric;

    [PNonSerialized]
    private List<Tuple<object, object>> _ranges = new List<Tuple<object, object>>();
    #endregion

    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="rangePoints">Интервалы значений.</param>
    public RangeAttribute( params object[] rangePoints )
    {
      if( rangePoints == null || rangePoints.Length < 1 )
        throw new ArgumentException( "Null or empty array.", "rangePoints" );

      if( rangePoints[0] == null )
        throw new ArgumentException( "Null element.", "rangePoints" );

      object[] typedRangePoints = new object[rangePoints.Length];

      if( rangePoints[0] is string )
      {
        // Проверяем, что все элементы - это строки, и конвертируем их в DateTime.
        for( var i = 0; i < rangePoints.Length; i++ )
        {
          var rval = rangePoints[i];

          if( rval == null )
            throw new ArgumentException( "Null element.", "rangePoints" );

          if( !(rval is string) )
            throw new ArgumentException( string.Format( "Incorrect type of element. {0}", rval ), "rangePoints" );

          DateTime date;

          if( !DateTime.TryParse( rval as string, out date ) )
            throw new ArgumentException( string.Format( "Incorrect datetime format. {0}", rval ), "rangePoints" );

          typedRangePoints[i] = date;
        }
      }
      else
        if( IsNumericType( rangePoints[0].GetType() ) )
        {
          _isElementNumeric = true;

          for( var i = 0; i < rangePoints.Length; i++ )
          {
            var rval = rangePoints[i];

            if( rval == null )
              throw new ArgumentException( "Null element.", "rangePoints" );

            if( !IsNumericType( rval.GetType() ) )
              throw new ArgumentException( string.Format( "Incorrect type of element. {0}", rval ), "rangePoints" );

            typedRangePoints[i] = Convert.ToDouble( rval );
          }
        }
        else
          throw new ArgumentException( "Incompatible type of elements.", "rangePoints" );

      for( var index = 0; index < rangePoints.Length; index += 2 )
      {
        var left = typedRangePoints[index];
        var right = (index + 1 < typedRangePoints.Length) ? typedRangePoints[index + 1] : left;

        if( (left as IComparable).CompareTo( right ) > 0 )
          throw new ArgumentException( string.Format( "Wrong order of range edges. Range [{0} - {1}]", left, right ), "rangePoints" );

        _ranges.Add( new Tuple<object, object>( left, right ) );
      }
    }
    #endregion

    #region CompileTimeValidate
    public override bool CompileTimeValidate( LocationInfo locationInfo )
    {
      var elementType = GetElementType( locationInfo );

      return !_isElementNumeric && IsNumericType( elementType ) ? AspectHelper.Fail( 1, "Non numeric range attibute applied for numeric element." ) :
             _isElementNumeric && !IsNumericType( elementType ) ? AspectHelper.Fail( 2, "Numeric range applied for non numeric element." ) :
             base.CompileTimeValidate( locationInfo );
    }
    #endregion

    #region GetValidationException
    protected override Exception GetValidationException( object value, string name )
    {
      var exception = base.GetValidationException( value, name );

      if( exception != null )
        return exception;

      if( _ranges.All( r => !((_isElementNumeric ? Convert.ToDouble( value ) : value) as IComparable).InRange( r.Item1, r.Item2 ) ) )
        return new ArgumentOutOfRangeException( name );

      return null;
    }
    #endregion
  }
}
