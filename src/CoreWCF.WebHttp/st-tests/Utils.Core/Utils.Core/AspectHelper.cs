using System.Diagnostics;
using PostSharp;
using PostSharp.Extensibility;

namespace ST.Utils
{
  /// <summary>
  /// Вспомогательный класс для работы с PostSharp.
  /// </summary>
  public static class AspectHelper
  {
    #region Fail
    /// <summary>
    /// Выводит сообщение об ошибке компиляции и возвращает False.
    /// Код ошибки будет сформирован следующим образом: {Название типа, вызывающего данный метод}#{number}.
    /// </summary>
    /// <param name="number">Номер ошибки.</param>
    /// <param name="message">Описание ошибки.</param>
    /// <param name="arguments">Параметры описания ошибки.</param>
    /// <returns>False.</returns>
    public static bool Fail( int number, string message, params object[] arguments )
    {
      return Fail( string.Format( "{0}#{1}", (new StackTrace()).GetFrame( 1 ).GetMethod().DeclaringType.Name, number ), message, arguments );
    }

    /// <summary>
    /// Выводит сообщение об ошибке компиляции и возвращает False.
    /// </summary>
    /// <param name="errorCode">Код ошибки.</param>
    /// <param name="message">Описание ошибки.</param>
    /// <param name="arguments">Параметры описания ошибки.</param>
    /// <returns>False.</returns>
    public static bool Fail( string errorCode, string message, params object[] arguments )
    {
      return Fail( MessageLocation.Unknown, errorCode, message, arguments );
    }

    /// <summary>
    /// Выводит сообщение об ошибке компиляции и возвращает False.
    /// </summary>
    /// <param name="messageLocation">Местоположение ошибки.</param>
    /// <param name="errorCode">Код ошибки.</param>
    /// <param name="message">Описание ошибки.</param>
    /// <param name="arguments">Параметры описания ошибки.</param>
    /// <returns>False.</returns>
    public static bool Fail( MessageLocation messageLocation, string errorCode, string message, params object[] arguments )
    {
      Message.Write( messageLocation, SeverityType.Error, errorCode, message, arguments );

      return false;
    }
    #endregion
  }
}
