using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

using ST.Utils.Attributes;

namespace ST.Utils.Wcf
{
  /// <summary>
  /// Вспомогательные extension-методы.
  /// </summary>
  public static class Extensions
  {
    public static T Find<T>(this KeyedCollection<Type, IOperationBehavior> collection)
    {
      for (int i = 0; i < collection.Count; i++)
      {
        IOperationBehavior val = collection[i];
        if (val is T)
        {
          return (T)(object)val;
        }
      }

      return default(T);
    }

    #region ExtendOperations
    /// <summary>
    /// Устанавливает для всех операций точки входа в сервис следующие значения свойств:
    /// IgnoreExtensionDataObject = true,
    /// MaxItemsInObjectGraph = int.MaxValue.
    /// </summary>
    /// <param name="endPoint">Точка входа сервиса.</param>
    public static void ExtendOperations( [NotNull] this ServiceEndpoint endPoint )
    {
      endPoint.Contract.Operations.ForEach( od => od.OperationBehaviors.Find<DataContractSerializerOperationBehavior>().IfNotNull( dcob =>
      {
        dcob!.MaxItemsInObjectGraph = int.MaxValue;
      } ) );
    }
    #endregion

    #region GetBinding
    /// <summary>
    /// Возвращает компоновку для указанного типа протокола.
    /// </summary>
    /// <param name="protocolType">Тип протокола.</param>
    /// <param name="nameSpace">Пространство имен.</param>
    /// <returns>Компоновка.</returns>
    public static Binding GetBinding( this WcfProtocolType protocolType, string nameSpace )
    {
      return protocolType == WcfProtocolType.Tcp ? BindingHelper.GetBinding<NetTcpBinding>( nameSpace ) :
             protocolType == WcfProtocolType.WinTcp ? BindingHelper.GetBinding<NetTcpBinding>( nameSpace, ST.Utils.Wcf.BindingHelper.AuthenticationType.Windows ) :
             protocolType == WcfProtocolType.Http ? BindingHelper.GetBinding<WSHttpBinding>( nameSpace ) :
             protocolType == WcfProtocolType.BasicHttp ? BindingHelper.GetBinding<BasicHttpBinding>( nameSpace ) :
             protocolType == WcfProtocolType.SecHttp ? BindingHelper.GetBinding<BasicHttpBinding>( nameSpace: nameSpace, useTransportLevelSecurity: true) :
             protocolType == WcfProtocolType.WinSecHttp ? BindingHelper.GetBinding<BasicHttpBinding>( nameSpace, ST.Utils.Wcf.BindingHelper.AuthenticationType.Windows, true ) :
             protocolType == WcfProtocolType.WinHttp ? BindingHelper.GetBinding<BasicHttpBinding>( nameSpace, ST.Utils.Wcf.BindingHelper.AuthenticationType.Windows ) :
             protocolType == WcfProtocolType.OpenHttp ? BindingHelper.GetBinding<BasicHttpBinding>( nameSpace ) :

             null as Binding;
    }
    #endregion

    #region GetHeader
    /// <summary>
    /// Возвращает значение заголовка сообщения.
    /// </summary>
    /// <typeparam name="T">Тип значения заголовка.</typeparam>
    /// <param name="msg">Сообщение.</param>
    /// <param name="name">Название заголовка.</param>
    /// <param name="ns">Пространство имен.</param>
    /// <returns>Значение заголовка.</returns>
    public static T GetHeader<T>( [NotNull] this Message msg, [NotNullNotEmpty] string name, [NotNullNotEmpty] string ns )
    {
      var index = msg.Headers.FindHeader( name, ns );

      return index >= 0 ? msg.Headers.GetHeader<T>( index ) : default( T );
    }
    #endregion

    #region IsEqual
    /// <summary>
    /// Проверяет эквивалентность двух экземпляров типа FaultCode.
    /// </summary>
    /// <param name="srcCode">Проверяемый экземпляр.</param>
    /// <param name="code">Проверочный экземпляр.</param>
    /// <returns>Экземпляры совпадают.</returns>
    public static bool IsEqual( this FaultCode srcCode, FaultCode code )
    {
      return srcCode.Name == code.Name && srcCode.Namespace == code.Namespace;
    }
    #endregion

    #region RemoveHeader
    /// <summary>
    /// Удаляет заголовок сообщения.
    /// </summary>
    /// <param name="msg">Сообщение.</param>
    /// <param name="name">Название заголовка.</param>
    /// <param name="ns">Пространство имен.</param>
    public static void RemoveHeader( [NotNull] this Message msg, [NotNullNotEmpty] string name, [NotNullNotEmpty] string ns )
    {
      var index = msg.Headers.FindHeader( name, ns );

      if( index >= 0 )
        msg.Headers.RemoveAt( index );
    }
    #endregion
  }
}
