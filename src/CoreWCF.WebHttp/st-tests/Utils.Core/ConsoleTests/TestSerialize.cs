using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTestsSerialize
{
  [Serializable]
  public class TestSerialize
  {
    public static Random Random = new Random();
    public static string Chars = new string(Enumerable.Range((int)'a', (int)'z' - (int)'a' + 1).Concat(Enumerable.Range((int)'A', (int)'Z' - (int)'Z' + 1)).Select(i => (char)i).ToArray())
      + "0123456789,.;':-+=@#$%^&?*()[]{}<>";

    public TestSerialize()
      : this(0)
    {
    }

    public TestSerialize(int level)
    {
      IntProperty = Random.Next();
      NullIntProperty = Random.Next(10) == 0 ? Random.Next() : null;
      StringProperty = new string(Enumerable.Range(10, 100).Select(_ => Chars[Random.Next(Chars.Length)]).ToArray());
      UlongProperty = (ulong)Random.Next();
      DateTimeProperty = DateTime.UtcNow;
      IntArrProperty = Random.Next(10) > 1 ? Enumerable.Range(10, 100).Select(i => Random.Next()).ToArray() : null;
      DummyProperty = level < 3 && Random.Next(10) > 3 ? new TestSerialize(level + 1) : null;
      DummyDictProperty = level < 3 && Random.Next(10) > 1 ?
       new Dictionary<int, TestSerialize>
       {
         [10] = new TestSerialize(level + 1),
         [20] = new TestSerialize(level + 1),
         [30] = new TestSerialize(level + 1),
         [int.MaxValue] = new TestSerialize(level + 1),
       }
       : null;

      PrivateIntProperty = Random.Next();
    }

    public int IntProperty { get; set; }
    public int? NullIntProperty { get; set; }
    public string StringProperty { get; set; }
    public ulong UlongProperty { get; set; }
    public DateTime DateTimeProperty { get; set; }
    public int[]? IntArrProperty { get; set; }
    public TestSerialize? DummyProperty { get; set; }
    public Dictionary<int, TestSerialize>? DummyDictProperty { get; set; }

    private int PrivateIntProperty { get; set; }

    public override bool Equals(object? obj)
    {
      return Equals(obj as TestSerialize);
    }

    public bool Equals(TestSerialize? other)
    {
      return other != null &&
             IntProperty == other.IntProperty &&
             NullIntProperty == other.NullIntProperty &&
             StringProperty == other.StringProperty &&
             UlongProperty == other.UlongProperty &&
             DateTimeProperty == other.DateTimeProperty &&
             (IntArrProperty ?? Array.Empty<int>()).SequenceEqual((other.IntArrProperty ?? Array.Empty<int>())) &&
             DummyProperty == other.DummyProperty &&
             (DummyDictProperty ?? new Dictionary<int, TestSerialize>()).OrderBy(kv => kv.Key).SequenceEqual((other.DummyDictProperty ?? new Dictionary<int, TestSerialize>()).OrderBy(kv => kv.Key));
    }

    public override int GetHashCode()
    {
      HashCode hash = new HashCode();
      hash.Add(IntProperty);
      hash.Add(NullIntProperty);
      hash.Add(StringProperty);
      hash.Add(UlongProperty);
      hash.Add(DateTimeProperty);
      hash.Add(IntArrProperty);
      hash.Add(DummyProperty);
      hash.Add(DummyDictProperty);
      return hash.ToHashCode();
    }

    public static bool operator ==(TestSerialize? left, TestSerialize? right)
    {
      return EqualityComparer<TestSerialize>.Default.Equals(left, right);
    }

    public static bool operator !=(TestSerialize? left, TestSerialize? right)
    {
      return !(left == right);
    }

    public TestSerialize TestClone()
    {
      return (TestSerialize)this.MemberwiseClone();
    }

  }
}
