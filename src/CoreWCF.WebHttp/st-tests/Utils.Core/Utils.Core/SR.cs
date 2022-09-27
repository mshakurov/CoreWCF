using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Threading;
using ST.Utils.Attributes;
using ST.Utils.Collections;

namespace ST.Utils
{
  /// <summary> 
  /// Класс предназначен для работы с локализованными ресурсами.
  /// Формат идентификатора локализованного ресурса (без кавычек): "{Название элемента ресурса}|{Простое название сбоки, содержащей ресурс}@{Корневое название ресурса}"
  /// </summary>
  public static class SR
  {
    #region .Constants
    /// <summary>
    /// Идентификатор отсутствующего ресурса.
    /// </summary>
    public const string EMPTY = "(^_^)";

    private const char ITEM_SEPARATOR = '|';
    private const char PATH_SEPARATOR = '@';
    #endregion

    #region .Static Fields
    private static readonly string ID_TEMPLATE = "{0}" + ITEM_SEPARATOR + "{1}" + PATH_SEPARATOR + "{2}";

    private static readonly FastCache<string, ResourceManager> _managers = new FastCache<string, ResourceManager>( true );
    private static readonly FastCache<string, string> _strings = new FastCache<string, string>( true );

    /// <summary>
    /// Культура, используемая по умолчанию.
    /// </summary>
    public static readonly CultureInfo DefaultCulture = CultureInfo.GetCultureInfo( "ru" );
    #endregion

    #region GetAssemblyName
    /// <summary>
    /// Возвращает простое название сбоки, содержащей ресурс.
    /// </summary>
    /// <param name="resourceId">Идентификатор ресурса.</param>
    /// <returns>Простое название сбоки.</returns>
    public static string GetAssemblyName( string resourceId )
    {
      if( !IsResourceId( resourceId ) )
        return string.Empty;

      var i = resourceId.IndexOf( ITEM_SEPARATOR );
      var j = resourceId.IndexOf( PATH_SEPARATOR );

      return i != -1 && j != -1 ? resourceId.Substring( i + 1, j - i - 1 ) : string.Empty;
    }
    #endregion

    #region GetItemName
    /// <summary>
    /// Возвращает название элемента ресурса.
    /// </summary>
    /// <param name="resourceId">Идентификатор ресурса.</param>
    /// <returns>Название элемента ресурса.</returns>
    public static string GetItemName( string resourceId )
    {
      if( !IsResourceId( resourceId ) )
        return string.Empty;

      var i = resourceId.IndexOf( ITEM_SEPARATOR );

      return i != -1 ? resourceId.Substring( 0, i ) : string.Empty;
    }
    #endregion

    #region GetManager
    private static ResourceManager GetManager( string resourceId )
    {
      return _managers.Get( resourceId, GetPathName, (ctx, key) => // Ради производительности нельзя использовать замыкание (closure).
      {
        ResourceManager manager = null;

        if( key != string.Empty )
          try
          {
            manager = new ResourceManager( GetResourceName( ctx ), Assembly.Load( GetAssemblyName( ctx ) ) );
          }
          catch
          {
          }

        return manager;
      } );
    }
    #endregion

    #region GetObject
    /// <summary>
    /// Возвращает локализованный объект по идентификатору ресурса.
    /// </summary>
    /// <param name="resourceId">Идентификатор ресурса.</param>
    /// <param name="culture">Информация о культуре, для которой необходимо вернуть значение.</param>
    /// <returns>Локализованное значение объекта или null, если ресурс с указанным идентификатором не найден.</returns>
    public static object GetObject( string resourceId, CultureInfo culture = null )
    {
      return GetObject<object>( resourceId, culture );
    }

    /// <summary>
    /// Возвращает локализованный объект по идентификатору ресурса.
    /// </summary>
    /// <param name="resourceId">Идентификатор ресурса.</param>
    /// <param name="culture">Информация о культуре, для которой необходимо вернуть значение.</param>
    /// <returns>Локализованное значение объекта или null, если ресурс с указанным идентификатором не найден.</returns>
    public static T GetObject<T>( string resourceId, CultureInfo culture = null )
      where T : class
    {
      T value = null;

      var manager = GetManager( resourceId );

      if( manager != null )
        try
        {
          value = manager.GetObject( GetItemName( resourceId ), culture ) as T;
        }
        catch
        {
        }

      return value;
    }
    #endregion

