using System;

namespace ST.Utils.Licence
{
  /// <summary>
  /// Класс реализует алгоритм 
  /// шифрования ГОСТ 28147-89
  /// http://ru.wikipedia.org/wiki/%D0%93%D0%9E%D0%A1%D0%A2_28147-89
  /// </summary>
  /// <example>
  ///		GostCrypter core = new GostCrypter();
  ///
  ///		var rnd = new Random();
  ///
  ///		var key = new byte[32];
  ///		rnd.NextBytes(key);
  ///
  ///		core.Key = key;
  ///		core.SBlock = GostCrypter.DefaultSBlock;
  ///	
  ///		var inArray = new byte[24];//3 block of 8 byte
  ///		rnd.NextBytes(inArray);
  ///
  ///		Console.WriteLine("Input array:");
  ///		Console.WriteLine(BitConverter.ToString(inArray));	
  ///
  ///		var enArray = core.Encode(inArray); //crypt array
  ///
  ///		Console.WriteLine("Encode array:");
  ///		Console.WriteLine(BitConverter.ToString(enArray));	
  ///
  ///		var decodeArray = core.Decode(enArray); //decrypt array
  ///
  ///		Console.WriteLine("Decode array:");
  ///		Console.WriteLine(BitConverter.ToString(decodeArray));	
  ///				
  ///		Console.ReadKey();
  /// </example>
  public class GostCrypter : ICrypter
  {
    #region .Constants
    /// <summary>
    /// Количество байт в блоке
    /// </summary>
    internal const int BlockSize = 8;

    /// <summary>
    /// Число байт в S-блоке
    /// </summary>
    internal const int SBoxLength = 128;

    /// <summary>
    /// Длина ключа в байтах
    /// </summary>
    internal const int KeyLength = 32;
    #endregion

    #region .Static Fields
    /// <summary>
    /// S-блок по умолчанию
    /// </summary>
    public static readonly byte[] DefaultSBlock =
    {
			0x4,0xA,0x9,0x2,0xD,0x8,0x0,0xE,0x6,0xB,0x1,0xC,0x7,0xF,0x5,0x3,
			0xE,0xB,0x4,0xC,0x6,0xD,0xF,0xA,0x2,0x3,0x8,0x1,0x0,0x7,0x5,0x9,
			0x5,0x8,0x1,0xD,0xA,0x3,0x4,0x2,0xE,0xF,0xC,0x7,0x6,0x0,0x9,0xB,
			0x7,0xD,0xA,0x1,0x0,0x8,0x9,0xF,0xE,0x4,0x6,0xC,0xB,0x2,0x5,0x3,
			0x6,0xC,0x7,0x1,0x5,0xF,0xD,0x8,0x4,0xA,0x9,0xE,0x0,0x3,0xB,0x2,
			0x4,0xB,0xA,0x0,0x7,0x2,0x1,0xD,0x3,0x6,0x8,0x5,0x9,0xC,0xF,0xE,
			0xD,0xB,0x4,0x1,0x3,0xF,0x5,0x9,0x0,0xA,0xE,0x7,0x6,0x8,0x2,0xC,
			0x1,0xF,0xD,0x0,0x5,0x7,0xA,0x4,0x9,0x2,0x3,0xE,0x6,0xB,0x8,0xC
		};
    #endregion

    #region .Fields
    private int[] _workingKey = null;
    private byte[] _key;
    private byte[] _sBlock;
    #endregion

    #region .Properties
    /// <summary>
    /// Ключ который используется 
    /// при шифорвании и дешифровании (256 бит)
    /// </summary>
    public byte[] Key
    {
      get { return _key; }
      set
      {
        if( value == null )
        {
          _key = null;
          _workingKey = null;
        }
        else
        {
          if( value.Length != KeyLength )
            throw new Exception( "Key length is invalid  (valid length is 256 bit)" );

          _key = value;

          if( _workingKey == null )
            _workingKey = new int[8];

          for( int i = 0; i != 8; i++ )
            _workingKey[i] = BytesToInt( _key, i * 4 );
        }
      }
    }

    /// <summary>
    /// S-блок (128 байт)
    /// </summary>
    public byte[] SBlock
    {
      get { return _sBlock; }
      set
      {
        if( value != null )
        {
          if( value.Length != SBoxLength )

            if( SBlock == null )
              throw new Exception( "S-block length is invalid (valid length is 128 byte)" );


          if( _sBlock == null )
            _sBlock = new byte[SBoxLength];

          Array.Copy( value, 0, _sBlock, 0, SBoxLength );
        }
        else
          _sBlock = value;
      }
    }
    #endregion

    #region .Ctor
    public GostCrypter()
    {
    }
    #endregion

    #region BytesToInt
    private static int BytesToInt( byte[] inBytes, int inOff )
    {
      return (int)( ( inBytes[inOff + 3] << 24 ) & 0xff000000 ) +
                  ( ( inBytes[inOff + 2] << 16 ) & 0xff0000 ) +
                  ( ( inBytes[inOff + 1] << 8 ) & 0xff00 ) + ( inBytes[inOff] & 0xff );
    }
    #endregion

