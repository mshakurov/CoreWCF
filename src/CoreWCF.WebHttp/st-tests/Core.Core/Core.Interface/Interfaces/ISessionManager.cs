using System;
using System.Collections.Generic;

namespace ST.Core
{
  /// <summary>
  /// Интерфейс сервера, поддерживающего работу с данными сессии.
  /// </summary>
  [ServerInterface]
  public interface ISessionManager
  {
    /// <summary>
    /// Возвращает первый найденный объект, хранящийся в сессии.
    /// </summary>
    /// <typeparam name="T">Тип объекта.</typeparam>
    /// <returns>Экземпляр объекта.</returns>
    T Get<T>()
      where T : class;

    /// <summary>
    /// Возвращает список объектов, хранящихся в сессии.
    /// </summary>
    /// <typeparam name="T">Тип объекта.</typeparam>
    /// <returns>Список экземпляров объектов.</returns>
    List<T> GetMany<T>()
      where T : class;

    /// <summary>
    /// Удаляет объект из сессии.
    /// </summary>
    /// <typeparam name="T">Тип объекта.</typeparam>
    void Remove<T>()
      where T : class;

    /// <summary>
    /// Сохраняет объект в сессии.
    /// </summary>
    /// <typeparam name="T">Тип объекта.</typeparam>
    /// <param name="data">Экземпляр объекта.</param>
    void Set<T>( T data )
      where T : class;
  }

  /// <summary>
  /// Вспомогательный класс для работы с сессией.
  /// </summary>
  public static class SessionManagerHelper
  {
    #region GetAndSet
    /// <summary>
    /// Сохраняет объект в сессии предварительно получив его или создав новый, если указанный тип объекта отсутствует в сессии.
    /// </summary>
    /// <typeparam name="T">Тип объекта.</typeparam>
    /// <param name="sm">Менеджер сессии.</param>
    /// <param name="modifier">Метод, модифицирующий состояние объекта.</param>
    public static void GetAndSet<T>( this ISessionManager sm, Action<T> modifier )
      where T : class, new()
    {
      var s = sm.Get<T>() ?? new T();

      modifier( s );

      sm.Set( s );
    }
    #endregion
  }
}
