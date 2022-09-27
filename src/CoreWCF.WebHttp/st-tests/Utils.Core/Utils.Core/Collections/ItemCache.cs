using ST.Utils.Attributes;

namespace ST.Utils.Collections
{
  public class ItemCache<TKey, TValue> : FastCache<TKey, TValue>
  {
    #region .Ctor
    public ItemCache()
      : base( false )
    {
    }
    #endregion

    #region MakeReadOnly
    protected static T MakeReadOnly<T>( T obj )
    {
      if( obj is IReadOnlyProperties )
        (obj as IReadOnlyProperties).MakePropertiesAsReadOnly();

      return obj;
    }

    protected static T[] MakeReadOnly<T>( T[] list )
    {
      if( list != null )
        list.ForEach( i => MakeReadOnly( i ) );

      return list;
    }
    #endregion

    #region OnAdded
    protected override void OnAdded( TKey key, TValue value )
    {
      MakeReadOnly( value );
    }
    #endregion
  }
}
