using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using ST.Utils.Attributes;

namespace ST.Utils
{
  /// <summary>
  /// Критерий фильтрации элементов.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Constants.QUERY_FILTER_NAMESPACE )]
  [KnownType( typeof( byte[] ) ), KnownType( typeof( sbyte[] ) ), KnownType( typeof( short[] ) ), KnownType( typeof( ushort[] ) ), KnownType( typeof( int[] ) ), KnownType( typeof( uint[] ) ),
   KnownType( typeof( long[] ) ), KnownType( typeof( ulong[] ) ), KnownType( typeof( Single[] ) ), KnownType( typeof( double[] ) ), KnownType( typeof( bool[] ) ), KnownType( typeof( char[] ) ),
   KnownType( typeof( decimal[] ) ), KnownType( typeof( string[] ) ), KnownType( typeof( ValueText ) ), KnownType( typeof( InjectText ) ), KnownType( typeof( ValueText[] ) ), KnownType( typeof( InjectText[] ) ), KnownType( typeof( DateTime ) ), KnownType( typeof( DateTime[] ) )]
  public sealed class QueryFilterCriterion : QueryFilterNode
  {
    #region .Static Fields
    private static readonly Dictionary<QueryFilterComparison, string> _names = new Dictionary<QueryFilterComparison, string>();
    #endregion

    #region .Fields
    private readonly QueryFilter _filter;
    #endregion

    #region .Properties
    /// <summary>
    /// Оператор сравнения.
    /// </summary>
    [DataMember]
    public QueryFilterComparison Comparison { get; private set; }

    /// <summary>
    /// Название элемента.
    /// </summary>
    [DataMember]
    public string Name { get; private set; }

    /// <summary>
    /// Значение элемента.
    /// </summary>
    [DataMember]
    public object Value { get; private set; }
    #endregion

    #region .Ctor
    static QueryFilterCriterion()
    {
      foreach( QueryFilterComparison value in Enum.GetValues( typeof( QueryFilterComparison ) ) )
        _names.Add( value, value.GetDisplayName() );
    }

    internal QueryFilterCriterion( QueryFilter filter, string name )
    {
      _filter = filter;

      Name = name;
    }
    #endregion

    #region Eq
    /// <summary>
    /// Задает критерий фильтрации "Равно".
    /// </summary>
    /// <param name="value">Значение.</param>
    /// <returns>Фильтр.</returns>
    public QueryFilter Eq( [NotNull] string value )
    {
      return Set( QueryFilterComparison.Eq, value );
    }

    /// <summary>
    /// Задает критерий фильтрации "Равно".
    /// </summary>
    /// <param name="value">Значение.</param>
    /// <returns>Фильтр.</returns>
    public QueryFilter Eq<T>( T value )
      where T : struct
    {
      return Set( QueryFilterComparison.Eq, value );
    }
    #endregion

    #region Gr
    /// <summary>
    /// Задает критерий фильтрации "Больше".
    /// </summary>
    /// <param name="value">Значение.</param>
    /// <returns>Фильтр.</returns>
    public QueryFilter Gr( [NotNull] string value )
    {
      return Set( QueryFilterComparison.Gr, value );
    }

    /// <summary>
    /// Задает критерий фильтрации "Больше".
    /// </summary>
    /// <param name="value">Значение.</param>
    /// <returns>Фильтр.</returns>
    public QueryFilter Gr<T>( T value )
      where T : struct
    {
      return Set( QueryFilterComparison.Gr, value );
    }
    #endregion

    #region GrOrEq
    /// <summary>
    /// Задает критерий фильтрации "Больше или равно".
    /// </summary>
    /// <param name="value">Значение.</param>
    /// <returns>Фильтр.</returns>
    public QueryFilter GrOrEq( [NotNull] string value )
    {
      return Set( QueryFilterComparison.GrOrEq, value );
    }

