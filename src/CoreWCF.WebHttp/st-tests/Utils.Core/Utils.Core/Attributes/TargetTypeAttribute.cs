namespace ST.Utils.Attributes
{
  /// <summary>
  /// Атрибут, указывающий целевой тип.
  /// </summary>
  [Serializable]
  [AttributeUsage( AttributeTargets.Class, Inherited = true, AllowMultiple = true )]
  public sealed class TargetTypeAttribute : Attribute
  {
    #region .Properties
    /// <summary>
    /// Целевой тип.
    /// </summary>
    public Type Type { get; private set; }
    #endregion

    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="type">Целевой тип.</param>
    public TargetTypeAttribute( [NotNull] Type type )
    {
      Type = type;
    }
    #endregion
  }
}
