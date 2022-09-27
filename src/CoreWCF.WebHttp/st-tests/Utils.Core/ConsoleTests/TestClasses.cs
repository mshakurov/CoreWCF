using ST.Utils.Attributes;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTestsClasses
{
  public class TestCount
  {
    private int totalCount;
    private int successCount;

    public int TotalCount => totalCount;
    public int SuccessCount => successCount;

    public void Calc(bool? result)
    {
      totalCount++;
      if (result ?? false)
        successCount++;
    }

    public TestCount Calc(TestCount? result)
    {
      if (result != null)
      {
        totalCount += result.totalCount;
        successCount += result.successCount;
      }
      return this;
    }

    public static TestCount operator +(TestCount left, TestCount right)
    {
      return left.Calc(right);
    }

    public override string ToString()
    {
      return $"Success: {successCount} of {totalCount}";
    }
  }

  public class TestNotNull
  {
    bool showTestConsoleOutput;
    public TestNotNull(bool showTestConsoleOutput)
    {
      this.showTestConsoleOutput = showTestConsoleOutput;
    }

    public void Test1([NotNull] string? value)
    {
      if (showTestConsoleOutput)
        Console.WriteLine($"Echo from {nameof(TestNotNull)}.{nameof(TestNotNull.Test1)}. Arg '{nameof(value)}: {value ?? "[null]"}')");
    }
  }

  public class TestResult<TResult>
  {
    public bool Success { get; set; }
    public TResult? Result { get; set; }
  }
}
