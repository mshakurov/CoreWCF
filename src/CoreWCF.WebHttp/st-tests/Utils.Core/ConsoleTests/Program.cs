// See https://aka.ms/new-console-template for more information
using ConsoleTests;

using ConsoleTestsClasses;

using ConsoleTestsDummy;

using ConsoleTestsSerialize;

using ST.BusinessEntity.Server;
using ST.Monitoring.Server.Entities;
using ST.Ramp.Server.Objects;
using ST.Utils;
using ST.Utils.Attributes;

using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Xml.Linq;

Console.WriteLine("Hello, World!");

bool ShowTestConsoleOutput = false,
    BreakOn1stFail = false;
string dbServer = "194.87.34.14";
bool isPG = true;
bool dbIntegratedSecurity = false;
string dbUser = "admin2";
string dbPass = "123QWEasd!";
bool isCP = true;
string vehicleChild = "VehicleCP"; // "ShipREKA"; // VehicleRamp";

{
  TestCount successCount = new();

  int TestCount = 100;

  TestCount = ConsoleEnter($"Enter test count [{TestCount}]:\r\n(1 = detailed output): ",
    sValue =>
    {
      if (string.IsNullOrWhiteSpace(sValue))
        return (true, false, TestCount);
      if (int.TryParse(sValue, out TestCount) && TestCount > 0)
        return (true, false, TestCount);
      Console.WriteLine($"Invalid test count. Should be between 1 and {int.MaxValue}");
      return (false, false, -1);
    });

  if (TestCount == 1)
    ShowTestConsoleOutput = true;
  else
  {
    if (ConsoleEnterBool($"Should stop on 1st FAIL and test agian with details? <y/n> (default y): ", true))
      BreakOn1stFail = true;
  }

  dbServer = ConsoleEnter($"Enter DB Server Host/IP: (defaule = '{dbServer}')",
    sValue =>
    {
      if (string.IsNullOrWhiteSpace(sValue))
        return (true, false, dbServer);
      return (true, false, sValue.Trim());
    });

  isPG = ConsoleEnterBool($"Is PG? <y/n> (default {(isPG ? "y" : "n")}): ", isPG);

  dbUser = ConsoleEnter($"Enter DB User (default - '{dbUser}'{(isPG ? "" : ", ! - integrated security")}): ",
    sValue =>
    {
      if (string.IsNullOrWhiteSpace(sValue))
        return (true, false, dbUser);
      sValue = sValue.Trim();
      if (isPG)
        dbIntegratedSecurity = false;
      else
      if (sValue == "!")
        dbIntegratedSecurity = true;
      return (true, false, sValue.Trim());
    });

  if (!dbIntegratedSecurity)
    dbPass = ConsoleEnter($"Enter DB Pass (default - '{dbUser}' password): ",
      sValue =>
      {
        if (string.IsNullOrWhiteSpace(sValue))
          return (true, false, dbPass);
        return (true, false, sValue.Trim());
      });

  isCP = !isPG && ConsoleEnterBool($"db names prefix is CP_ ?: <y/n> (default - {(isCP ? "y" : "n")}): ", isCP);

  vehicleChild = ConsoleEnter($"Vehicle highest child code: (default - '{vehicleChild}')", sValue =>
    {
      if (string.IsNullOrWhiteSpace(sValue))
        return (true, false, vehicleChild);
      return (true, false, sValue.Trim());
    }).Trim('[', ']', ' ');
  vehicleChild = isPG ? vehicleChild.ToLower() : vehicleChild;

  Console.WriteLine($"dbServer: {dbServer}");
  if (dbIntegratedSecurity)
    Console.WriteLine($"dbIntegratedSecurity: {dbIntegratedSecurity}");
  else
  {
    Console.WriteLine($"dbUser: {dbUser}");
    Console.WriteLine($"dbPass: {dbPass}");
  }

  Stopwatch swTotal = new Stopwatch();
  Stopwatch swShow = Stopwatch.StartNew();

  int msgLastLen = 0;
  //[SupportedOSPlatform("windows")]
  void ShowInfo(int iTest, bool force = false)
  {
    //if (!force && !OperatingSystem.IsWindows())
    //  return;
    if (TestCount < 2) return;
    if (force || swShow.ElapsedMilliseconds >= 1000)
    {
      swShow.Restart();

      Console.Write(successCount.SuccessCount == successCount.TotalCount ? "." : "#");

      var left = Console.CursorLeft;
      var top = Console.CursorTop;

      var msg = $"{Math.Round((iTest * 100.0) / TestCount):#,0}%,Speed:{(swTotal.Elapsed.Seconds > 0 ? successCount.TotalCount / swTotal.Elapsed.Seconds : 0):#,0.0}";
      if (msgLastLen > 0)
      {
        Console.Write(new String(' ', msgLastLen));
        Console.CursorLeft = left;
        Console.CursorTop = top;
      }
      Console.Write(msg);
      msgLastLen = msg.Length;

      Console.CursorLeft = left;
      Console.CursorTop = top;
    }
  }

  Console.WriteLine();

  ShowInfo(0, true);

  for (int iTest = 0; iTest < TestCount; iTest++)
  {
    swTotal.Start();

    //successCount += TestExecTryVoid();

    //successCount += TestExecTryReturn();

    //successCount += TestValidator();

    //successCount += TestCopyFastTestDummy();

    //successCount += TestCopyFastTestDummyChild();

    //successCount += TestConvertUpDown();

    //successCount += TestSerialize();

    //successCount += TestMemoryHelper();

    //successCount += TestMemberHeler();

    //successCount += TestEnvHelper();

    //successCount += TestNetworkInfo();

    //successCount += TestDbi();

    successCount += TestIAspectProvider();

    swTotal.Stop();

    ShowInfo(iTest + 1);

    if (successCount.SuccessCount != successCount.TotalCount)
    {
      if (BreakOn1stFail)
      {
        if (ShowTestConsoleOutput)
          break;
        ShowTestConsoleOutput = true;
      }
    }
    else
    {
      if (BreakOn1stFail && ShowTestConsoleOutput)
        ShowTestConsoleOutput = false;
    }
  }

  ShowInfo(TestCount, true);

  Console.WriteLine();
  Console.WriteLine($"Speed: {(swTotal.Elapsed.Seconds > 0 ? successCount.TotalCount / swTotal.Elapsed.Seconds : 0):#,0.0} per second");

  Console.WriteLine("--------------------");
  Console.WriteLine($"{(successCount.SuccessCount == successCount.TotalCount ? "!!!" : "###")} {successCount}");
  Console.WriteLine("--------------------");
}

Console.WriteLine("Press any key...");
Console.ReadKey();

TResult ConsoleEnter<TResult>(string prompt, Func<string, (bool success, bool cancel, TResult result)> check)
{
  while (true)
  {
    Console.WriteLine(prompt);
    string sValue = Console.ReadLine() ?? string.Empty;
    var result = check(sValue);
    if (result.success)
      return result.result;
    if (result.cancel)
      break;
  }
  return default(TResult)!;
}

bool ConsoleEnterBool(string prompt, bool defaultValue)
{
  return ConsoleEnter(prompt,
      sValue =>
      {
        if (string.IsNullOrWhiteSpace(sValue))
          return (true, false, defaultValue);
        if (sValue.ToLowerInvariant().In("y", "n"))
          return (true, false, sValue.ToLowerInvariant() == "y");
        Console.WriteLine($"# Invalid choise. Should be 'y' or 'n'");
        return (false, false, !defaultValue);
      });
}

void TestVoidC(TestCount counter, string testName, bool shouldExcept, Action act)
{
  counter.Calc(TestVoid(testName, shouldExcept, act));
}

bool TestVoid(string testName, bool shouldExcept, Action act)
{
  try
  {
    act();

    if (ShowTestConsoleOutput)
      if (shouldExcept)
        Console.WriteLine($"#. (Test '{testName}'. ### FAILED - No Exception).");
      else
        Console.WriteLine($"!. (Test '{testName}'. !!! Success - No Exception).");

    return !shouldExcept;
  }
  catch (Exception ex)
  {
    if (ShowTestConsoleOutput)
      if (shouldExcept)
        Console.WriteLine($"!. (Test '{testName}'. !!! Success - Exception: {Environment.NewLine}{ex.GetFullMessage()}).");
      else
        Console.WriteLine($"#. (Test '{testName}'. ### FAILED - Exception: {Environment.NewLine}{ex.GetFullMessage()}).");

    return shouldExcept;
  }
}

TestResult<TResult> TestReturnC<TResult>(TestCount counter, string testName, bool checkValue, TResult? shuldValue, bool shouldExcept, Func<TResult?> act)
{
  TestResult<TResult> result = TestReturn(testName, checkValue, shuldValue, shouldExcept, act) ?? new();
  counter.Calc(result?.Success);
  return result!;
}

TestResult<TResult> TestReturn<TResult>(string testName, bool checkValue, TResult? shuldValue, bool shouldExcept, Func<TResult?> act)
{
  TestResult<TResult> result = new();

  Func<bool, bool, string> ChecktotalResult = (excSucc, eq) => excSucc && eq ? "!" : "#";

  try
  {
    result.Result = act();

    bool eq = checkValue ? object.Equals(result.Result, shuldValue) : true;

    Func<string> CheckValueResult = () => checkValue ? $" Result check: {(eq ? "SUCCESS" : "### FAILED")}" : String.Empty;

    result.Success = !shouldExcept && eq;

    if (ShowTestConsoleOutput)
      if (shouldExcept)
        Console.WriteLine($"{ChecktotalResult(false, eq)}. (Test Test '{testName}'. Exception check: ### FAILED - No Exception.{CheckValueResult()}).");
      else
        Console.WriteLine($"{ChecktotalResult(true, eq)}. (Test '{testName}'. Exception check: !!! Success - No Exception.{CheckValueResult()}).");
  }
  catch (Exception ex)
  {
    result.Success = shouldExcept;

    if (ShowTestConsoleOutput)
      if (shouldExcept)
        Console.WriteLine($"{ChecktotalResult(true, true)}. (Test '{testName}'. Exception check: !!! Success - Exception: {Environment.NewLine}{ex.GetFullMessage()}).");
      else
        Console.WriteLine($"{ChecktotalResult(false, true)}. (Test Test '{testName}'. Exception check: ### FAILED - Exception: {Environment.NewLine}{ex.GetFullMessage()}).");
  }

  return result;
}

TestCount TestExecTryVoid()
{
  var cnt = new TestCount();

  TestVoidC(cnt, $"{nameof(Exec)}{nameof(Exec.Try)} void, rethrow = default", false, () =>
  {
    Exec.Try(() =>
    {
      throw new Exception("testing exception 1");
    });
  });

  TestVoidC(cnt, $"{nameof(Exec)}{nameof(Exec.Try)} void, rethrow = false", false, () =>
  {
    Exec.Try(() =>
    {
      throw new Exception("testing exception 2");
    }, false);
  });

  TestVoidC(cnt, $"{nameof(Exec)}{nameof(Exec.Try)} void, rethrow = true", true, () =>
  {
    Exec.Try(() =>
    {
      throw new Exception("testing exception 3");
    }, true);
  });

  return cnt;
}

TestCount TestExecTryReturn()
{
  var cnt = new TestCount();

  TestReturnC(cnt, $"{nameof(Exec)}{nameof(Exec.Try)} return, throw and catch exception 1", false, "", false, () =>
 {
   var result = Exec.Try(() =>
   {
     throw new Exception("testing exception 1");

     //return "Result";
   }, ex => $"### Exec.Try Exception: {ex.Message}");

   return result;
 });

  TestReturnC(cnt, $"{nameof(Exec)}{nameof(Exec.Try)} return, div by zero", false, "", true, () =>
 {
   var result = Exec.Try(() =>
   {
     int i1 = 10, i2 = 0;
     var i3 = i1 / i2;

     return "Result";
   }, ex => $"### Exec.Try Exception: {ex.Message}");

   return result;
 });


  TestReturnC(cnt, $"{nameof(Exec)}{nameof(Exec.Try)} return", true, "Result", false, () =>
 {
   var result = Exec.Try(() =>
   {
     return "Result";
   }, ex => $"### Exec.Try Exception: {ex.Message}");

   return result;
 });

  return cnt;
}

TestCount TestValidator()
{
  var cnt = new TestCount();

  TestVoidC(cnt, "[NotNull]", true, () =>
  {
    var t1 = new TestNotNull(ShowTestConsoleOutput);
    t1.Test1(null);
  });

  TestVoidC(cnt, "[NotNull]", false, () =>
  {
    var t1 = new TestNotNull(ShowTestConsoleOutput);
    t1.Test1("123");
  });

  return cnt;
}

TestCount TestCopyFastTestDummy()
{
  var cnt = new TestCount();

  TestReturnC(cnt, "TestDummy == (self) ", true, true, false, () =>
 {
   var d1 = new TestDummy();

   var d2 = d1;

   return d1 == d2;
 });

  TestReturnC(cnt, "TestDummy == (by TestClone) ", true, true, false, () =>
 {
   var d1 = new TestDummy();

   var d2 = d1.TestClone();

   return d1 == d2;
 });

  TestReturnC(cnt, "TestDummy != ", true, true, false, () =>
 {
   var d1 = new TestDummy();

   var d2 = new TestDummy();

   while (d2.IntProperty == d1.IntProperty)
     d2.IntProperty = TestDummy.Random.Next();

   return d1 != d2;
 });

  TestReturnC(cnt, "TestDummy.Equals (by TestClone)", true, true, false, () =>
 {
   var d1 = new TestDummy();

   var d2 = d1.TestClone();

   return d1.Equals(d2);
 });

  TestReturnC(cnt, "CopyFast (for TestDummy) == ", true, true, false, () =>
 {
   var d1 = new TestDummy();

   var d2 = new TestDummy();

   d1.CopyFast(d2);

   return d1 == d2!;
 });

  TestReturnC(cnt, "CopyFast(create) (for TestDummy) == ", true, true, false, () =>
 {
   var d1 = new TestDummy();

   var d2 = d1.CopyFast();

   return d1 == d2;
 });

  TestReturnC(cnt, "CopyFast(create).Equals  (for TestDummy)", true, true, false, () =>
 {
   var d1 = new TestDummy();

   var d2 = d1.CopyFast();

   return d1.Equals(d2);
 });

  return cnt;
}

TestCount TestCopyFastTestDummyChild()
{
  var cnt = new TestCount();

  TestReturnC(cnt, "TestDummyChild == (self) ", true, true, false, () =>
 {
   var d1 = new TestDummyChild();

   var d2 = d1;

   return d1 == d2;
 });

  TestReturnC(cnt, "TestDummyChild == (by TestClone) ", true, true, false, () =>
 {
   var d1 = new TestDummyChild();

   var d2 = d1.TestClone();

   return d1 == d2;
 });

  TestReturnC(cnt, "TestDummyChild != ", true, true, false, () =>
 {
   var d1 = new TestDummyChild();

   var d2 = new TestDummyChild();

   while (d2.IntProperty == d1.IntProperty)
     d2.IntProperty = TestDummyChild.Random.Next();

   return d1 != d2;
 });

  TestReturnC(cnt, "TestDummyChild.Equals (by TestClone)", true, true, false, () =>
 {
   var d1 = new TestDummyChild();

   var d2 = d1.TestClone();

   return d1.Equals(d2);
 });

  TestReturnC(cnt, "CopyFast (for TestDummyChild cast to TestDummy) == ", true, true, false, () =>
 {
   var d1 = new TestDummyChild();

   var d2 = new TestDummyChild();

   d1.CopyFast(d2);

   return (TestDummy)d1 == (TestDummy)d2!;
 });

  TestReturnC(cnt, "CopyFast(create) (for TestDummyChild) == cast to TestDummy", true, true, false, () =>
 {
   var d1 = new TestDummyChild();

   var d2 = d1.CopyFast();

   return (TestDummy)d1 == (TestDummy)d2!;
 });

  TestReturnC(cnt, "CopyFast(create) (for TestDummyChild) == no cast", true, false, false, () =>
 {
   var d1 = new TestDummyChild();

   var d2 = d1.CopyFast();

   return d1 == d2!;
 });

  TestReturnC(cnt, "CopyFast(create) (for TestDummyChild) != no cast", true, true, false, () =>
 {
   var d1 = new TestDummyChild();

   var d2 = d1.CopyFast();

   return d1 != d2!;
 });

  TestReturnC(cnt, "CopyFast(create).Equals (for TestDummyChild cast to TestDummy)", true, true, false, () =>
 {
   var d1 = new TestDummyChild();

   var d2 = d1.CopyFast();

   return ((TestDummy)d1).Equals((TestDummy)d2!);
 });

  TestReturnC(cnt, "CopyFast(create).Equals (for TestDummyChild no cast)", true, false, false, () =>
 {
   var d1 = new TestDummyChild();

   var d2 = d1.CopyFast();

   return d1.Equals(d2!);
 });

  return cnt;
}

TestCount TestConvertUpDown()
{
  var cnt = new TestCount();

  TestReturnC(cnt, "TestDummy == ConvertDown<TestDummyChild>(self) ", true, true, false, () =>
 {
   var d1 = new TestDummy();

   var d2 = d1.ConvertDown<TestDummy, TestDummyChild>();

   return d1 == d2;
 });

  TestReturnC(cnt, "TestDummy.Equals ConvertDown<TestDummyChild>(self) ", true, true, false, () =>
 {
   var d1 = new TestDummy();

   var d2 = d1.ConvertDown<TestDummy, TestDummyChild>();

   return d1.Equals(d2);
 });

  TestReturnC(cnt, "ConvertDown<TestDummyChild>(self).Equals TestDummy", true, true, false, () =>
 {
   var d1 = new TestDummy();

   var d2 = d1.ConvertDown<TestDummy, TestDummyChild>();

   return d2?.Equals(d1);
 });

  TestReturnC(cnt, "TestDummyChild == ConvertUp<TestDummy>(self) ", true, true, false, () =>
 {
   var d1 = new TestDummyChild();

   var d2 = d1.ConvertUp<TestDummyChild, TestDummy>();

   return d1 == d2;
 });

  TestReturnC(cnt, "TestDummyChild.Equals ConvertUp<TestDummy>(self) ", true, true, false, () =>
 {
   var d1 = new TestDummyChild();

   var d2 = d1.ConvertUp<TestDummyChild, TestDummy>();

   return d1.Equals(d2);
 });

  TestReturnC(cnt, "ConvertUp<TestDummy>(self).Equals TestDummyChild", true, true, false, () =>
 {
   var d1 = new TestDummyChild();

   var d2 = d1.ConvertUp<TestDummyChild, TestDummy>();

   return d2?.Equals(d1);
 });

  return cnt;
}

TestCount TestSerialize()
{
  var cnt = new TestCount();

  TestReturnC(cnt, "Serialize TestSerialize == ", true, true, false, () =>
   {
     var d1 = new TestSerialize();

     byte[] bytes = d1.Serialize();

     TestSerialize d2 = bytes.Deserialize<TestSerialize>();

     return d1 == d2;
   });

  TestReturnC(cnt, "Serialize TestSerialize Equals ", true, true, false, () =>
  {
    var d1 = new TestSerialize();

    byte[] bytes = d1.Serialize();

    TestSerialize d2 = bytes.Deserialize<TestSerialize>();

    return d1.Equals(d2);
  });

  TestReturnC(cnt, "Serialize TestSerializeChild == ", true, true, false, () =>
  {
    var d1 = new TestSerializeChild();

    byte[] bytes = d1.Serialize();

    TestSerializeChild d2 = bytes.Deserialize<TestSerializeChild>();

    return d1 == d2;
  });

  TestReturnC(cnt, "Serialize TestSerializeChild Equals ", true, true, false, () =>
  {
    var d1 = new TestSerializeChild();

    byte[] bytes = d1.Serialize();

    TestSerializeChild d2 = bytes.Deserialize<TestSerializeChild>();

    return d1.Equals(d2);
  });

  TestReturnC(cnt, "DeepClone TestSerializeChild Equals ", true, true, false, () =>
  {
    var d1 = new TestSerializeChild();

    var d2 = d1.DeepClone();

    return d1.Equals(d2);
  });

  return cnt;
}

TestCount TestMemoryHelper()
{
  var cnt = new TestCount();

  var res = TestReturnC<GCMemoryInfo?>(cnt, "GetGCMemoryInfo 1", false, (GCMemoryInfo?)null, false, () => GC.GetGCMemoryInfo());

  TestReturnC(cnt, "GetGCMemoryInfo 1 not null", true, true, false, () => res != null && res.Result != null);

  var mi1 = GC.GetGCMemoryInfo();
  var mi2 = GC.GetGCMemoryInfo();

  TestCompareMemoryInfo(cnt, "(no changess)", mi1, mi2, true);

  //--------
  TestVoidC(cnt, "Marshal.AllocHGlobal", false, () =>
  {
    IntPtr gl = IntPtr.Zero;
    try
    {
      gl = System.Runtime.InteropServices.Marshal.AllocHGlobal(1024 * 1024);
    }
    finally
    {
      System.Runtime.InteropServices.Marshal.FreeHGlobal(gl);
    }
  });

  //--------
  mi1 = GC.GetGCMemoryInfo();
  TestVoidC(cnt, "Create Many Objects", false, () =>
  {
    _ = Enumerable.Range(1, 1000).Select(_ => new TestDummyChild()).ToArray();

    mi2 = GC.GetGCMemoryInfo();

    TestCompareMemoryInfo(cnt, "(Many Objects)", mi1, mi2, false);
  });


  //--------
  mi1 = GC.GetGCMemoryInfo();

  TestVoidC(cnt, "MemoryHelper.Collect()", false, () => MemoryHelper.Collect());

  mi2 = GC.GetGCMemoryInfo();

  TestCompareMemoryInfo(cnt, "(collected)", mi1, mi2, false);

  return cnt;
}

void TestCompareMemoryInfo(TestCount cnt, string info, GCMemoryInfo? result1, GCMemoryInfo? result2, bool shouldEq)
{
  string? log = null;
  TestReturnC(cnt, $"Compare GetGCMemoryInfo {info}", true, shouldEq, false, () =>
  {
    var diffProps = GetDiffs(result1, result2, p => !p.Name.InCI("GenerationInfo", "PauseDurations"));

    var gi1 = result1?.GenerationInfo.ToArray() ?? Array.Empty<GCGenerationInfo>();
    var gi2 = result2?.GenerationInfo.ToArray() ?? Array.Empty<GCGenerationInfo>();
    var giDiffProps = gi1.Length == gi2.Length ? gi1.Select((g, i) => (g, i)).Join(gi2.Select((g, i) => (g, i)), g => g.i, g => g.i, (g1, g2) => GetDiffs(g1.g, g2.g, _ => true)).SelectMany(props => props).ToArray() : null;

    var pd1 = result1?.PauseDurations.ToArray() ?? Array.Empty<TimeSpan>();
    var pd2 = result2?.PauseDurations.ToArray() ?? Array.Empty<TimeSpan>();
    var pdPropsEq = pd1.Length == pd2.Length && pd1.SequenceEqual(pd2);

    bool success =
      shouldEq && !diffProps.Any() && giDiffProps != null && !giDiffProps.Any() && pdPropsEq
      ||
      !shouldEq && diffProps.Any() && !(giDiffProps != null && !giDiffProps.Any()) && !pdPropsEq
      ;
    if (diffProps.Any() || giDiffProps != null && giDiffProps.Any())
      if (ShowTestConsoleOutput)
      {
        log = $"  {(success ? "!" : "#")} diff by props: {string.Join(",", diffProps.Select(p => p.Name))}";
        if (giDiffProps != null)
          log += ". GenerationInfo: " + string.Join(",", giDiffProps.Select(p => p.Name));
      }
    return diffProps.Length == 0;
  });
  if (log != null)
    Console.WriteLine(log);

}

PropertyInfo[] GetDiffs(object? obj1, object? obj2, Func<PropertyInfo, bool> filter)
{
  if (obj1 == null || obj2 == null)
    return Array.Empty<PropertyInfo>();
  return obj1.GetType().GetProperties().Where(filter).Where(p => !object.Equals(p.GetValue(obj1, null), p.GetValue(obj2, null))).ToArray();
}

TestCount TestMemberHeler()
{
  var cnt = new TestCount();

  TestReturnC(cnt, "MemberHelper.Get...", true, "SuccessCount.IntField.Calc", false, () =>
  {
    var msg = $"{MemberHelper.GetProperty((TestCount e) => e.SuccessCount)?.Name}"
      + $".{MemberHelper.GetField((TestDummyChild e) => e.IntField)?.Name}"
      + $".{MemberHelper.GetMethod((TestCount e) => e.Calc(true))?.Name}"
      ;
    if (ShowTestConsoleOutput)
      Console.WriteLine(msg);
    return msg;
  });

  return cnt;
}

TestCount TestEnvHelper()
{
  var cnt = new TestCount();

  var appBaseDir = TestReturnC(cnt, "AppDomain.CurrentDomain.BaseDirectory", false, null, false, () =>
  {
    var dir = AppDomain.CurrentDomain.BaseDirectory;

    if (ShowTestConsoleOutput)
      Console.WriteLine($"AppDomain.CurrentDomain.BaseDirectory: {dir}");

    return dir;
  });

  var entryAssemblyLocation = TestReturnC(cnt, "Assembly.GetEntryAssembly()?.Location", false, null, false, () =>
  {
    var dir = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);

    if (ShowTestConsoleOutput)
      Console.WriteLine($"Assembly.GetEntryAssembly()?.Location: {dir}");

    return dir;
  });

  if (((appBaseDir?.Success) ?? false) && ((entryAssemblyLocation?.Success) ?? false))
    TestReturnC(cnt, "appBaseDir == entryAssemblyLocation", true, true, false, () =>
    {
      var equals = appBaseDir?.Result?.Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).IsEqualCI(entryAssemblyLocation?.Result?.Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

      if (ShowTestConsoleOutput)
        Console.WriteLine($"Paths equals: {equals}. ({appBaseDir?.Result} == {entryAssemblyLocation!.Result})");

      return equals;
    });

  TestVoidC(cnt, "EnvironmentHelper.GetApplicationCultures", false, () =>
  {
    var cultures = EnvironmentHelper.GetApplicationCultures();

    if (ShowTestConsoleOutput)
      Console.WriteLine($"Cultures: {string.Join(",", cultures.Select(ci => $"{ci}"))}");
  });

  TestVoidC(cnt, "EnvironmentHelper.IsCommandLineArgumentDefined", false, () =>
  {
    _ = EnvironmentHelper.IsCommandLineArgumentDefined("any");
  });

  return cnt;
}

