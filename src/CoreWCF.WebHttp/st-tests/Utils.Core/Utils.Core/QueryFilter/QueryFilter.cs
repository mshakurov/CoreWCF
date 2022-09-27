using System;
using System.Runtime.Serialization;
using ST.Utils.Attributes;
using System.Xml.Serialization;

namespace ST.Utils
{
  /// <summary>
  /// Фильтр элементов.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Constants.QUERY_FILTER_NAMESPACE )]
  [KnownType( typeof( QueryFilterCriterion ) ), KnownType( typeof( QueryFilterCondition ) )]
  public sealed class QueryFilter : QueryFilterNode
  {
    public static QueryFilter Empty = new QueryFilter();

    #region .Properties
    /// <summary>
    /// Корневой узел фильтра.
    /// </summary>
    [DataMember]
    public QueryFilterNode Node { get; private set; }

    /// <summary>
    /// Дополлнительный коментарий, добавляемый к запросу без обработки, как есть.
    /// </summary>
    [DataMember]
    public string Comment { get; private set; }

    [IgnoreDataMember, XmlIgnore]
    public bool IsEmpty
    {
      get
      {
        return Node == null;
      }
    }
    #endregion

    #region .Ctor
    private QueryFilter()
    {
    }
    #endregion

    #region And
    /// <summary>
    /// Добавляет критерий фильтрации через логическое условие "И".
    /// </summary>
    /// <param name="name">Название элемента.</param>
    /// <returns>Критерий фильтрации.</returns>
    public QueryFilterCriterion And( string name )
    {
      return AddCondition<QueryFilterAnd>( name );
    }

    /// <summary>
    /// Добавляет критерий фильтрации через логическое условие "И".
    /// </summary>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Текущий фильтр.</returns>
    public QueryFilter And( QueryFilter filter )
    {
      return AddCondition<QueryFilterAnd>( filter );
    }
    #endregion

    #region AddCondition
    private QueryFilterCriterion AddCondition<T>( [NotNull] string name )
      where T : QueryFilterCondition, new()
    {
      var criterion = new QueryFilterCriterion( this, name );

      Node = new T { Left = Node, Right = criterion };

      return criterion;
    }

    private QueryFilter AddCondition<T>( [NotNull] QueryFilter filter )
      where T : QueryFilterCondition, new()
    {
      Node = new T { Left = Node, Right = filter };

      return this;
    }
    #endregion

    #region Group
    /// <summary>
    /// Группирует узлы текущего фильтра в новый фильтр.
    /// </summary>
    /// <returns>Фильтр.</returns>
    public QueryFilter Group()
    {
      return new QueryFilter { Node = this };
    }
    #endregion

    #region Or
    /// <summary>
    /// Добавляет критерий фильтрации через логическое условие "ИЛИ".
    /// </summary>
    /// <param name="name">Название элемента.</param>
    /// <returns>Критерий фильтрации.</returns>
    public QueryFilterCriterion Or( string name )
    {
      return AddCondition<QueryFilterOr>( name );
    }

    /// <summary>
    /// Добавляет критерий фильтрации через логическое условие "ИЛИ".
    /// </summary>
    /// <param name="filter">Фильтр.</param>
    /// <returns>Текущий фильтр.</returns>
    public QueryFilter Or( QueryFilter filter )
    {
      return AddCondition<QueryFilterOr>( filter );
    }
    #endregion

    #region SetComment
    /// <summary>
    /// Устанавливает дополлнительный коментарий, добавляемый к запросу без обработки, как есть.
    /// </summary>
    /// <param name="comment">Дополлнительный коментарий, добавляемый к запросу без обработки, как есть.</param>
    /// <returns>Текущий фильтр</returns>
    public QueryFilter SetComment( string comment )
    {
      Comment = comment;

      return this;
    } 
    #endregion

    #region ToString
    /// <summary>
    /// Возвращает строковое представление фильтра.
    /// </summary>
    /// <returns>Строковое представление фильтра.</returns>
    public override string ToString()
    {
      if ( Node == null )
        return "";

      Node.CriterionFormatter = CriterionFormatter;
      Node.ConditionFormatter = ConditionFormatter;

      if ( string.IsNullOrWhiteSpace( Comment ) )
        return Node.ToString();

      return Comment + " " + Node.ToString();
    }

    /// <summary>
    /// Возвращает строковое представление фильтра.
    /// </summary>
    /// <param name="criterionFormatter">Метод, задающий строковые представления составляющих частей критерия фильтрации.</param>
    /// <returns>Строковое представление фильтра.</returns>
    public string ToString( Action<QueryFilterCriterionFormat> criterionFormatter )
    {
      return ToString( criterionFormatter, null );
    }

    /// <summary>
    /// Возвращает строковое представление фильтра.
    /// </summary>
    /// <param name="criterionFormatter">Метод, задающий строковые представления составляющих частей критерия фильтрации.</param>
    /// <param name="conditionFormatter">Метод, возвращающий строковое представление логического условия фильтрации.</param>
    /// <returns>Строковое представление фильтра.</returns>
    public string ToString( Action<QueryFilterCriterionFormat> criterionFormatter, Func<QueryFilterCondition, string> conditionFormatter )
    {
      CriterionFormatter = criterionFormatter;
      ConditionFormatter = conditionFormatter;

      return ToString();
    }
    #endregion

    #region Unwrap
    /// <summary>
    /// Преобразует все критерии фильтрации типа "Входит" в последовательность критериев фильтрации "Равно", объединенных логических условием "ИЛИ" и
    /// все критерии фильтрации типа "Не входит" в последовательность критериев фильтрации "Не равно", объединенных логических условием "И".
    /// </summary>
    /// <returns>Текущий фильтр.</returns>
    public QueryFilter Unwrap()
    {
      Node = Unwrap( Node );

      return this;
    }

    private QueryFilterNode Unwrap( QueryFilterNode node )
    {
      if ( node is QueryFilterCriterion )
      {
        var crit = node as QueryFilterCriterion;

        if ( crit.Value is Array && crit.Comparison.In( QueryFilterComparison.In, QueryFilterComparison.NotIn ) )
        {
          var values = crit.Value as Array;

          if ( values.Length > 0 )
          {
            QueryFilter filter = null;

            foreach ( var value in values )
            {
              var comp = crit.Comparison == QueryFilterComparison.In ? QueryFilterComparison.Eq : QueryFilterComparison.NotEq;

              if ( filter == null )
                filter = QueryFilter.Where( crit.Name ).Set( comp, value );
              else
                ( comp == QueryFilterComparison.Eq ? filter.Or( crit.Name ) : filter.And( crit.Name ) ).Set( comp, value );
            }

            return filter;
          }
        }
      }
      else
        if ( node is QueryFilterCondition )
        {
          var cond = node as QueryFilterCondition;

          cond.Left = Unwrap( cond.Left );
          cond.Right = Unwrap( cond.Right );
        }
        else
          if ( node is QueryFilter )
            ( node as QueryFilter ).Node = Unwrap( ( node as QueryFilter ).Node );

      return node;
    }
    #endregion

    #region Where
    /// <summary>
    /// Создает фильтр.
    /// </summary>
    /// <param name="name">Название элемента.</param>
    /// <returns>Критерий фильтрации.</returns>
    public static QueryFilterCriterion Where( [NotNull] string name )
    {
      var f = new QueryFilter();

      return ( f.Node = new QueryFilterCriterion( f, name ) ) as QueryFilterCriterion;
    }
    #endregion
  }
}
