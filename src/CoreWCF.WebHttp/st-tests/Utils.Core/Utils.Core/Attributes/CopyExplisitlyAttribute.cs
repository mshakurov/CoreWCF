namespace ST.Utils.Attributes
{
  /// <summary>
  /// Атрибут, определяющий необходимость копирования поля или свойства средствами PropertyCopier.
  /// </summary>
  [Serializable]
  [AttributeUsage( AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false )]
  public sealed class CopyExplisitlyAttribute : Attribute
  {
  }
}
