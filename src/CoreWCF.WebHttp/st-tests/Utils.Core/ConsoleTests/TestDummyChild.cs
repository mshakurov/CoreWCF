// See https://aka.ms/new-console-template for more information
using ST.Utils;

namespace ConsoleTestsDummy
{

  public class TestDummyChild : TestDummy, IEquatable<TestDummyChild?>
  {
    public int IntField;
    public int? NullIntField;
    public string StringField;
    public ulong UlongField;
    public DateTime DateTimeField;
    public int[]? IntArrField;
    public TestDummy? DummyField;
    public Dictionary<int, TestDummy>? DummyDictField;

    public TestDummyChild()
      : this(0)
    {
    }

    public TestDummyChild(int level)
    {
      IntField = TestDummy.Random.Next();
      NullIntField = TestDummy.Random.Next(10) == 0 ? TestDummy.Random.Next() : null;
      StringField = new string(Enumerable.Range(10, 100).Select(_ => TestDummy.Chars[TestDummy.Random.Next(TestDummy.Chars.Length)]).ToArray());
      UlongField = (ulong)TestDummy.Random.Next();
      DateTimeField = DateTime.UtcNow;
      IntArrField = TestDummy.Random.Next(10) > 1 ? Enumerable.Range(10, 100).Select(i => TestDummy.Random.Next()).ToArray() : null;
      DummyField = level < 3 && TestDummy.Random.Next(10) > 3 ? new TestDummy(level + 1) : null;
      DummyDictField = level < 3 && TestDummy.Random.Next(10) > 1 ?
       new Dictionary<int, TestDummy>
       {
         [10] = new TestDummy(level + 1),
         [20] = new TestDummy(level + 1),
         [30] = new TestDummy(level + 1),
         [int.MaxValue] = new TestDummy(level + 1),
       }
       : null;
    }

    public override bool Equals(object? obj)
    {
      return base.Equals(obj as TestDummy) && Equals(obj as TestDummyChild);
    }

    public bool Equals(TestDummyChild? other)
    {
      return other != null &&
             IntField == other.IntField &&
             NullIntField == other.NullIntField &&
             StringField == other.StringField &&
             UlongField == other.UlongField &&
             DateTimeField == other.DateTimeField &&
             (IntArrField ?? Array.Empty<int>()).SequenceEqual((other.IntArrField ?? Array.Empty<int>())) &&
             DummyField == other.DummyField &&
             (DummyDictField ?? new Dictionary<int, TestDummy>()).OrderBy(kv => kv.Key).SequenceEqual((other.DummyDictField ?? new Dictionary<int, TestDummy>()).OrderBy(kv => kv.Key));
    }

    public override int GetHashCode()
    {
      HashCode hash = new HashCode();
      hash.Add(base.GetHashCode());
      hash.Add(IntField);
      hash.Add(NullIntField);
      hash.Add(StringField);
      hash.Add(UlongField);
      hash.Add(DateTimeField);
      hash.Add(IntArrField);
      hash.Add(DummyField);
      hash.Add(DummyDictField);
      return hash.ToHashCode();
    }

    public static bool operator ==(TestDummyChild? left, TestDummyChild? right)
    {
      return EqualityComparer<TestDummyChild>.Default.Equals(left, right);
    }

    public static bool operator !=(TestDummyChild? left, TestDummyChild? right)
    {
      return !(left == right);
    }
  }
}