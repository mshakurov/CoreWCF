using CoreWCF;
using CoreWCF.Channels;

using System;
using System.ComponentModel;
using System.Xml;

namespace ST.Utils.Wcf
{
  /// <summary>
  /// Класс для создания/изменения компоновки.
  /// </summary>
  public static class BindingHelper
  {
    #region GetBinding
    /// <summary>
    /// Создает и инициализирует компоновку.
    /// </summary>
    /// <typeparam name="T">Тип создаваемой компоновки.</typeparam>
    /// <param name="nameSpace">Название пространства имен.</param>
    /// <param name="authenticationType">Тип аутентификации.</param>
    /// <param name="useTransportLevelSecurity">use TransportLevelSecurity</param>
    /// <returns>Объект требуемой компоновки.</returns>
    public static T GetBinding<T>(string nameSpace = Constants.BASE_NAMESPACE, AuthenticationType authenticationType = AuthenticationType.None, bool useTransportLevelSecurity = false)
      where T : Binding, new()
    {
      var b = new T();

      b.GetSetter<string>("Namespace")(nameSpace);
      b.GetSetter<bool>("PortSharingEnabled")(true);
      b.GetSetter<TimeSpan>("ReceiveTimeout")(TimeSpan.MaxValue);
      b.GetSetter<TimeSpan>("SendTimeout")(TimeSpan.MaxValue);
      b.GetSetter<long>("MaxBufferPoolSize")(134217728);
      b.GetSetter<long>("MaxReceivedMessageSize")(int.MaxValue);

      if (typeof(T).IsInheritedFrom(typeof(NetTcpBinding)))
        b.GetSetter<int>("MaxConnections")(1000);
      else
        if (typeof(T).IsInheritedFrom(typeof(WebHttpBinding)))
        b.GetSetter<bool>("CrossDomainScriptAccessEnabled")(true);

      var readerQuotas = b.GetGetter<XmlDictionaryReaderQuotas>("ReaderQuotas")();

      readerQuotas.GetSetter<int>("MaxArrayLength")(int.MaxValue);
      readerQuotas.GetSetter<int>("MaxBytesPerRead")(int.MaxValue);
      readerQuotas.GetSetter<int>("MaxDepth")(int.MaxValue);
      readerQuotas.GetSetter<int>("MaxNameTableCharCount")(int.MaxValue);
      readerQuotas.GetSetter<int>("MaxStringContentLength")(int.MaxValue);

      var security = b.GetGetter<object>("Security")();

      if (security is NetTcpSecurity netTcpSecurity)
      {
        if (authenticationType == AuthenticationType.Windows)
        {
          netTcpSecurity.Mode = SecurityMode.Transport;
          netTcpSecurity.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
        }
        else
          netTcpSecurity.Mode = SecurityMode.None;
      }
      else
        if (security is WSHTTPSecurity wsHttpSecurity)
      {
        wsHttpSecurity.Mode = SecurityMode.Transport;
        wsHttpSecurity.Transport.ClientCredentialType = HttpClientCredentialType.None;
      }
      else
          if (security is BasicHttpSecurity basicHttpSecurity)
      {

        basicHttpSecurity.Mode = useTransportLevelSecurity ? BasicHttpSecurityMode.Transport : BasicHttpSecurityMode.TransportCredentialOnly;
        basicHttpSecurity.Transport.ClientCredentialType = authenticationType == AuthenticationType.Windows ? HttpClientCredentialType.Windows :
                                                            authenticationType == AuthenticationType.Basic ? HttpClientCredentialType.Basic : HttpClientCredentialType.None;
      }
      else
        if (security is WebHttpSecurity webHttpSecurity)
      {
        //if (typeof(T).IsInheritedFrom(typeof(CustomWebHttpBinding)))
        //{
        //  if (authenticationType != AuthenticationType.None)
        //    b.GetSetter<bool>("CrossDomainScriptAccessEnabled")(false);

          //webHttpSecurity.Mode = useTransportLevelSecurity ? WebHttpSecurityMode.Transport : authenticationType == AuthenticationType.None ? WebHttpSecurityMode.None : WebHttpSecurityMode.TransportCredentialOnly;

          //webHttpSecurity.Transport.ClientCredentialType = authenticationType == AuthenticationType.Windows ? HttpClientCredentialType.Windows :
          //                                                authenticationType == AuthenticationType.Basic ? HttpClientCredentialType.Basic : HttpClientCredentialType.None;
        //}
        //else
        {
          webHttpSecurity.Mode = useTransportLevelSecurity ? WebHttpSecurityMode.Transport : WebHttpSecurityMode.None;

          webHttpSecurity.Transport.ClientCredentialType = HttpClientCredentialType.None;
        }
      }
      //else
      //        if (security is NetNamedPipeSecurity)
      //  (security as NetNamedPipeSecurity).Mode = NetNamedPipeSecurityMode.None;

      return b;
    }
    #endregion


    /// <summary>
    /// Тип аутентификации.
    /// </summary>
    public enum AuthenticationType
    {
      #region .Static Fields
      None,
      Windows,
      Basic
      #endregion
    }

    /// <summary>
    /// Тип получения контекста пользователя.
    /// </summary>
    public enum ContextUserType
    {
      #region .Static Fields
      None,
      SessionId,
      IP,
      Login
      #endregion
    }

    /// <summary>
    /// Тип обмена информацией.
    /// </summary>
    public enum ProtocolType
    {
      #region .Static Fields
      Soap,
      Json
      #endregion
    }

    /// <summary>
    /// Тип передачи данных.
    /// </summary>
    public enum TransferType
    {
      #region .Static Fields
      [Description("net.tcp")]
      Tcp,
      [Description("http")]
      Http
      #endregion
    }

    /// <summary>
    /// Тип сжатия.
    /// </summary>
    public enum ZippedType
    {
      #region .Static Fields
      None,
      Read,
      Write,
      All
      #endregion
    }
  }
}
