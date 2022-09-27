using System;
using System.Runtime.Serialization;

using ST.Utils;

namespace ST.EventLog.Server
{
    /// <summary>
    /// Событие.
    /// </summary>
    [DataContract(Namespace = Constants.MODULE_NAMESPACE)]
    [Serializable]
    public class Event
    {
        #region .Fields
        private DateTime time;
        private DateTime?[] resolveTime;
        #endregion

        #region .Properties
        /// <summary>
        /// Идентификатор.
        /// </summary>
        [DataMember]
        public int Id { get; protected set; }

        /// <summary>
        /// Идентификатор типа.
        /// </summary>
        [DataMember]
        public int TypeId { get; protected set; }

        /// <summary>
        /// Идентификатор уровня.
        /// </summary>
        [DataMember]
        public EventLevel Level { get; protected set; } = EventLevel.Information;

        /// <summary>
        /// Идентификатор источника.
        /// </summary>
        [DataMember]
        public int SourceId { get; protected set; }

        /// <summary>
        /// Идентификатор категории.
        /// </summary>
        [DataMember]
        public int CategoryId { get; protected set; }

        /// <summary>
        /// Текст.
        /// </summary>
        [DataMember]
        public string Text { get; set; }

        /// <summary>
        /// Детали.
        /// </summary>
        [DataMember]
        public string Detail { get; protected set; }

        /// <summary>
        /// Время.
        /// </summary>
        [DataMember]
        public DateTime Time
        {
            get { return time; }
            protected set { time = value.AddMilliseconds(-value.Millisecond); }
        }

        /// <summary>
        /// Идентификатор типа реакции.
        /// </summary>
        [DataMember]
        public EventReaction Reaction { get; protected set; } = EventReaction.None;

        /// <summary>
        /// Состояние события с учетом обработки пользователем.
        /// </summary>
        [DataMember]
        public EventState State { get; protected set; } = EventState.ProcessNotRequired;

        /// <summary>
        /// Состояние события.
        /// </summary>
        [DataMember]
        [Dbi.BindNone]
        public EventState InternalState { get; set; } = EventState.ProcessNotRequired;

        /// <summary>
        /// Идентификатор группы организаций.
        /// </summary>
        [IgnoreDataMember]
        public int? OrgGroupId { get; protected set; }

        /// <summary>
        /// Идентификатор пользователя, отработавшего событие.
        /// </summary>
        [DataMember]
        public int[] ResolveUserId { get; set; }

        /// <summary>
        /// Пользователь, отработавший событие.
        /// </summary>
        [DataMember]
        public string[] ResolveUser { get; set; }

        /// <summary>
        /// Время когда было отработано событие.
        /// </summary>
        [DataMember]
        public DateTime?[] ResolveTime
        {
            get { return resolveTime; }
            set { SetResolveTimes(value); }
        }

        /// <summary>
        /// Комментарий, оставленный пользователем при отработке события.
        /// </summary>
        [DataMember]
        public string[] ResolveDescription { get; set; }

        /// <summary>
        /// Идентификатор транспортного средства.
        /// </summary>
        [IgnoreDataMember]
        public int? MonObjId { get; set; }

        /// <summary>
        /// Водитель.
        /// </summary>
        [DataMember]
        public string DriverName { get; set; }

        /// <summary>
        /// Признак наличия номера телефона.
        /// </summary>
        [DataMember]
        public bool HasPhoneNumber { get; set; }

        /// <summary>
        /// Время отображения.
        /// </summary>
        [DataMember]
        public DateTime? Expiration { get; set; }
        #endregion

        #region GetDetail
        /// <summary>
        /// Возвращает объект с деталями события.
        /// </summary>
        /// <returns>Событие.</returns>
        public EventBase GetDetail()
        {
            return string.IsNullOrEmpty(Detail) ? null : EventBase.GetDetail(EventBase.GetEventTypes().GetValue(TypeId), Detail);
        }
        #endregion

        #region SetResolveTimes
        private void SetResolveTimes(DateTime?[] times)
        {
            if (times != null)
                for (int i = 0; i < times.Length; i++)
                {
                    times[i] = times[i].HasValue ? times[i].Value.AddMilliseconds(-times[i].Value.Millisecond) : times[i];
                }

            resolveTime = times;
        }
        #endregion

        #region Equals
        public override bool Equals(object obj)
        {
            var evt = obj as Event;

            return evt != null && evt.Id == Id;
        }
        #endregion

        #region GetHashCode
        public override int GetHashCode()
        {
            return Id;
        }
        #endregion

        #region ToString
        public override string ToString()
        {
            return Text;
        }
        #endregion
    }
}
