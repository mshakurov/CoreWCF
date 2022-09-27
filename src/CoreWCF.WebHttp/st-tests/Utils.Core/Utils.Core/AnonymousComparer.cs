using System.Collections.Generic;

namespace System.Linq
{
  public static class AnonymousComparer
  {
    #region IComparer<T>

    /// <summary>Example:AnonymousComparer.Create&lt;int&gt;((x, y) => y - x)</summary>
    public static IComparer<T> Create<T>( Func<T, T, int> compare )
    {
      return compare == null ? throw new ArgumentNullException( nameof(compare)) : (IComparer<T>)new Comparer<T>( compare );
    }

    private class Comparer<T> : IComparer<T>
    {
      private readonly Func<T, T, int> compare;

      public Comparer( Func<T, T, int> compare )
      {
        this.compare = compare;
      }

      public int Compare( T x, T y )
      {
        return compare( x, y );
      }
    }

    #endregion

    #region IEqualityComparer<T>

    /// <summary>Example:AnonymousComparer.Create((MyClass mc) => mc.MyProperty)</summary>
    public static IEqualityComparer<T> Create<T, TKey>( Func<T, TKey> compareKeySelector )
    {
      return compareKeySelector == null
          ? throw new ArgumentNullException( nameof(compareKeySelector))
          : (IEqualityComparer<T>)new EqualityComparer<T>(
          ( x, y ) =>
          {
            return object.ReferenceEquals( x, y )
                ? true
                : x == null || y == null ? false : compareKeySelector( x ).Equals( compareKeySelector( y ) );
          },
          obj =>
          {
            return obj == null ? 0 : compareKeySelector( obj ).GetHashCode();
          });
    }

    /// <summary>Example:AnonymousComparer.Create((MyClass mc) => mc.MyProperty, StringComparer.InvariantCultureIgnoreCase)</summary>
    public static IEqualityComparer<T> Create<T, TKey>( Func<T, TKey> compareKeySelector, IEqualityComparer<TKey> keyComparer )
    {
      if ( compareKeySelector == null )
        throw new ArgumentNullException( nameof(compareKeySelector));

      if (keyComparer == null )
        throw new ArgumentNullException( nameof(keyComparer));

      return new EqualityComparer<T>(
          ( x, y ) =>
          {
            if ( object.ReferenceEquals( x, y ) )
              return true;
            if ( x == null || y == null )
              return false;
            return keyComparer.Equals( compareKeySelector( x ), compareKeySelector( y ) );
          },
          obj =>
          {
            if ( obj == null )
              return 0;
            return compareKeySelector( obj ).GetHashCode();
          } );
    }

    public static IEqualityComparer<T> Create<T>( Func<T, T, bool> equals, Func<T, int> getHashCode )
    {
      if ( equals == null )
        throw new ArgumentNullException( nameof(equals));
      if ( getHashCode == null )
        throw new ArgumentNullException( nameof(getHashCode));

      return new EqualityComparer<T>( equals, getHashCode );
    }

    private class EqualityComparer<T> : IEqualityComparer<T>
    {
      private readonly Func<T, T, bool> equals;
      private readonly Func<T, int> getHashCode;

      public EqualityComparer( Func<T, T, bool> equals, Func<T, int> getHashCode )
      {
        this.equals = equals;
        this.getHashCode = getHashCode;
      }

      public bool Equals( T x, T y )
      {
        return equals( x, y );
      }

      public int GetHashCode( T obj )
      {
        return getHashCode( obj );
      }
    }

    #endregion

    #region Extensions for LINQ Standard Query Operators

    // IComparer<T>

    public static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>( this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, TKey, int> compare )
    {
      return source.OrderBy( keySelector, AnonymousComparer.Create( compare ) );
    }

    public static IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>( this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, TKey, int> compare )
    {
      return source.OrderByDescending( keySelector, AnonymousComparer.Create( compare ) );
    }

    public static IOrderedEnumerable<TSource> ThenBy<TSource, TKey>( this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, TKey, int> compare )
    {
      return source.ThenBy( keySelector, AnonymousComparer.Create( compare ) );
    }

    public static IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>( this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, TKey, int> compare )
    {
      return source.ThenByDescending( keySelector, AnonymousComparer.Create( compare ) );
    }

    // IEqualityComparer<T>

    public static bool Contains<TSource, TCompareKey>( this IEnumerable<TSource> source, TSource value, Func<TSource, TCompareKey> compareKeySelector )
    {
      return source.Contains( value, AnonymousComparer.Create( compareKeySelector ) );
    }

    public static IEnumerable<TSource> Distinct<TSource, TCompareKey>( this IEnumerable<TSource> source, Func<TSource, TCompareKey> compareKeySelector )
    {
      return source.Distinct( AnonymousComparer.Create( compareKeySelector ) );
    }

    public static IEnumerable<TSource> Distinct<TSource, TCompareKey>( this IEnumerable<TSource> source, Func<TSource, TCompareKey> compareKeySelector, IEqualityComparer<TCompareKey> keyComparer )
    {
      return source.Distinct( AnonymousComparer.Create( compareKeySelector, keyComparer ) );
    }

    public static IEnumerable<TSource> Except<TSource, TCompareKey>( this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TCompareKey> compareKeySelector )
    {
      return first.Except( second, AnonymousComparer.Create( compareKeySelector ) );
    }

