using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using ST.Utils.Attributes;

namespace ST.Utils
{
  /// <summary>
  /// Вспомогательный класс для получения информации об атрибутах.
  /// </summary>
  public static class AttributeHelper
  {
    #region GetAttribute
    /// <summary>
    /// Возвращает первый найденный атрибут указанного типа.
    /// </summary>
    /// <typeparam name="T">Тип атрибута.</typeparam>
    /// <param name="element">Элемент, в котором производится поиск атрибута.</param>
    /// <param name="inherit">Признак поиска атрибута в родителях element.</param>
    /// <returns>Экземпляр искомого атрибута или null, если атрибут не найден.</returns>
    [DebuggerStepThrough]
    public static T GetAttribute<T>( [NotNull] this MemberInfo element, bool inherit = true )
      where T : Attribute
    {
      return Attribute.GetCustomAttribute( element, typeof( T ), inherit ) as T;
    }

    /// <summary>
    /// Возвращает первый найденный атрибут указанного типа.
    /// </summary>
    /// <typeparam name="T">Тип атрибута.</typeparam>
    /// <param name="obj">Объект, в типе которого производится поиск атрибута.</param>
    /// <param name="inherit">Признак поиска атрибута в родителях типа obj.</param>
    /// <returns>Экземпляр искомого атрибута или null, если атрибут не найден.</returns>
    [DebuggerStepThrough]
    public static T GetAttribute<T>( [NotNull] this object obj, bool inherit = true )
      where T : Attribute
    {
      return obj.GetMemberInfo().GetAttribute<T>( inherit );
    }
    #endregion

    #region GetAttributes
    /// <summary>
    /// Возвращает список найденных атрибутов указанного типа.
    /// </summary>
    /// <typeparam name="T">Тип атрибута.</typeparam>
    /// <param name="element">Элемент, в котором производится поиск атрибутов.</param>
    /// <param name="inherit">Признак поиска атрибута в родителях element.</param>
    /// <returns>Список атрибутов.</returns>
    [DebuggerStepThrough]
    public static T[] GetAttributes<T>( [NotNull] this MemberInfo element, bool inherit = true )
      where T : Attribute
    {
      return Attribute.GetCustomAttributes( element, typeof( T ), inherit ) as T[];
    }

    /// <summary>
    /// Возвращает список найденных атрибутов указанного типа.
    /// </summary>
    /// <typeparam name="T">Тип атрибута.</typeparam>
    /// <param name="obj">Объект, в типе которого производится поиск атрибутов.</param>
    /// <param name="inherit">Признак поиска атрибутов в родителях типа obj.</param>
    /// <returns>Список атрибутов.</returns>
    [DebuggerStepThrough]
    public static T[] GetAttributes<T>( [NotNull] this object obj, bool inherit = true )
      where T : Attribute
    {
      return obj.GetMemberInfo().GetAttributes<T>( inherit );
    }
    #endregion

    #region GetCategory
    /// <summary>
    /// Возвращает значение атрибута CategoryAttribute для объекта.
    /// </summary>
    /// <param name="obj">Объект.</param>
    /// <param name="inherit">Признак поиска атрибута в родителях типа obj.</param>
    /// <param name="defaultValue">Значение, возвращаемое при отсутствии атрибута CategoryAttribute.</param>
    /// <returns>Значение атрибута CategoryAttribute.</returns>
    [DebuggerStepThrough]
    public static string GetCategory( [NotNull] this object obj, bool inherit = true, string defaultValue = null )
    {
      return obj.GetMemberInfo().GetDescription( inherit, defaultValue );
    }

    /// <summary>
    /// Возвращает значение атрибута CategoryAttribute для указанного элемента.
    /// </summary>
    /// <param name="element">Элемент.</param>
    /// <param name="inherit">Признак поиска атрибута в родителях element.</param>
    /// <param name="defaultValue">Значение, возвращаемое при отсутствии атрибута CategoryAttribute.</param>
    /// <returns>Значение атрибута CategoryAttribute.</returns>
    [DebuggerStepThrough]
    public static string GetCategory( [NotNull] this MemberInfo element, bool inherit = true, string defaultValue = null )
    {
      var cat = element.GetAttribute<CategoryAttribute>( inherit );

      return cat != null ? cat.Category : (defaultValue ?? element.Name);
    }
    #endregion

