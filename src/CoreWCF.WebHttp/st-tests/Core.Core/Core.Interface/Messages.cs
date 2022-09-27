using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ST.Utils;

namespace ST.Core
{
  /// <summary>
  /// Базовый класс сообщения.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Interface.Constants.SERVER_NAMESPACE )]
  [KnownType( typeof( CommunicationMessage ) )]
  public abstract class BaseMessage
  {
  }

  /// <summary>
  /// Базовый класс коммуникационного сообщения.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Interface.Constants.SERVER_NAMESPACE )]
  [KnownType( "GetCommunicationMessages" )]
  public class CommunicationMessage : BaseMessage
  {
    #region .Static Fields
    internal static long MessageId;
    #endregion

    #region .Properties
    /// <summary>
    /// Идентификатор коммуникационного сообщения.
    /// </summary>
    [DataMember]
    public long Id { get; internal set; }
    #endregion

    #region GetCommunicationMessages
    /// <summary>
    /// Возвращает список всех найденных в сборках домена коммуникационных сообщений.
    /// </summary>
    /// <returns>Список коммуникационных сообщений.</returns>
    public static IEnumerable<Type> GetCommunicationMessages()
    {
      return AssemblyHelper.GetSubtypes( false, new [] { typeof( CommunicationMessage ) }, typeof( PlatformAssemblyAttribute ) );
    }
    #endregion

    #region GetMessageTypeName
    /// <summary>
    /// Возвращает название типа коммуникационного сообщения.
    /// </summary>
    /// <param name="type">Тип коммуникационного сообщения.</param>
    /// <returns>Название типа коммуникационного сообщения.</returns>
    public static string GetMessageTypeName( Type type )
    {
      var attribute = type.GetAttribute<DataContractAttribute>( false );

      return attribute != null && !string.IsNullOrEmpty( attribute.Name ) ? attribute.Name : type.FullName;
    }
    #endregion

    internal sealed class Wrapper
    {
      #region .Properties
      internal CommunicationMessage Message { get; set; }

      internal DateTime ExpirationTime { get; set; }
      #endregion
    }
  }

  /// <summary>
  /// Сообщение рассылается при изменении статуса соединения с сервером.
  /// </summary>
  [Serializable]
  public sealed class ConnectionStatusChangedMessage : BaseMessage
  {
    #region .Properties
    /// <summary>
    /// Состояние соединения с сервером.
    /// </summary>
    public bool IsConnected { get; set; }
    #endregion
  }

  /// <summary>
  /// Базовый класс нетипизированного коммуникационного сообщения.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Interface.Constants.SERVER_NAMESPACE )]
  public class UntypedCommunicationMessage : CommunicationMessage
  {
    #region .Properties
    /// <summary>
    /// Название типа сообщения.
    /// </summary>
    [DataMember]
    public string Type { get; internal set; }

    /// <summary>
    /// Тело сообщения.
    /// </summary>
    [DataMember]
    public string Body { get; internal set; }
    #endregion
  }

  /// <summary>
  /// Класс-обертка коммуникационного сообщения для модуля диспетчера сообщений.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Interface.Constants.SERVER_NAMESPACE )]
  public class MsgDispatcherMessage : CommunicationMessage
  {
    #region .Properties
    /// <summary>
    /// Идентификатор исходного сообщения.
    /// </summary>
    [DataMember]
    public new long MessageId { get; set; }

    /// <summary>
    /// Название типа сообщения.
    /// </summary>
    [DataMember]
    public int TypeId { get; set; }

    /// <summary>
    /// Идентификатор объекта.
    /// </summary>
    [DataMember]
    public int MessageObjId { get; set; }

    /// <summary>
    /// Тип объекта.
    /// </summary>
    [DataMember]
    public MessageObjectType MessageObjectType { get; set; }

    /// <summary>
    /// Сообщение.
    /// </summary>
    [DataMember]
    public object Message { get; set; }

    ///// <summary>
    ///// Фильтр.
    ///// </summary>
    //[DataMember]
    //public Func<int, bool> Filter{ get; set; }    // Проблема с сериализацией-десериализацией делегата. Вместо него добавлено свойство RecipientUserIds, чтобы не передавать фильтр, но передавать значения для фильтрации.

    /// <summary>
    /// Идентификаторы получателей сообщения.
    /// </summary>
    [DataMember]
    public int[] RecipientUserIds { get; set; }
    #endregion
  }
}
