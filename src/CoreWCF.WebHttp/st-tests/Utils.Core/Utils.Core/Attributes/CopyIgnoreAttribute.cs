namespace ST.Utils.Attributes
{
  /// <summary>
  /// Атрибут, исключающий свойство при копировании средствами PropertyCopier.
  /// </summary>
  [Serializable]
  [AttributeUsage( AttributeTargets.Property, AllowMultiple = false, Inherited = false )]
  public sealed class CopyIgnoreAttribute : Attribute
  {
  }
}