    #region GetPathName
    /// <summary>
    /// Возвращает путь ресурса - часть идентификатора ресурса, представляющую собой "{Простое название сбоки, содержащей ресурс}@{Корневое название ресурса}".
    /// </summary>
    /// <param name="resourceId">Идентификатор ресурса.</param>
    /// <returns>Путь ресурса.</returns>
    public static string GetPathName( string resourceId )
    {
      if( !IsResourceId( resourceId ) )
        return string.Empty;

      var i = resourceId.IndexOf( ITEM_SEPARATOR );

      return i != -1 && i < resourceId.Length - 1 ? resourceId.Substring( i + 1 ) : string.Empty;
    }
    #endregion

    #region GetResourceId
    /// <summary>
    /// Возвращает идентификатор ресурса.
    /// </summary>
    /// <param name="itemName">Название элемента ресурса.</param>
    /// <param name="assemblyName">Простое название сбоки, содержащей ресурс.</param>
    /// <param name="resourceName">Корневое название ресурса.</param>
    /// <returns>Идентификатор ресурса</returns>
    public static string GetResourceId( [NotNull] string itemName, [NotNull] string assemblyName, [NotNull] string resourceName )
    {
      return string.Format( ID_TEMPLATE, itemName, assemblyName, resourceName );
    }

    /// <summary>
    /// Возвращает идентификатор ресурса.
    /// </summary>
    /// <param name="itemName">Название элемента ресурса.</param>
    /// <param name="assembly">Сборка, содержащая ресурс.</param>
    /// <param name="resourceName">Корневое название ресурса.</param>
    /// <returns>Идентификатор ресурса</returns>
    public static string GetResourceId( [NotNullNotEmpty] string itemName, [NotNull] Assembly assembly, [NotNullNotEmpty] string resourceName )
    {
      return GetResourceId( itemName, assembly.GetName().Name ?? String.Empty, resourceName );
    }
    #endregion

    #region GetResourceName
    /// <summary>
    /// Возвращает корневое название ресурса.
    /// </summary>
    /// <param name="resourceId">Идентификатор ресурса.</param>
    /// <returns>Корневое название ресурса.</returns>
    public static string GetResourceName( string resourceId )
    {
      if( !IsResourceId( resourceId ) )
        return string.Empty;

      var i = resourceId.IndexOf( PATH_SEPARATOR );

      return i != -1 && i < resourceId.Length - 1 ? resourceId.Substring( i + 1 ) : string.Empty;
    }
    #endregion

    #region GetString
    /// <summary>
    /// Возвращает локализованную строку по идентификатору ресурса.
    /// </summary>
    /// <param name="resourceId">Идентификатор ресурса.</param>
    /// <param name="args">Параметры форматирования.</param>
    /// <returns>Локализованное значение строки или null, если строковый ресурс с указанным идентификатором не найден.</returns>
    public static string GetString( string resourceId, params object[] args )
    {
      return GetString( Thread.CurrentThread.CurrentUICulture, resourceId, args );
    }

    /// <summary>
    /// Возвращает локализованную строку по идентификатору ресурса.
    /// </summary>
    /// <param name="culture">Информация о культуре, для которой необходимо вернуть значение.</param>
    /// <param name="resourceId">Идентификатор ресурса.</param>
    /// <param name="args">Параметры форматирования.</param>
    /// <returns>Локализованное значение строки или null, если строковый ресурс с указанным идентификатором не найден.</returns>
    public static string GetString( CultureInfo culture, string resourceId, params object[] args )
    {
      string value = _strings.Get( new ResourceContext { Culture = culture, ResourceId = resourceId }, // Ради производительности нельзя использовать замыкание (closure).
                                   ctx => ctx.ResourceId + (ctx.Culture == null || ctx.Culture.Name.StartsWith( "ru" ) ? string.Empty : ctx.Culture.Name), (ctx, key) =>
      {
        var manager = GetManager( ctx.ResourceId );

        if( manager != null )
          try
          {
            return manager.GetString( GetItemName( ctx.ResourceId ), ctx.Culture ) ?? String.Empty;
          }
          catch
          {
          }

        return null;
      } );

      return value == null ? resourceId :
             args == null || args.Length == 0 ? value :
             string.Format( value, args );
    }
    #endregion

    #region IsResourceId
    /// <summary>
    /// Определяет, является ли строка идентификатором ресурса.
    /// </summary>
    /// <param name="str">Строка.</param>
    /// <returns>True - строка является идентификатором ресурса, иначе - False.</returns>
    public static bool IsResourceId( string str )
    {
      if( string.IsNullOrWhiteSpace( str ) )
        return false;

      var i = str.IndexOf( ITEM_SEPARATOR );

      return i != -1 && str.IndexOf( PATH_SEPARATOR, i ) > i + 1;
    }
    #endregion

    [StructLayout( LayoutKind.Auto )]
    private struct ResourceContext
    {
      #region .Fields
      public CultureInfo Culture;
      public string ResourceId;
      #endregion
    }
  }
}