    #region GetDescription
    /// <summary>
    /// Возвращает значение атрибута DescriptionAttribute для объекта.
    /// </summary>
    /// <param name="obj">Объект.</param>
    /// <param name="inherit">Признак поиска атрибута в родителях типа obj.</param>
    /// <param name="defaultValue">Значение, возвращаемое при отсутствии атрибута DescriptionAttribute.</param>
    /// <returns>Значение атрибута DescriptionAttribute.</returns>
    [DebuggerStepThrough]
    public static string GetDescription( [NotNull] this object obj, bool inherit = true, string defaultValue = null )
    {
      return obj.GetMemberInfo().GetDescription( inherit, defaultValue );
    }

    /// <summary>
    /// Возвращает значение атрибута DescriptionAttribute для указанного элемента.
    /// </summary>
    /// <param name="element">Элемент.</param>
    /// <param name="inherit">Признак поиска атрибута в родителях element.</param>
    /// <param name="defaultValue">Значение, возвращаемое при отсутствии атрибута DescriptionAttribute.</param>
    /// <returns>Значение атрибута DescriptionAttribute.</returns>
    [DebuggerStepThrough]
    public static string GetDescription( [NotNull] this MemberInfo element, bool inherit = true, string defaultValue = null )
    {
      var descr = element.GetAttribute<DescriptionAttribute>( inherit );

      return descr != null ? descr.Description : (defaultValue ?? element.Name);
    }
    #endregion

    #region GetDisplayName
    /// <summary>
    /// Возвращает значение атрибута DisplayNameAttribute для объекта.
    /// </summary>
    /// <param name="obj">Объект.</param>
    /// <param name="inherit">Признак поиска атрибута в родителях типа obj.</param>
    /// <param name="defaultValue">Значение, возвращаемое при отсутствии атрибута DisplayNameAttribute.</param>
    /// <returns>Значение атрибута DisplayNameAttribute.</returns>
    [DebuggerStepThrough]
    public static string GetDisplayName( [NotNull] this object obj, bool inherit = true, string defaultValue = null )
    {
      return obj.GetMemberInfo().GetDisplayName( inherit, defaultValue );
    }

    /// <summary>
    /// Возвращает значение атрибута DisplayNameAttribute для указанного элемента.
    /// </summary>
    /// <param name="element">Элемент.</param>
    /// <param name="inherit">Признак поиска атрибута в родителях element.</param>
    /// <param name="defaultValue">Значение, возвращаемое при отсутствии атрибута DisplayNameAttribute.</param>
    /// <returns>Значение атрибута DisplayNameAttribute.</returns>
    [DebuggerStepThrough]
    public static string GetDisplayName( [NotNull] this MemberInfo element, bool inherit = true, string defaultValue = null )
    {
      var dn = element.GetAttribute<DisplayNameAttribute>( inherit );

      return dn != null ? dn.DisplayName : (defaultValue ?? element.Name);
    }
    #endregion

    #region IsDefined
    /// <summary>
    /// Проверяет, помечен ли элемент указанным атрибутом.
    /// </summary>
    /// <typeparam name="T">Тип атрибута.</typeparam>
    /// <param name="element">Элемент, в котором производится поиск атрибута.</param>
    /// <param name="inherit">Признак поиска атрибута в родителях element.</param>
    /// <returns>True - элемент помечен атрибутом, иначе - False.</returns>
    public static bool IsDefined<T>( [NotNull] this MemberInfo element, bool inherit = true )
      where T : Attribute
    {
      return Attribute.IsDefined( element, typeof( T ), inherit );
    }

    /// <summary>
    /// Проверяет, помечен ли тип объекта указанным атрибутом.
    /// </summary>
    /// <typeparam name="T">Тип атрибута.</typeparam>
    /// <param name="obj">Объект, в типе которого производится поиск атрибута.</param>
    /// <param name="inherit">Признак поиска атрибута в родителях типа obj.</param>
    /// <returns>True - тип объекта помечен атрибутом, иначе - False.</returns>
    public static bool IsDefined<T>( [NotNull] this object obj, bool inherit = true )
      where T : Attribute
    {
      return obj.GetMemberInfo().IsDefined<T>( inherit );
    }
    #endregion
  }
}