TestCount TestNetworkInfo()
{
  var cnt = new TestCount();

  TestVoidC(cnt, $"TcpClient.Connect {dbServer}:1433", false, () =>
  {
    var cli = new System.Net.Sockets.TcpClient();
    try
    {
      cli.Connect(dbServer!, 1433);
    }
    finally
    {
      cli.Close();
    }
  });

  //TestVoidC(cnt, "NetworkInterface.GetAllNetworkInterfaces", false, () =>
  //{
  //  var interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
  //  if (ShowTestConsoleOutput)
  //  {
  //    Console.WriteLine($"Interfaces: {interfaces.Length}");
  //    TestExtensions.TypeDumpers.Add(typeof(System.Net.IPAddress), (obj, type) => obj.ToString());
  //    foreach (var iface in interfaces)
  //    {
  //      Console.WriteLine($"{iface.Id}. {iface.Name}. {iface.Description}. {iface.NetworkInterfaceType}. {iface.Speed}. {iface.OperationalStatus}");
  //      Console.WriteLine($"  {iface.GetIPProperties().DumpProps("IPProperties", 5, "\r\n")}");
  //    }
  //  }
  //});

  return cnt;
}

TestCount TestDbi()
{
  var cnt = new TestCount();

  void TestDbi(bool usePG)
  {
    //PG: Server=194.87.34.14;Port=5432;Database=st_idea;User Id=postgres;Password=FhVfYpcy8WBu

    string connStr =
      usePG
      ? $"Server={dbServer};Port=5432;Database=st_idea;User Id={dbUser};Password={dbPass}"
      : $"Data Source={dbServer};Initial Catalog={(isCP ? "CP_" : "")}ST-BusinessEntity;Integrated Security={(dbIntegratedSecurity ? "True" : "False")}{(dbIntegratedSecurity ? "" : $";User ID={dbUser};Password={dbPass}")}";

    string _nl = usePG ? "\"" : "[", _nr = usePG ? "\"" : "]";

    var vehIds = new List<int>();
    int entTypeId_Vehicle = -1;

    bool connectionSucces = false;

    TestVoidC(cnt, "SqlConnection.Open", false, () =>
    {
      if (ShowTestConsoleOutput)
        Console.WriteLine($"Creating connection... {connStr}");

      System.Data.Common.DbConnection conn = isPG ? new Npgsql.NpgsqlConnection(connStr) : new SqlConnection(connStr);
      try
      {
        if (ShowTestConsoleOutput)
          Console.WriteLine($"Connecting... {connStr}");
        conn.Open();

        connectionSucces = true;
      }
      finally
      {
        if (conn.State != System.Data.ConnectionState.Closed)
          conn.Close();
      }
    });

    if (connectionSucces)
      TestVoidC(cnt, "SqlCommand Entity Ids", false, () =>
      {
        System.Data.Common.DbConnection conn = isPG ? new Npgsql.NpgsqlConnection(connStr) : new SqlConnection(connStr);
        try
        {
          conn.Open();

          List<long> ReadInt64List(string select)
          {
            var ids = new List<long>();
            using System.Data.Common.DbCommand cmd = isPG ? new Npgsql.NpgsqlCommand(select, (Npgsql.NpgsqlConnection)conn) : new SqlCommand(select, (SqlConnection)conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
              ids.Add(reader.GetInt32(0));
            return ids;
          }

          string limit1 = isPG ? "" : "top 100";
          string limit2 = isPG ? "limit 100" : "";

          vehIds = ReadInt64List(
  @$"select * from (select {limit1} f.entityid from {(isPG ? "businessentity_data" : "[data]")}.{_nl}{vehicleChild}v{_nr} f {(isPG ? "" : "(nolock)")} where f.deleted is null order by f.entityid desc {limit2}) t
union
select * from (select {limit1} f.entityid from {(isPG ? "businessentity_data" : "[data]")}.{_nl}{vehicleChild}v{_nr} f {(isPG ? "" : "(nolock)")} where f.deleted is not null order by f.entityid desc {limit2}) t
union
select * from (select {limit1} f.entityid from {(isPG ? "businessentity_data" : "[data]")}.{_nl}{vehicleChild}v{_nr} f {(isPG ? "" : "(nolock)")} where f.plate is not null order by f.entityid desc {limit2}) t
union
select * from (select {limit1} f.entityid from {(isPG ? "businessentity_data" : "[data]")}.{_nl}{vehicleChild}v{_nr} f {(isPG ? "" : "(nolock)")} where f.garageNumber is not null order by f.entityid desc {limit2}) t
union
select * from (select {limit1} f.entityid from {(isPG ? "businessentity_data" : "[data]")}.{_nl}{vehicleChild}v{_nr} f {(isPG ? "" : "(nolock)")} where f.{_nl}currentrun{_nr} is not null order by f.entityid desc {limit2}) t
union
select * from (select {limit1} f.entityid from {(isPG ? "businessentity_data" : "[data]")}.{_nl}{vehicleChild}v{_nr} f {(isPG ? "" : "(nolock)")} where f.{_nl}driverid{_nr} is not null order by f.entityid desc {limit2}) t
union
select * from (select {limit1} f.entityid from {(isPG ? "businessentity_data" : "[data]")}.{_nl}{vehicleChild}v{_nr} f {(isPG ? "" : "(nolock)")} where f.{_nl}routeid{_nr} is not null order by f.entityid desc {limit2}) t").Select(l => (int)l)
            .OrderBy(id => id).ToList();

          entTypeId_Vehicle = (int)ReadInt64List($"select et.entitytypeid from {(isPG ? "businessentity_entitytype" : "[entitytype]")}.entitytype et {(isPG ? "" : "(nolock)")} where lower(et.code) = lower('{vehicleChild}')")[0];

          if (ShowTestConsoleOutput)
            Console.WriteLine($"entIds: {vehIds.Count}, entTypeId_Vehicle: {entTypeId_Vehicle}");
        }
        finally
        {
          if (conn.State != System.Data.ConnectionState.Closed)
            conn.Close();
        }
      });

    if (connectionSucces && vehIds.Count > 0)
    {
      Dbi.SetBind<Entity>(obj => obj.Id, "EntityId", "Id");
      Dbi.SetBind<Entity>(obj => obj.TypeId, "TypeId", "EntityTypeId");

      const string EMPTY_RESULTSET_QUERY = "select -1 where 1=-1";

      Dbi.RegisterAction(DbiType.PostgreSQL, "[Entity].[Get]", (Dbi_PG dbi, CommandBehavior? behavior, object[] args) =>
      {
        var queryBehavior = behavior.HasValue ? behavior.Value : CommandBehavior.Default;

        var selectQuery = dbi.GetScalar<string>("[entity].[getquery]", args);   // using getQuery instead of "[Entity].[Get]" to get select query for selected entity (because of PostgreSQL specific)

        // Примеры selectQuery: 
        // "select * from businessentity_data.agrigationlogicv"
        // "select * from businessentity_data.vehicledrvstyleparamv where entitytypeid = :id"
        // "select * from businessentity_data.monobjbaseparamexv where entitytypeid = 281"

        if (selectQuery == null)               //return null;
          selectQuery = EMPTY_RESULTSET_QUERY;  // Нельзя вернуть null, приходится возвращать пустой результат запроса, поскольку он ожидается в случае, если не удалось построить запрос на получение БС.

        return new Dbi_PG.StoredProcedure.Query(dbi, queryBehavior, selectQuery, args);
      });

      string xmlFile1 = $@".\Entity.Get.1.Core.{(isPG ? "PG" : "MSSQL")}.xml", xmlFile2 = $@".\Entity.Get.All.Core.{(isPG ? "PG" : "MSSQL")}.xml";

      TestVoidC(cnt, $"[Entity].[Get] by one", false, () =>
      {
        IDbi db = usePG ? new Dbi_PG() { OwnerModuleName = "businessentity" } : new Dbi();
        db.Connection = connStr;

        var vehList = new List<Vehicle>(vehIds.Count);
        var listPartial = new List<Dbi.RSResult.PartialObject<Vehicle>>(vehIds.Count);

        foreach (var entId in vehIds)
        {
          var partial = db.RS.SinglePartial<Vehicle>($"[Entity].[Get]", entId, 0);
          listPartial.Add(partial);
          vehList.Add(partial.Target);
        }

        if (ShowTestConsoleOutput)
          Console.WriteLine($"vehList: {vehList.Count}");

        var missCount = vehList.Count(e => !vehIds.Contains(e.Id));
        if (missCount > 0)
          throw new Exception($"Not found Entity ids: {missCount}");

        if (ShowTestConsoleOutput)
          File.WriteAllText(xmlFile1, "<VehListEntityGet>\r\n" + vehList.OrderBy(v => v.Id).Select(v => Serializer.SerializeXml2(v, "v")).JoinAsStrings(int.MaxValue, null, "\r\n") + "\r\n</VehListEntityGet>");

        var unbound = listPartial.SelectMany(p => p.UnboundFields).Distinct(p => p.Key, StringComparer.InvariantCultureIgnoreCase).ToArray();
        if (ShowTestConsoleOutput)
          if (unbound.Length > 0)
            Console.WriteLine($" @ Unbound: {unbound.Select(kv => kv.Key).JoinAsStrings(int.MaxValue)}");
      });

      TestVoidC(cnt, $"[Entity].[Get] all", false, () =>
      {
        IDbi db = usePG ? new Dbi_PG() { OwnerModuleName = "businessentity" } : new Dbi();
        db.Connection = connStr;

        var vehList = new List<Vehicle>(vehIds.Count);
        var listPartial = db.RS.ListPartial<Vehicle>($"[Entity].[Get]", entTypeId_Vehicle, 2);

        vehList.AddRange(listPartial.Select(p => p.Target));

        if (ShowTestConsoleOutput)
          Console.WriteLine($"vehList: {vehList.Count}, Expected: {vehList.Count(v => vehIds.Contains(v.Id))}");

        var missCount = vehIds.Count(eId => !vehList.Any(v => v.Id == eId));
        if (missCount > 0)
          throw new Exception($"Not found Entity ids: {missCount}");

        if (ShowTestConsoleOutput)
          File.WriteAllText(xmlFile2, "<VehListEntityGet>\r\n" + vehList.Where(v => vehIds.Contains(v.Id)).OrderBy(v => v.Id).Select(v => Serializer.SerializeXml2(v, "v")).JoinAsStrings(int.MaxValue, null, "\r\n") + "\r\n</VehListEntityGet>");

        var unbound = listPartial.SelectMany(p => p.UnboundFields).Distinct(p => p.Key, StringComparer.InvariantCultureIgnoreCase).ToArray();
        if (ShowTestConsoleOutput)
          if (unbound.Length > 0)
            Console.WriteLine($" @ Unbound: {unbound.Select(kv => kv.Key).JoinAsStrings(int.MaxValue)}");
      });

      TestVoidC(cnt, "Compare Entity xmls", false, () =>
      {
        var fi1 = new FileInfo(xmlFile1);
        var fi2 = new FileInfo(xmlFile2);
        if (ShowTestConsoleOutput)
        {
          Console.WriteLine($"File 1: {fi1.Name}, {fi1.Length}");
          Console.WriteLine($"File 2: {fi2.Name}, {fi2.Length}");
        }

        XDocument doc1 = XDocument.Load(xmlFile1);
        XNamespace ns1 = doc1.Root!.GetDefaultNamespace();
        XDocument doc2 = XDocument.Load(xmlFile2);
        XNamespace ns2 = doc2.Root!.GetDefaultNamespace();
        var desc1 = doc1.Root!.Elements().Select(e => new { e, Id = e.Element(ns1 + "Id")!.Value, elements = e.Elements().ToArray() }).ToArray();
        var desc2 = doc2.Root!.Elements().Select(e => new { e, Id = e.Element(ns2 + "Id")!.Value, elements = e.Elements().ToArray() }).ToArray();
        var joined = desc1.Join(desc2, e => e.Id, e => e.Id, (e1, e2) => (e1, e2, joinedelems: e1.elements.Join(e2.elements, ee => ee.Name.LocalName, ee => ee.Name.LocalName, (ee1, ee2) => (ee1, ee2)).ToArray())).ToArray();
        var eq = joined.Where(e => e.joinedelems.Length > 0 && e.joinedelems.Length == e.e1.elements.Length && e.joinedelems.Length == e.e2.elements.Length && e.joinedelems.All(je => string.Compare(je.ee1.Value, je.ee2.Value, StringComparison.InvariantCultureIgnoreCase) == 0)).ToArray();

        if (ShowTestConsoleOutput)
        {
          Console.WriteLine($"cnt 1: {desc1.Length}, cnt 2: {desc2.Length}");
          Console.WriteLine($"joined: {joined.Length}");
          Console.WriteLine($"eq: {eq.Length}");
        }

      });
    }

  }

  TestDbi(isPG);

  return cnt;
}

TestCount TestIAspectProvider()
{
  var cnt = new TestCount();

  TestReturnC(cnt, "IAspectProvider.AutoDataContractClass", true, true, false, () =>
  {
    return typeof(AutoDataContractClass).IsDefined(typeof(System.Runtime.Serialization.DataContractAttribute));
  });

  TestReturnC(cnt, "IAspectProvider.ServiceBehaviorAttribute", true, true, false, () =>
  {
    return typeof(WcfServer).IsDefined(typeof(CoreWCF.ServiceBehaviorAttribute));
  });

  return cnt;
}


