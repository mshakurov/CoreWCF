namespace ConsoleTestAssembly
{
  public class BaseClass1
  {
    public int IntProp1 { get; set; }

    public string? StrProp2 { get; set; }

    private DateTime? DateTimeProp3 { get; set; }

  }

  public class Class1 : BaseClass1
  {
    public int IntProp4 { get; set; }

    public string? StrProp5 { get; set; }

    private DateTime? DateTimeProp6 { get; set; }

  }


  public class ChildOfClass1 : Class1
  {
    public int IntProp7 { get; set; }

    public string? StrProp8 { get; set; }

    private DateTime? DateTimeProp9 { get; set; }

  }

}