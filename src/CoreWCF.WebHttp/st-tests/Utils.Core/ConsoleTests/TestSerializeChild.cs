using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTestsSerialize
{
  [Serializable]
  public class TestSerializeChild : TestSerialize, IEquatable<TestSerializeChild?>
  {
    private int PrivateIntField;
    [NonSerialized]
    private int NosProvateIntField;

    public int IntField;
    public int? NullIntField;
    public string StringField;
    public ulong UlongField;
    public DateTime DateTimeField;
    public int[]? IntArrField;
    public TestSerialize? DummyField;
    public Dictionary<int, TestSerialize>? DummyDictField;

    public TestSerializeChild()
      : this(0)
    {
    }

    public TestSerializeChild(int level)
    {
      IntField = TestSerialize.Random.Next();
      NullIntField = TestSerialize.Random.Next(10) == 0 ? TestSerialize.Random.Next() : null;
      StringField = new string(Enumerable.Range(10, 100).Select(_ => TestSerialize.Chars[TestSerialize.Random.Next(TestSerialize.Chars.Length)]).ToArray());
      UlongField = (ulong)TestSerialize.Random.Next();
      DateTimeField = DateTime.UtcNow;
      IntArrField = TestSerialize.Random.Next(10) > 1 ? Enumerable.Range(10, 100).Select(i => TestSerialize.Random.Next()).ToArray() : null;
      DummyField = level < 3 && TestSerialize.Random.Next(10) > 3 ? new TestSerialize(level + 1) : null;
      DummyDictField = level < 3 && TestSerialize.Random.Next(10) > 1 ?
       new Dictionary<int, TestSerialize>
       {
         [10] = new TestSerialize(level + 1),
         [20] = new TestSerialize(level + 1),
         [30] = new TestSerialize(level + 1),
         [int.MaxValue] = new TestSerialize(level + 1),
       }
       : null;

      PrivateIntField = TestSerialize.Random.Next();

      NosProvateIntField = TestSerialize.Random.Next();
    }

    int GetNosProvateIntField() => NosProvateIntField;

    public override bool Equals(object? obj)
    {
      return base.Equals(obj as TestSerialize) && Equals(obj as TestSerializeChild);
    }

    public bool Equals(TestSerializeChild? other)
    {
      return other != null &&
             IntField == other.IntField &&
             NullIntField == other.NullIntField &&
             StringField == other.StringField &&
             UlongField == other.UlongField &&
             DateTimeField == other.DateTimeField &&
             (IntArrField ?? Array.Empty<int>()).SequenceEqual((other.IntArrField ?? Array.Empty<int>())) &&
             DummyField == other.DummyField &&
             (DummyDictField ?? new Dictionary<int, TestSerialize>()).OrderBy(kv => kv.Key).SequenceEqual((other.DummyDictField ?? new Dictionary<int, TestSerialize>()).OrderBy(kv => kv.Key)) &&
             PrivateIntField == other.PrivateIntField;
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
      hash.Add(PrivateIntField);
      return hash.ToHashCode();
    }

    public static bool operator ==(TestSerializeChild? left, TestSerializeChild? right)
    {
      return EqualityComparer<TestSerializeChild>.Default.Equals(left, right);
    }

    public static bool operator !=(TestSerializeChild? left, TestSerializeChild? right)
    {
      return !(left == right);
    }
  }
}
