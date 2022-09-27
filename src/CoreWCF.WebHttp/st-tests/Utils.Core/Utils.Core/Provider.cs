
using System;
namespace ST.Utils
{
  /// <summary>
  /// Интерфейс поставщика сервисов, поддерживающего контекст.
  /// </summary>
  public interface IContextServiceProvider
  {
    /// <summary>
    /// Возвращает сервис указанного типа.
    /// </summary>
    /// <typeparam name="T">Тип сервиса.</typeparam>
    /// <param name="context">Контекст.</param>
    /// <returns>Сервис.</returns>
    T GetService<T>( ProviderContext context )
      where T : class;
  }

  /// <summary>
  /// Базовый класс контекста поставщика.
  /// </summary>
  [Serializable]
  public abstract class ProviderContext
  {
  }
}
