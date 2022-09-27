using System;
using System.Runtime.Serialization;
using ST.Utils.Attributes;

namespace ST.Utils
{
  /// <summary>
  /// Операторы сравнения для фильтра элементов.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Constants.QUERY_FILTER_NAMESPACE )]
  public enum QueryFilterComparison
  {
    #region .Static Fields
    /// <summary>
    /// Оператор "Равно".
    /// </summary>
    [DisplayNameLocalized( "=" )]
    [EnumMember]
    Eq,

    /// <summary>
    /// Оператор "Больше".
    /// </summary>
    [DisplayNameLocalized( ">" )]
    [EnumMember]
    Gr,

    /// <summary>
    /// Оператор "Больше или равно".
    /// </summary>
    [DisplayNameLocalized( ">=" )]
    [EnumMember]
    GrOrEq,

    /// <summary>
    /// Оператор "Входит".
    /// </summary>
    [DisplayNameLocalized( "in" )]
    [EnumMember]
    In,

    /// <summary>
    /// Оператор "Меньше".
    /// </summary>
    [DisplayNameLocalized( "<" )]
    [EnumMember]
    Le,

    /// <summary>
    /// Оператор "Меньше или равно".
    /// </summary>
    [DisplayNameLocalized( "<=" )]
    [EnumMember]
    LeOrEq,

    /// <summary>
    /// Оператор "Похоже на".
    /// </summary>
    [DisplayNameLocalized( "like" )]
    [EnumMember]
    Like,

    /// <summary>
    /// Оператор "Не равно".
    /// </summary>
    [DisplayNameLocalized( "!=" )]
    [EnumMember]
    NotEq,

    /// <summary>
    /// Оператор "Не входит".
    /// </summary>
    [DisplayNameLocalized( "not in" )]
    [EnumMember]
    NotIn,

    /// <summary>
    /// Оператор "Не похоже на".
    /// </summary>
    [DisplayNameLocalized( "not like" )]
    [EnumMember]
    NotLike
    #endregion
  }
}
