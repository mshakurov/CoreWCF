using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using ST.Core;
using ST.Server;
using ST.Utils;
using ST.Utils.Attributes;
using System.Collections.Concurrent;
using ST.Utils.Threading;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace ST.EventLog.Server
{
    /// <summary>
    /// Серверная часть модуля протоколирования.
    /// </summary>
    [WcfServer(Constants.MODULE_ADDRESS, Constants.MODULE_NAMESPACE)]
    [Configurable(typeof(EventLogConfig))]
    [ModuleLoadingPriority(ModuleLoadingPriority.Highest - 1)]
    [DisplayNameLocalized(RI.ModuleName)]
    public sealed partial class EventLog : DatabaseModule_PG
    {
        #region .Static Fields
        [ThreadStatic]
        private static DataTable _identifierTypes;

        [ThreadStatic]
        private static DataTable _identifierTypesFilter;

        [ThreadStatic]
        private static DataTable _identifierLevels;

        [ThreadStatic]
        private static DataTable _identifierSources;

        [ThreadStatic]
        private static DataTable _identifierCategories;

        [ThreadStatic]
        private static DataTable _identifierStates;

        [ThreadStatic]
        private static DataTable _identifierEvents;

        [ThreadStatic]
        private static DataTable _identifierUsers;

        [ThreadStatic]
        private static DataTable _identifierMonObjs;
        #endregion

        #region .Fields
        private readonly Dictionary<Type, List<Type>> _types = new Dictionary<Type, List<Type>>();

        private readonly Dictionary<int, IEventResolver> _eventResolverList = new Dictionary<int, IEventResolver>();

        private List<Func<EventBase, int[], int[]>> _filterEventUserFuncList = new List<Func<EventBase, int[], int[]>>();
        private List<Func<Event[], int, Event[]>> _filterEventFuncList = new List<Func<Event[], int, Event[]>>();

        private ConcurrentDictionary<int, EventTypeUser[]> _userEventTypes = new ConcurrentDictionary<int, EventTypeUser[]>();

        private SimpleQueueTask<EventToLogEventItem> _eventToLogEventTask;
        #endregion

        #region .Properties
        private EventLogConfig Configuration { get; set; }

        //[ImportService]
        //private ISecurityManager ISecurityManager { get; set; }

        //[ImportService]
        //private ISecurityUser ISecurityUser { get; set; }

        //[ImportService]
        //private ITelematicsTacho ITelematicsTacho { get; set; }

        public override string DbModuleName
        {
            get
            {
                return "EventLog";
            }
        }
        #endregion

        #region .Ctor
        static EventLog()
        {
            Dbi.SetBind<Event>(obj => obj.Id, "EventId");
            Dbi.SetBind<Event>(obj => obj.Level, "LevelId");
            Dbi.SetBind<Event>(obj => obj.Reaction, "ReactionId");
            Dbi.SetBind<Event>(obj => obj.State, "StateId");
            Dbi.SetBind<Event>(obj => obj.Text, new Dbi.BindInNoneAttribute());
            Dbi.SetBind<EventTypeUser>(obj => obj.AccessType, "AccessTypeId");
            Dbi.SetBind<ProcessingInfo>(obj => obj.AccessType, "AccessTypeId");
        }
        #endregion

        #region .ctor
        public EventLog()
        {
            //Test();

            _eventToLogEventTask = new SimpleQueueTask<EventToLogEventItem>(EventToLogEventProcessItem, System.Threading.Tasks.TaskCreationOptions.LongRunning);
        }

        private void Test()
        {
            var evt1 = new Event();

            var tev1 = TestClassesUtils.CreateImplFrom(1234, EventState.PartiallyProcessed);
            var str1 = tev1.ToXmlStringWithDCS();
            var tev2 = str1.DeserializeObjectWithDCS<TestEvent>();
            Debug.Assert(tev1 == tev2, "tev1 != tev2");

            var ser = new System.Runtime.Serialization.DataContractSerializer(typeof(TestEvent), "GetResult", "http://www.space-team.com/EventLog", null);
            using var stream = new MemoryStream();
            var writer = System.Xml.XmlDictionaryWriter.CreateTextWriter(stream, System.Text.UTF8Encoding.UTF8, false);
            ser.WriteObject(writer, tev1);
            writer.Flush();
            stream.Position = 0;
            var strd1 = System.Text.UTF8Encoding.UTF8.GetString(stream.GetBuffer());
        }
        #endregion

        #region FillTypes
        private void FillTypes<T>(IEnumerable<Type> types)
        {
            var list = _types.GetOrAdd(typeof(T), t => new List<Type>());

            types.Where(t => t.IsInheritedFrom(typeof(T))).ForEach(type => list.Add(type));
        }
        #endregion

        #region InitializeIdentifiers
        private static DataTable InitializeIdentifiers(IEnumerable<int> ids, ref DataTable table)
        {
            if (table != null)
                table.Rows.Clear();
            else
            {
                table = new DataTable();

                table.Columns.Add(new DataColumn("Id", typeof(int)) { AllowDBNull = false });
            }

            var initializeTable = table;

            if (ids != null)
                ids.ForEach(id => initializeTable.Rows.Add(id));

            return table;
        }
        #endregion

        //#region OnUserRemoveMessage
        //private void OnUserRemoveMessage( UserRemoveMessage msg )
        //{
        //  DB.Execute( "[Util].[RemoveUser]", msg.UserId );
        //}
        //#endregion

        #region Initialize
        protected override void Initialize()
        {
            base.Initialize();

           DB.RS.List<EventTypeUser>("[Access].[Get]").IfNotNull(l => l.GroupBy(t => t.UserId).ForEach(g => _userEventTypes.GetOrAdd(g.Key, g.ToArray())));

            _eventToLogEventTask.Start();
        }
        #endregion

        #region UpdateUserEventTypeCache
        private void UpdateUserEventTypeCache(int userId)
        {
            var eTypes = (this as IEventLogManager).GetListByUser(userId);

            _userEventTypes.AddOrUpdate(userId, eTypes, (k, v) => eTypes);
        }
        #endregion

        #region PostInitialize
        [MethodImpl(MethodImplOptions.NoInlining)]
        protected override void PostInitialize()
        {
            base.PostInitialize();

            EventUtils.Initialize(GetEventByUserId);

            //Subscribe<UserRemoveMessage>( OnUserRemoveMessage );

            var typeList = _types.GetOrAdd(typeof(EventBase), t => new List<Type>());

            EventBase.GetEventTypes().IfNotNull(eventTypes => eventTypes.ForEach(type =>
              {
                  typeList.Add(type.Value);

                  Exec.Try(() =>
                     {
                         var attr = type.Value.GetAttribute<EventResolverAttribute>(true);

                         if (attr != null)
                         {
                             var resolver = _eventResolverList.Values.FirstOrDefault(value => value.GetType().IsAssignableFrom(attr.EventResolverType));

                             if (resolver == null)
                                 resolver = attr.EventResolverType.CreateFast() as IEventResolver;

                             if (resolver != null)
                                 _eventResolverList[type.Key] = resolver;
                         }
                     }, e => WriteToLog(e, true));
              }));

            var types = AssemblyHelper.GetSubtypes(false, new[] { typeof(EventCategory), typeof(EventSource) }, typeof(PlatformAssemblyAttribute)).Where(t => t.IsDefined<SerializableAttribute>());

            FillTypes<EventCategory>(types);

            FillTypes<EventSource>(types);

            GetServices<IEventFilterProvider>().ForEach(s =>
           {
               _filterEventUserFuncList.Add(s.GetFilterUsersFunc());
               _filterEventFuncList.Add(s.GetFilterFunc());
           });
        }
        #endregion

        #region Uninitialize
        protected override void Uninitialize()
        {
            _eventToLogEventTask.Stop(1000);

            EventBase.ClearEventTypes();

            base.Uninitialize();
        }
        #endregion

        #region OnConfigurationChanged
        protected override void OnConfigurationChanged()
        {
            base.OnConfigurationChanged();

            Configuration = GetConfiguration<EventLogConfig>();
        }
        #endregion

        #region GetEventByUserId
        private Event GetEventByUserId(int eventId, int userId)
        {
            var evt = DB.RS.Single<Event>("[Event].[Get]", eventId);

            if (evt != null && _eventResolverList.GetValue(evt.TypeId).IfNotNull(resolver => resolver.IsAvailable(evt, userId)))
            {
                var evtList = new Event[] { evt };

                for (int i = 0; i < _filterEventFuncList.Count; i++)
                {
                    if (_filterEventFuncList[i] != null)
                        evtList = _filterEventFuncList[i](evtList, userId);
                }

                return evtList.Any() ? evtList[0] : null;
            }

            return null;
        }
        #endregion

        #region EventToLogEventProcessItem
        void EventToLogEventProcessItem(EventToLogEventItem queueItem, CancellationToken token)
        {
            var evt = (this as IEventLog).Get(queueItem.EventId);

            this.WriteToLogWithoutModuleHeader(string.Format("{0}\r\n{1}\r\n{2}\r\n{3:dd.MM.yyyy HH:mm:ss}\r\n-----\r\n{4}", queueItem.TypeName, queueItem.SourceName, queueItem.CategoryName, queueItem.Time, evt.Text), queueItem.Level);
        }
        #endregion
    }
}
