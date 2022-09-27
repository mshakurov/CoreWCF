using ST.Utils.Attributes;

namespace ST.Utils
{
  /// <summary>
  /// Cтроковое представление частей критерия фильтрации элементов.
  /// </summary>
  public sealed class QueryFilterCriterionFormat
  {
    #region .Properties
    /// <summary>
    /// Критерий фильтрации элементов.
    /// </summary>
    public QueryFilterCriterion Criterion { get; private set; }

    /// <summary>
    /// Строковое представление названия элемента.
    /// </summary>
    [NotNullNotEmpty]
    public string Name { get; set; }

    /// <summary>
    /// Строковое представление оператора сравнения.
    /// </summary>
    [NotNullNotEmpty]
    public string Comparison { get; set; }

    /// <summary>
    /// Строковое представление значения элемента.
    /// </summary>
    [NotNullNotEmpty]
    public string Value { get; set; }
    #endregion

    #region .Ctor
    internal QueryFilterCriterionFormat( QueryFilterCriterion сriterion, string name, string comparison, string value )
    {
      Criterion = сriterion;
      Name = name;
      Comparison = comparison;
      Value = value;
    }
    #endregion
  }
}
