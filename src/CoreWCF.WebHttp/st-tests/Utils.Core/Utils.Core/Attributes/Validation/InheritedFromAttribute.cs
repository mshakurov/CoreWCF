using PostSharp.Reflection;

namespace ST.Utils.Attributes
{
  /// <summary>
  /// Валидатор, гарантирующий, что значение-тип параметра/свойства унаследован от заданного базового типа.
  /// Если значение-тип оставляет желать лучшего, то выбрасывается исключение ArgumentException.
  /// </summary>
  [PSerializable]
  public class InheritedFromAttribute : NotNullAttribute
  {
    #region .Fields
    private Type _baseType;

    private bool _allowNull;
    #endregion

    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="baseType">Базовый тип.</param>
    public InheritedFromAttribute( Type baseType )
    {
      _baseType = baseType;
    }

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="baseType">Базовый тип.</param>
    /// <param name="allowNull">Признак того, что параметр/свойство может принимать значение null.</param>
    public InheritedFromAttribute( Type baseType, bool allowNull ) : this( baseType )
    {
      _allowNull = allowNull;
    }
    #endregion

    #region CompileTimeValidate
    public override bool CompileTimeValidate( LocationInfo locationInfo )
    {
      return GetElementType( locationInfo ) != typeof( Type ) ? AspectHelper.Fail( 1, "{0} is applicable for elements with type of 'Type' only.", GetType().Name ) : base.CompileTimeValidate( locationInfo );
    }
    #endregion

    #region GetValidationException
    protected override Exception GetValidationException( object value, string name )
    {
      if( !_allowNull || value != null )
      {
        var exception = base.GetValidationException( value, name );

        if( exception != null )
          return exception;

        if( !(value as Type).IsInheritedFrom( _baseType ) )
          return new ArgumentException( string.Format( "The type must be inherited from type '{0}.", _baseType.Name ), name );
      }

      return null;
    }
    #endregion
  }
}
