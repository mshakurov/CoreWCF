using CoreWCF;

using System.Globalization;

namespace ST.Utils.Exceptions
{


  /// <summary>
  /// Интерфейс поставщика Fault-исключений.
  /// </summary>
  public interface IFaultExceptionProvider
  {
    /// <summary>
    /// Возвращает Fault-исключение.
    /// </summary>
    /// <param name="culture">Культура для которой необходимо вернуть исключение.</param>
    /// <rereturns>Fault-исключение.</rereturns>
    FaultException GetFaultException( CultureInfo culture );
  }


}