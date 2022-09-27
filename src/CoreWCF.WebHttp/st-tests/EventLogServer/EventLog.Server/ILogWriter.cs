using System;
using System.Data;

using CoreWCF.Configuration;

using Microsoft.AspNetCore.Builder;

using ST.Core;
using ST.Utils;

namespace ST.EventLog.Server
{
    /// <summary>
    /// Протоколирование событий.
    /// </summary>
    public sealed partial class EventLog : ILogWriter
    {
        #region .Fields
        private DateTime? _lastUDPSignal;
        private object _syncRoot = new object();
        #endregion

        #region GetTime
        private DateTime? GetTime(DateTime? time)
        {
            return time == DateTime.MinValue ? null : time;
        }
        #endregion

        #region SendEvent
        private void SendEvent(EventBaseMessage message)
        {
            //Send( message, msg => ISecurityManager.ExecForUser( userId => ServerContext.Session.Permissions.Contains<Permissions.EventViewAll>() ? true : DB.GetScalar<bool>( "[Access].[Exists]", msg.TypeId, userId ) ) );
        }
        #endregion

        #region GetIdentifierUsers
        private DataTable GetIdentifierUsers(EventBase evt)
        {
            var userIds = evt.GetAvailableUserIds();

            if (userIds == null)
                userIds = new int[] { -1 };
            else
            {
                for (int i = 0; i < _filterEventUserFuncList.Count; i++)
                {
                    if (_filterEventUserFuncList[i] != null)
                        userIds = _filterEventUserFuncList[i](evt, userIds);
                }
            }

            return InitializeIdentifiers(userIds, ref _identifierUsers);
        }

        public static EventLog CreateModule(WebApplication app, IServiceBuilder builder)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region WriteEvent
        private Event WriteEvent<TSource, TCategory>(EventBase evtBase, DateTime? time, EventLevel level, int? expiration, Func<object, string, string> serializer = null)
      where TSource : EventSource
      where TCategory : EventCategory
        {
            //if ( ISecurityManager.ExecForUser( userId => ServerContext.Session.Permissions.Contains<Permissions.IgnoreEventWrite>() ) )
            //  return null;

            var detail = (serializer ?? Serializer.SerializeXml)(evtBase, Constants.ROOT_ELEMENT_NAME);

            var evt = DB.RS.Single<Event>("[Event].[Add]", evtBase.GetCode(), time, level, EventSource.GetCode<TSource>(), EventCategory.GetCode<TCategory>(), detail, evtBase.GetOrgGroupId(), GetIdentifierUsers(evtBase), expiration);

            EventToLogEvent<TSource, TCategory>(evt.Id, evtBase, time ?? DateTime.UtcNow, level);

            SendEvent(new EventLoggedMessage { EventId = evt.Id, TypeId = evt.TypeId, OrgGroupId = evt.OrgGroupId });

            lock (_syncRoot)
            {
                var timeNow = DateTime.UtcNow;

                if (level == EventLevel.Alert && (!_lastUDPSignal.HasValue || _lastUDPSignal.Value.AddMinutes(5) < timeNow))
                {
                    SendUDPSignal("ST_IDEA_EVT_Alarm");

                    _lastUDPSignal = timeNow;
                }
            }

            return evt;
        }
        #endregion

        #region IEventWriter.Write
        /// <summary>
        /// Выполняет логирование события. (в строковых данных события запрещается использование встроенных символов xml)
        /// </summary>
        /// <param name="evt">Событие.</param>
        /// <param name="time">Время регистрации события.</param>
        /// <param name="level">Уровень событие.</param>
        /// <param name="expiration">Протухание</param>
        Event ILogWriter.Write(EventBase evt, DateTime? time, EventLevel level, short? expiration)
        {
            return WriteEvent<EventSourceNone, EventCategoryNone>( //evt.GetCode(), 
              evt, GetTime(time), level
              //, EventSource.GetCode<EventSourceNone>(), EventCategory.GetCode<EventCategoryNone>(), Serializer.SerializeXml( evt, Constants.ROOT_ELEMENT_NAME ), evt.GetOrgGroupId(), GetIdentifierUsers( evt )
              , expiration);
        }