    public static IEnumerable<TSource> Exclude<TSource, TCompareKey>( this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TCompareKey> compareKeySelector )
    {
      var hashSet = new HashSet<TCompareKey>( second.Select( s => compareKeySelector( s ) ) );
      return first.Where( f => !hashSet.Contains( compareKeySelector( f ) ) );
    }

    public static IEnumerable<TSource> Difference<TSource, TCompareKey>( this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TCompareKey> compareKeySelector )
    {
      return first.Except( second, compareKeySelector ).Concat( second.Except( first, compareKeySelector ) );
    }

    public static void Difference<TSource, TCompareKey>( this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TCompareKey> compareKeySelector, Func<TSource, TSource, bool> comparer, Action<IEnumerable<TSource>> actionLeft, Action<IEnumerable<TSource>> actionRight, Action<IEnumerable<Tuple<TSource, TSource>>> actionEqual )
    {
      if ( first != null )
      {
        if ( second == null )
        {
          if ( first.Any() )
            actionLeft( first );
          return;
        }
        var arr = first.Except( second, compareKeySelector ).ToArray();
        if ( arr.Length > 0 )
          actionLeft( arr );
      }
      if ( second != null )
      {
        if ( first == null )
        {
          if ( second.Any() )
            actionRight( second );
          return;
        }
        var arr = second.Except( first, compareKeySelector ).ToArray();
        if ( arr.Length > 0 )
          actionRight( arr );
      }
      if ( first != null && second != null )
      {
        var arrEq = first.Join( second, left => compareKeySelector( left ), right => compareKeySelector( right ), ( left, right ) => Tuple.Create( left, right ) )
          .Where( tp => comparer( tp.Item1, tp.Item2 ) ).ToArray();
        if ( arrEq.Length > 0 )
          actionEqual( arrEq );
      }
    }

    public static IEnumerable<TResult> GroupBy<TSource, TKey, TResult, TCompareKey>( this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IEnumerable<TSource>, TResult> resultSelector, Func<TKey, TCompareKey> compareKeySelector )
    {
      return source.GroupBy( keySelector, resultSelector, AnonymousComparer.Create( compareKeySelector ) );
    }

    public static IEnumerable<IGrouping<TKey, TElement>> GroupBy<TSource, TKey, TElement, TCompareKey>( this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, TCompareKey> compareKeySelector )
    {
      return source.GroupBy( keySelector, elementSelector, AnonymousComparer.Create( compareKeySelector ) );
    }

    public static IEnumerable<TResult> GroupBy<TSource, TKey, TElement, TResult, TCompareKey>( this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IEnumerable<TElement>, TResult> resultSelector, Func<TKey, TCompareKey> compareKeySelector )
    {
      return source.GroupBy( keySelector, elementSelector, resultSelector, AnonymousComparer.Create( compareKeySelector ) );
    }

    public static IEnumerable<TResult> GroupJoin<TOuter, TInner, TKey, TResult, TCompareKey>( this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, IEnumerable<TInner>, TResult> resultSelector, Func<TKey, TCompareKey> compareKeySelector )
    {
      return outer.GroupJoin( inner, outerKeySelector, innerKeySelector, resultSelector, AnonymousComparer.Create( compareKeySelector ) );
    }

    public static IEnumerable<TSource> Intersect<TSource, TCompareKey>( this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TCompareKey> compareKeySelector )
    {
      return first.Intersect( second, AnonymousComparer.Create( compareKeySelector ) );
    }

    public static IEnumerable<TResult> Join<TOuter, TInner, TKey, TResult, TCompareKey>( this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner, TResult> resultSelector, Func<TKey, TCompareKey> compareKeySelector )
    {
      return outer.Join( inner, outerKeySelector, innerKeySelector, resultSelector, AnonymousComparer.Create( compareKeySelector ) );
    }

    public static bool SequenceEqual<TSource, TCompareKey>( this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TCompareKey> compareKeySelector )
    {
      return first.SequenceEqual( second, AnonymousComparer.Create( compareKeySelector ) );
    }

    public static Dictionary<TKey, TElement> ToDictionary<TSource, TKey, TElement, TCompareKey>( this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, TCompareKey> compareKeySelector )
      where TKey : notnull
    {
      return source.ToDictionary( keySelector, elementSelector, AnonymousComparer.Create( compareKeySelector ) );
    }

    public static ILookup<TKey, TElement> ToLookup<TSource, TKey, TElement, TCompareKey>( this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, TCompareKey> compareKeySelector )
    {
      return source.ToLookup( keySelector, elementSelector, AnonymousComparer.Create( compareKeySelector ) );
    }

    public static IEnumerable<TSource> Union<TSource, TCompareKey>( this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TCompareKey> compareKeySelector )
    {
      return first.Union( second, AnonymousComparer.Create( compareKeySelector ) );
    }

    public static IEnumerable<TSource> Union<TSource>( this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TSource, bool> equals, Func<TSource, int> getHashCode )
    {
      return first.Union( second, AnonymousComparer.Create( equals, getHashCode ) );
    }

    public static IEnumerable<TSource> UpdateOrAdd<TSource, TCompareKey>( this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TCompareKey> compareKeySelector )
    {
      return ( second == null || !second.Any() ) ? first : first.Except( second, AnonymousComparer.Create( compareKeySelector ) ).Concat( second );
    }

    #endregion
  }
}