    /// <summary>
    /// Задает критерий фильтрации "Больше или равно".
    /// </summary>
    /// <param name="value">Значение.</param>
    /// <returns>Фильтр.</returns>
    public QueryFilter GrOrEq<T>( T value )
      where T : struct
    {
      return Set( QueryFilterComparison.GrOrEq, value );
    }
    #endregion

    #region In
    /// <summary>
    /// Задает критерий фильтрации "Входит".
    /// </summary>
    /// <param name="values">Значения.</param>
    /// <returns>Фильтр.</returns>
    public QueryFilter In( params string[] values )
    {
      if ( values.Length == 0 )
      {
        Name = "EntityId";
        Comparison = QueryFilterComparison.Eq;
        Value = null;
        return _filter;
      }
      return Set( QueryFilterComparison.In, values );
    }

    /// <summary>
    /// Задает критерий фильтрации "Входит".
    /// </summary>
    /// <param name="values">Значения.</param>
    /// <returns>Фильтр.</returns>
    public QueryFilter In<T>( params T[] values )
      where T : struct
    {
      if (values.Length == 0)
      {
        Name = "EntityId";
        Comparison = QueryFilterComparison.Eq;
        Value = null;
        return _filter;
      }
      return Set( QueryFilterComparison.In, values );
    }
    #endregion

    #region IsEmpty
    /// <summary>
    /// Задает критерий фильтрации "Не содержит значения".
    /// </summary>
    /// <returns>Фильтр.</returns>
    public QueryFilter IsEmpty()
    {
      return Set( QueryFilterComparison.Eq, null );
    }
    #endregion

    #region IsNotEmpty
    /// <summary>
    /// Задает критерий фильтрации "Содержит значение".
    /// </summary>
    /// <returns>Фильтр.</returns>
    public QueryFilter IsNotEmpty()
    {
      return Set( QueryFilterComparison.NotEq, null );
    }
    #endregion

    #region Le
    /// <summary>
    /// Задает критерий фильтрации "Меньше".
    /// </summary>
    /// <param name="value">Значение.</param>
    /// <returns>Фильтр.</returns>
    public QueryFilter Le( [NotNull] string value )
    {
      return Set( QueryFilterComparison.Le, value );
    }

    /// <summary>
    /// Задает критерий фильтрации "Меньше".
    /// </summary>
    /// <param name="value">Значение.</param>
    /// <returns>Фильтр.</returns>
    public QueryFilter Le<T>( T value )
      where T : struct
    {
      return Set( QueryFilterComparison.Le, value );
    }
    #endregion

    #region LeOrEq
    /// <summary>
    /// Задает критерий фильтрации "Меньше или равно".
    /// </summary>
    /// <param name="value">Значение.</param>
    /// <returns>Фильтр.</returns>
    public QueryFilter LeOrEq( [NotNull] string value )
    {
      return Set( QueryFilterComparison.LeOrEq, value );
    }

    /// <summary>
    /// Задает критерий фильтрации "Меньше или равно".
    /// </summary>
    /// <param name="value">Значение.</param>
    /// <returns>Фильтр.</returns>
    public QueryFilter LeOrEq<T>( T value )
      where T : struct
    {
      return Set( QueryFilterComparison.LeOrEq, value );
    }
    #endregion

    #region Like
    /// <summary>
    /// Задает критерий фильтрации "Похоже на".
    /// </summary>
    /// <param name="value">Значение.</param>
    /// <returns>Фильтр.</returns>
    public QueryFilter Like( [NotNull] string value )
    {
      return Set( QueryFilterComparison.Like, value );
    }
    #endregion

    #region NotEq
    /// <summary>
    /// Задает критерий фильтрации "Не равно".
    /// </summary>
    /// <param name="value">Значение.</param>
    /// <returns>Фильтр.</returns>
    public QueryFilter NotEq( [NotNull] string value )
    {
      return Set( QueryFilterComparison.NotEq, value );
    }