        /// <summary>
        /// Выполняет логирование события. (в строковых данных события запрещается использование встроенных символов xml)
        /// </summary>
        /// <typeparam name="T">Тип источника события.</typeparam>
        /// <param name="evt">Событие.</param>
        /// <param name="time">Время регистрации события.</param>
        /// <param name="level">Уровень событие.</param>
        /// <param name="expiration">Протухание</param>
        Event ILogWriter.Write<T>(EventBase evt, DateTime? time, EventLevel level, short? expiration)
        {
            return WriteEvent<T, EventCategoryNone>( //evt.GetCode(), 
              evt, GetTime(time), level
              //, EventSource.GetCode<T>(), EventCategory.GetCode<EventCategoryNone>(), Serializer.SerializeXml( evt, Constants.ROOT_ELEMENT_NAME ), evt.GetOrgGroupId(), GetIdentifierUsers( evt )
              , expiration);
        }

        /// <summary>
        /// Выполняет логирование события. (в строковых данных события запрещается использование встроенных символов xml)
        /// </summary>
        /// <typeparam name="TSource">Тип источника события.</typeparam>
        /// <typeparam name="TGategory">Тип категории события.</typeparam>
        /// <param name="evt">Событие.</param>
        /// <param name="time">Время регистрации события.</param>
        /// <param name="level">Уровень событие.</param>
        /// <param name="expiration">Протухание</param>
        Event ILogWriter.Write<TSource, TGategory>(EventBase evt, DateTime? time, EventLevel level, int? expiration)
        {
            return WriteEvent<TSource, TGategory>( //evt.GetCode(), 
              evt, GetTime(time), level
              //, EventSource.GetCode<TSource>(), EventCategory.GetCode<TGategory>(), Serializer.SerializeXml( evt, Constants.ROOT_ELEMENT_NAME ), evt.GetOrgGroupId(), GetIdentifierUsers( evt )
              , expiration);
        }

        /// <summary>
        /// Сохраняет событие, допуская любые символы в строковых значениях
        /// </summary>
        /// <typeparam name="TSource">Тип источника события.</typeparam>
        /// <typeparam name="TCategory">Тип категории события.</typeparam>
        /// <param name="evt">Событие.</param>
        /// <param name="time">Время регистрации события.</param>
        /// <param name="level">Уровень событие.</param>
        /// <param name="expiration">Протухание</param>
        /// <returns>Сохраненная версия события</returns>
        Event ILogWriter.Write2<TSource, TCategory>(EventBase evt, DateTime? time, EventLevel level, int? expiration)
        {
            return WriteEvent<TSource, TCategory>( //evt.GetCode()
              evt, GetTime(time), level
              //, EventSource.GetCode<TSource>(), EventCategory.GetCode<TCategory>(), Serializer.SerializeXml2( evt, Constants.ROOT_ELEMENT_NAME ), evt.GetOrgGroupId(), GetIdentifierUsers( evt )
              , expiration
              , Serializer.SerializeXml2);
        }
        #endregion

        #region EventLevelToServerLogEntryType
        private ServerLogEntryType EventLevelToServerLogEntryType(EventLevel level)
        {
            switch (level)
            {
                case EventLevel.Information:
                    return ServerLogEntryType.Information;
                case EventLevel.Warning:
                    return ServerLogEntryType.Warning;
                case EventLevel.Alert:
                case EventLevel.Error:
                    return ServerLogEntryType.Error;
                default:
                    return ServerLogEntryType.Warning;
            }
        }
        #endregion

        #region EventToLogEvent
        private void EventToLogEvent<TSource, TCategory>(int eventId, EventBase evtBase, DateTime time, EventLevel level)
          where TSource : EventSource
          where TCategory : EventCategory
        {
            if (!Configuration.LogToWindowsEventLog)
                return;

            _eventToLogEventTask.Enqueue(new EventToLogEventItem(typeof(TSource).GetDisplayName(), typeof(TCategory).GetDisplayName(), evtBase.GetType().GetDisplayName(), eventId, time, EventLevelToServerLogEntryType(level), evtBase));
        }
        #endregion

        #region class EventToLogEventItem
        private class EventToLogEventItem
        {
            public readonly int EventId;
            public readonly DateTime Time;
            public readonly ServerLogEntryType Level;
            public readonly string SourceName;
            public readonly string CategoryName;
            public readonly string TypeName;

            public readonly EventBase EventBase;

            public EventToLogEventItem(string sourceName, string categoryName, string typeName, int eventId, DateTime time, ServerLogEntryType level, EventBase eventBase = null)
            {
                SourceName = sourceName;
                CategoryName = categoryName;
                TypeName = typeName;
                EventId = eventId;
                Time = time;
                Level = level;

                EventBase = eventBase;
            }
        }
        #endregion
    }
}
