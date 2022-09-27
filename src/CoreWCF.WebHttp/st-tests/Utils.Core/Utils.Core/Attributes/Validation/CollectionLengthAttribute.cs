using PostSharp.Reflection;

using System.Collections;

namespace ST.Utils.Attributes
{
  /// <summary>
  /// Валидатор, гарантирующий минимальную и максимальную длину коллекции или строки.
  /// Если размер строки/коллекции оставляет желать лучшего, то выбрасывается исключение ArgumentException.
  /// </summary>
  [PSerializable]
  public class CollectionLengthAttribute : NotNullNotEmptyAttribute
  {
    #region .Fields
    private int _min;
    private int _max;
    #endregion

    #region .Ctor
    /// <summary>Конструктор.</summary>
    /// <param name="min">Минимально допустимая длина или -1, если нет ограничения на минимальную длину.</param>
    /// <param name="max">Максимально допустимая длина или -1, если нет ограничения на максимальную длину.</param>
    public CollectionLengthAttribute( int min, int max = -1 )
    {
      _min = min;
      _max = max;
    }
    #endregion

    #region CompileTimeValidate
    public override bool CompileTimeValidate( LocationInfo locationInfo )
    {
      var elementType = GetElementType( locationInfo );

      return elementType != typeof( string ) && !typeof( ICollection ).IsAssignableFrom( elementType ) ? AspectHelper.Fail( 1, "{0} is applicable for string or collection elements only.", GetType().Name ) : base.CompileTimeValidate( locationInfo );
    }
    #endregion

    #region GetValidationException
    protected override Exception GetValidationException( object value, string name )
    {
      var exception = base.GetValidationException( value, name );

      if( exception != null )
        return exception;

      if( value is string && !IsValidLength( (value as string).Length ) )
        return new ArgumentException( "Wrong string size.", name );

      if( value is ICollection && !IsValidLength( (value as ICollection).Count ) )
        return new ArgumentException( "Wrong collection size.", name );

      return null;
    }
    #endregion

    #region IsValidLength
    private bool IsValidLength( int length )
    {
      return (_min < 0 || _min <= length) && (_max < 0 || length <= _max);
    }
    #endregion
  }
}