    /// <summary>
    /// Задает критерий фильтрации "Не равно".
    /// </summary>
    /// <param name="value">Значение.</param>
    /// <returns>Фильтр.</returns>
    public QueryFilter NotEq<T>( T value )
      where T : struct
    {
      return Set( QueryFilterComparison.NotEq, value );
    }
    #endregion

    #region NotIn
    /// <summary>
    /// Задает критерий фильтрации "Не входит".
    /// </summary>
    /// <param name="values">Значения.</param>
    /// <returns>Фильтр.</returns>
    public QueryFilter NotIn( [NotNullNotEmpty] params string[] values )
    {
      return Set( QueryFilterComparison.NotIn, values );
    }

    /// <summary>
    /// Задает критерий фильтрации "Не входит".
    /// </summary>
    /// <param name="values">Значения.</param>
    /// <returns>Фильтр.</returns>
    public QueryFilter NotIn<T>( [NotNullNotEmpty] params T[] values )
      where T : struct
    {
      return Set( QueryFilterComparison.NotIn, values );
    }
    #endregion

    #region NotLike
    /// <summary>
    /// Задает критерий фильтрации "Не похоже на".
    /// </summary>
    /// <param name="value">Значение.</param>
    /// <returns>Фильтр.</returns>
    public QueryFilter NotLike( [NotNull] string value )
    {
      return Set( QueryFilterComparison.NotLike, value );
    }
    #endregion

    #region Set
    internal QueryFilter Set( QueryFilterComparison comparison, object value )
    {
      Comparison = comparison;
      Value = value;

      return _filter;
    }
    #endregion

    #region ToString

    private string FormatName()
    {
      if( Name.Contains( "[" ) || Name.Contains( "." ) )
        return Name;
      else
        return "[" + Name + "]";
    }

    /// <summary>
    /// Возвращает строковое представление критерия фильтрации.
    /// </summary>
    /// <returns>Строковое представление критерия фильтрации.</returns>
    public override string ToString()
    {
      var format = new QueryFilterCriterionFormat( this, FormatName(), _names[Comparison], Value is Array ? "(" + string.Join( ",", (Value as Array).Cast<object>().Select( o => ToString( o ) ) ) + ")" : ToString( Value ) );

      if( CriterionFormatter != null )
        CriterionFormatter( format );

      return string.Format( "{0} {1} {2}", format.Name, format.Comparison, format.Value );
    }

    internal static string ToString( object value )
    {
      if( value is ValueText )
        value = ((ValueText) value).Value;
      return
        value == null ? "null"
        :
        (
        (value is string || value is char) ? ("'" + value.ToString().Replace( "'", "''" ) + "'") 
        :
        (
        value is DateTime ? string.Format( "convert(datetime, '{0}', 121)", ((DateTime) value).ToString( "yyyy-MM-dd HH:mm:ss" ) ) 
        :
        (
        value is bool ? (((bool) value) ? "1" : "0")
        :
        string.Format( CultureInfo.InvariantCulture, "{0}", value )
        )
        )
        );
    }
    #endregion
  }


  [Serializable]
  [DataContract( Namespace = Constants.QUERY_FILTER_NAMESPACE )]
  public struct InjectText
  {

    public static InjectText Create( string value )
    {
      return new InjectText() { Value = value };
    }

    [DataMember]
    public string Value { get; set; }

    public override string ToString()
    {
      return Value;
    }
  }

  [Serializable]
  [DataContract( Namespace = Constants.QUERY_FILTER_NAMESPACE )]
  public struct ValueText
  {

    public static ValueText Create( object value )
    {
      if( value == null )
        return new ValueText();
      if( value is ValueText )
        return (ValueText) value;
      return new ValueText()
      {
        Value =
          value is DateTime
          ?
          ((DateTime) value).ToUniversalTime()
          :
          value
      };
    }

    [DataMember]
    public object Value { get; set; }
  }
}