    #region DoOneStep
    private int DoOneStep( int n1, int key )
    {
      int cm = ( key + n1 );

      int om = _sBlock[0 + ( ( cm >> ( 0 * 4 ) ) & 0xF )] << ( 0 * 4 );
      om += _sBlock[16 + ( ( cm >> ( 1 * 4 ) ) & 0xF )] << ( 1 * 4 );
      om += _sBlock[32 + ( ( cm >> ( 2 * 4 ) ) & 0xF )] << ( 2 * 4 );
      om += _sBlock[48 + ( ( cm >> ( 3 * 4 ) ) & 0xF )] << ( 3 * 4 );
      om += _sBlock[64 + ( ( cm >> ( 4 * 4 ) ) & 0xF )] << ( 4 * 4 );
      om += _sBlock[80 + ( ( cm >> ( 5 * 4 ) ) & 0xF )] << ( 5 * 4 );
      om += _sBlock[96 + ( ( cm >> ( 6 * 4 ) ) & 0xF )] << ( 6 * 4 );
      om += _sBlock[112 + ( ( cm >> ( 7 * 4 ) ) & 0xF )] << ( 7 * 4 );

      return ( om << 11 ) | ( (int)( ( (uint)om ) >> ( 32 - 11 ) ) );
    }
    #endregion

    #region IntToBytes
    private static void IntToBytes( int num, byte[] outBytes, int outOff )
    {
      outBytes[outOff + 3] = (byte)( num >> 24 );

      outBytes[outOff + 2] = (byte)( num >> 16 );

      outBytes[outOff + 1] = (byte)( num >> 8 );

      outBytes[outOff] = (byte)num;
    }
    #endregion

    #region ProcessArray
    private byte[] ProcessArray( byte[] input, bool isEncryption )
    {
      if( input == null )
        throw new Exception( "Input byte array is not set" );

      if( input.Length % BlockSize != 0 )
        throw new Exception( "Input byte array is uncorrect(not multiple of 8 bits)" );

      if( Key == null )
        throw new Exception( "Key is not set" );

      if( SBlock == null )
        throw new Exception( "S-Block is not set" );

      var blocksCount = input.Length / BlockSize;

      var res = new byte[input.Length];

      var inBlock = new byte[BlockSize];

      var outBlock = new byte[BlockSize];

      for( int i = 0; i < blocksCount; i++ )
      {
        var position = BlockSize * i;

        Array.Copy( input, position, inBlock, 0, BlockSize );

        ProcessBlock( inBlock, outBlock, isEncryption );

        Array.Copy( outBlock, 0, res, position, BlockSize );
      }

      return res;
    }
    #endregion

    #region ProcessBlock
    private void ProcessBlock( byte[] inBytes, byte[] outBytes, bool isEncryption )
    {
      int tmp;  //tmp -> for saving N1

      int left = BytesToInt( inBytes, 0 );

      int right = BytesToInt( inBytes, 4 );

      if( isEncryption )
      {
        for( int k = 0; k < 3; k++ )  // 1-24 steps
        {
          for( int j = 0; j < 8; j++ )
          {
            tmp = left;

            int step = DoOneStep( left, _workingKey[j] );

            left = right ^ step; // CM2

            right = tmp;
          }
        }
        for( int j = 7; j > 0; j-- )  // 25-31 steps
        {
          tmp = left;

          left = right ^ DoOneStep( left, _workingKey[j] ); // CM2

          right = tmp;
        }
      }
      else //decrypt
      {
        for( int j = 0; j < 8; j++ )  // 1-8 steps
        {
          tmp = left;

          left = right ^ DoOneStep( left, _workingKey[j] ); // CM2

          right = tmp;
        }
        for( int k = 0; k < 3; k++ )  //9-31 steps
        {
          for( int j = 7; j >= 0; j-- )
          {
            if( ( k == 2 ) && ( j == 0 ) )
            {
              break; // break 32 step
            }

            tmp = left;

            left = right ^ DoOneStep( left, _workingKey[j] ); // CM2
            right = tmp;
          }
        }
      }

      right = right ^ DoOneStep( left, _workingKey[0] );  // 32 step (N1=N1)

      IntToBytes( left, outBytes, 0 );

      IntToBytes( right, outBytes, 4 );
    }
    #endregion

    #region ICrypter
    /// <summary>
    /// Метод шифрует массив байт. 
    /// Должен быть кратен 8 байтам
    /// </summary>
    /// <param name="input">массив байт который будет зашифрован</param>
    /// <returns>массив содержит зашифрованный входной массив, 
    /// и имеет ту же размерность</returns>
    public byte[] Encode( byte[] input )
    {
      return ProcessArray( input, true );
    }

    /// <summary>
    /// Метод дешифрует массив байт. 
    /// Должен быть кратен 8 байтам
    /// </summary>
    /// <param name="input">массив байт который будет дешифрован</param>
    /// <returns>массив содержит дешифрованный входной массив, 
    /// и имеет ту же размерность</returns>
    public byte[] Decode( byte[] input )
    {
      return ProcessArray( input, false );
    }

    public int GetAligmentSize( int dataSize )
    {
      var aligmentSize = dataSize % BlockSize; //кратен ли входной массив размеру блока

      if( aligmentSize != 0 )
        aligmentSize = BlockSize - aligmentSize;

      return aligmentSize;
    }
    #endregion
  }
}
