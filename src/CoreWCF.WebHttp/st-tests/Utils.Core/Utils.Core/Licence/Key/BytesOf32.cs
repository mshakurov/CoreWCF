using System;

namespace ST.Utils.Licence
{
  /// <summary>
  /// Служебный класс. Является оберткой 
  /// для массива байт длиной в 32 байта.
  /// </summary>
  public class BytesOf32
  {
    #region .Constants
    /// <summary>
    /// Размерность массива байт
    /// </summary>
    public const int ByteArraySize = 32;
    #endregion

    #region .Fields
    private byte[] _value;
    #endregion

    #region .Properties
    /// <summary>
    /// Значением является 
    /// массив 32 байт. Not null
    /// </summary>
    public byte[] Value
    {
      get
      {
        return _value;
      }
      set
      {
        if( value == null )
          throw new Exception( "Значение не задано" );

        if( value.Length != ByteArraySize )
          throw new Exception( "Размерность массива должна быть 32 байта" );

        _value = value;
      }
    }
    #endregion

    #region .Ctor
    public BytesOf32( byte[] value )
    {
      Value = value;
    }
    #endregion
  }
}
