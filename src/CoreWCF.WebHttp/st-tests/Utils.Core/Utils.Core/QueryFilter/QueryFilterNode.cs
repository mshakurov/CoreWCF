using System;
using System.Runtime.Serialization;

namespace ST.Utils
{
  /// <summary>
  /// Базовый класс узла фильтра элементов.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Constants.QUERY_FILTER_NAMESPACE )]
  [KnownType( typeof( QueryFilterCriterion ) ), KnownType( typeof( QueryFilterCondition ) ), KnownType( typeof( QueryFilter ) )]
  public abstract class QueryFilterNode
  {
    #region .Properties
    [IgnoreDataMember]
    internal Action<QueryFilterCriterionFormat> CriterionFormatter { get; set; }

    [IgnoreDataMember]
    internal Func<QueryFilterCondition, string> ConditionFormatter { get; set; }
    #endregion
  }
}
