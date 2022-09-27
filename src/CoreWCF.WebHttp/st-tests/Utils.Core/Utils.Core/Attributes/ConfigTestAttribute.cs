namespace ST.Utils.Attributes
{
  /// <summary>
  /// Атрибут, указывающий полное название типа и название метода, выполняющего тест, и вызываемого из конфигурационной консоли.
  /// </summary>
  [Serializable]
  [AttributeUsage( AttributeTargets.Class, Inherited = true, AllowMultiple = true )]
  public class ConfigTestAttribute : Attribute
  {
    public string TypeFullName { get; private set; }
    public string StaticMethodName { get; private set; }
    public string DisplayName { get; private set; }
    //public string Description { get; private set; }
    public ConfigTestAttribute( string typeFullName, string staticMethodName, string displayName )
    {
      TypeFullName = typeFullName;
      StaticMethodName = staticMethodName;
      DisplayName = displayName;
    }
    //public ConfigTestAttribute( string classAndStaticMethodName, string displayName, string description )
    //{
    //  ClassAndStaticMethodName = classAndStaticMethodName;
    //  DisplayName = displayName;
    //  Description = description;
    //}
  }
}
