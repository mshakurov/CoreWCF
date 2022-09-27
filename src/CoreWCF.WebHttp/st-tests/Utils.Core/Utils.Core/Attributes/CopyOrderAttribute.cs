namespace ST.Utils.Attributes
{
  /// <summary>
  /// Атрибут, определяющий порядок свойств при копировании объектов.
  /// Свойства, помеченные этим атрибутом будут копироваться перед свойствами, не помеченными этим атрибутом.
  /// Порядок копирования свойств, помеченных этим атрибутом, определяется параметром order, передаваемым в конструктор (по возрастанию).
  /// Порядок копирования свойств, не помеченных этим атрибутом, определяется по их названиям (по возрастанию).
  /// </summary>
  [Serializable]
  [AttributeUsage( AttributeTargets.Property, AllowMultiple = false, Inherited = false )]
  public sealed class CopyOrderAttribute : Attribute
  {
    #region .Properties
    /// <summary>
    /// Порядок копирования свойства.
    /// </summary>
    public uint Order { get; private set; }
    #endregion

    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="order">Порядок копирования свойства.</param>
    public CopyOrderAttribute( uint order )
    {
      Order = order;
    }
    #endregion
  }
}
