using PostSharp.Reflection;

using System.Reflection;

namespace ST.Utils.Attributes
{
  /// <summary>
  /// Валидатор, гарантирующий, что значение параметра/свойста удовлетворяет условиям метода, реализующего проверку.
  /// Метод, реализующий условия проверки должен иметь сигнатуру:
  /// public static string Validate( T value ); где T тип параметра/свойства.
  /// Если метод возвращает null, то проверка прошла успешно, в противном случае возвращается строка с описанием несоответствия.
  /// </summary>
  /// 
  [PSerializable]
  [AttributeUsage( AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true, Inherited = true )]
  public class CustomValidatorAttribute : ValidatorAspect
  {
    #region .Fields
    [PNonSerialized]
    private Type _validatorType;

    [PNonSerialized]
    private string _validateMethodName;

    private MethodInfo _validateMethod;
    #endregion

    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="validatorType">Тип, реализующий метод проверки.</param>
    /// <param name="validateMethodName">Название метода.</param>
    public CustomValidatorAttribute( Type validatorType, string validateMethodName )
    {
      if( validatorType == null )
        throw new ArgumentNullException( "validatorType" );

      _validatorType = validatorType;

      if( string.IsNullOrWhiteSpace( validateMethodName ) )
        throw new ArgumentNullException( "validateMethodName" );

      _validateMethodName = validateMethodName;
    }
    #endregion

    #region CompileTimeValidate
    public override bool CompileTimeValidate( LocationInfo locationInfo )
    {
      var elementType = GetElementType( locationInfo );

      _validateMethod = _validatorType.GetMethod( _validateMethodName, BindingFlags.Public | BindingFlags.Static, null, new Type[] { elementType }, null );

      return _validateMethod == null ? AspectHelper.Fail( 1, "Validator class {0} does not contain {1}( {2} ) method.", _validatorType, _validateMethodName, elementType.Name ) :
             _validateMethod.ReturnType != typeof( string ) ? AspectHelper.Fail( 2, "The type of return value of validation method {0}.{1}( {2} ) is not a string.", _validatorType, _validateMethodName, elementType.Name ) :
             base.CompileTimeValidate( locationInfo );
    }
    #endregion

    #region GetValidationException
    protected override Exception GetValidationException( object value, string name )
    {
      var failMessage = _validateMethod.Invoke( null, new object[] { value } ) as string;

      if( failMessage != null )
        return new ArgumentException( string.Format( "Validation fail: {0}, Value = {1}", failMessage, value ), name );

      return null;
    }
    #endregion
  }
}
