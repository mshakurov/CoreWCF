using System;
using System.Runtime.Serialization;

namespace ST.Utils
{
  /// <summary>
  /// Логическое условие фильтрации.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Constants.QUERY_FILTER_NAMESPACE )]
  [KnownType( typeof( QueryFilterAnd ) ), KnownType( typeof( QueryFilterOr ) ), KnownType( typeof( QueryFilterCriterion ) ), KnownType( typeof( QueryFilter ) )]
  public abstract class QueryFilterCondition : QueryFilterNode
  {
    #region .Properties
    /// <summary>
    /// Левая часть условия.
    /// </summary>
    [DataMember]
    public QueryFilterNode Left { get; internal set; }

    /// <summary>
    /// Правая часть условия.
    /// </summary>
    [DataMember]
    public QueryFilterNode Right { get; internal set; }

    [IgnoreDataMember]
    protected abstract string Condition { get; }
    #endregion

    #region ToString
    /// <summary>
    /// Возвращает строковое представление условия фильтрации.
    /// </summary>
    /// <returns>Строковое представление условия фильтрации.</returns>
    public override string ToString()
    {
      Left.CriterionFormatter = Right.CriterionFormatter = CriterionFormatter;
      Left.ConditionFormatter = Right.ConditionFormatter = ConditionFormatter;

      var condition = ConditionFormatter == null ? null : ConditionFormatter( this );

      return string.Format( "{0} {1} {2}", ToString( Left ), condition ?? Condition, ToString( Right ) );
    }

    private static string ToString( QueryFilterNode node )
    {
      return node is QueryFilter ? "(" + node + ")" : node.ToString();
    }
    #endregion
  }

  /// <summary>
  /// Логическое условие фильтрации "И".
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Constants.QUERY_FILTER_NAMESPACE )]
  public sealed class QueryFilterAnd : QueryFilterCondition
  {
    #region .Properties
    [IgnoreDataMember]
    protected override string Condition
    {
      get { return "and"; }
    }
    #endregion
  }

  /// <summary>
  /// Логическое условие фильтрации "ИЛИ".
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Constants.QUERY_FILTER_NAMESPACE )]
  public sealed class QueryFilterOr : QueryFilterCondition
  {
    #region .Properties
    [IgnoreDataMember]
    protected override string Condition
    {
      get { return "or"; }
    }
    #endregion
  }
}
