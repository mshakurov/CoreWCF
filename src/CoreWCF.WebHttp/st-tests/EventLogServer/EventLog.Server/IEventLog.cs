using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;

using ST.Core;
using ST.Server;
using ST.Utils;
using ST.Utils.Attributes;
using ST.Utils.DataTypes;

namespace ST.EventLog.Server
{
    /// <summary>
    /// Журнал протоколирования событий. 
    /// </summary>
    public sealed partial class EventLog : DatabaseModule_PG, IEventLog
    {
        #region .Constants
        private const int EVENTS_COUNT_LIMIT = 200;
        #endregion

        #region .Fields
        private readonly ConcurrentDictionary<CultureInfo, Dictionary<int, EventTypeDescriptorImpl>> _eventTypes = new ConcurrentDictionary<CultureInfo, Dictionary<int, EventTypeDescriptorImpl>>();
        private readonly ConcurrentDictionary<CultureInfo, Dictionary<int, SourceDescriptorImpl>> _eventSources = new ConcurrentDictionary<CultureInfo, Dictionary<int, SourceDescriptorImpl>>();
        private readonly ConcurrentDictionary<CultureInfo, Dictionary<int, CategoryDescriptorImpl>> _eventCategories = new ConcurrentDictionary<CultureInfo, Dictionary<int, CategoryDescriptorImpl>>();
        #endregion

        #region .Properties
        private Dictionary<int, EventTypeDescriptorImpl> EventTypes
        {
            get { return _eventTypes.GetOrAdd(ServerContext.Session.Culture, ci => GetDictionary<EventBase, EventTypeDescriptorImpl>(ci, t => EventBase.GetCode(t))); }
        }

        private Dictionary<int, SourceDescriptorImpl> EventSources
        {
            get { return _eventSources.GetOrAdd(ServerContext.Session.Culture, ci => GetDictionary<EventSource, SourceDescriptorImpl>(ci, t => EventSource.GetCode(t))); }
        }

        private Dictionary<int, CategoryDescriptorImpl> EventCategories
        {
            get { return _eventCategories.GetOrAdd(ServerContext.Session.Culture, ci => GetDictionary<EventCategory, CategoryDescriptorImpl>(ci, t => EventCategory.GetCode(t))); }
        }
        #endregion

        #region FilterFormatter
        private void FilterFormatter(QueryFilterCriterionFormat format)
        {
            if (format.Name != null)
                format.Name = format.Name.Replace("[", "@").Replace("]", "");
        }
        #endregion

        #region GetDictionary<TBase, TDesc>
        private Dictionary<int, TDesc> GetDictionary<TBase, TDesc>(CultureInfo cultureInfo, Func<Type, int> getCode)
          where TBase : class
          where TDesc : EventBaseDescriptor, IEventBaseDescriptorImpl, new()
        {
            var currentUICulture = Thread.CurrentThread.CurrentUICulture;

            try
            {
                Thread.CurrentThread.CurrentUICulture = cultureInfo;

                return _types.GetValue(typeof(TBase)).Select(type => new TDesc { Type = type, Id = getCode(type), Name = type.GetDisplayName(), Category = type.GetCategory() }).ToDictionary(desc => desc.Id);
            }
            finally
            {
                Thread.CurrentThread.CurrentUICulture = currentUICulture;
            }
        }
        #endregion

        #region GetEventUserTypes
        private int[] GetEventUserTypes()
        {
            return EventTypes.Keys.ToArray();
            //return ISecurityManager.ExecForUser( userId => ServerContext.Session.Permissions.Contains<Permissions.EventViewAll>() ? EventTypes.Keys.ToArray() :
            //                                      DB.RS.ListScalar<int>( "[Access].[GetEventUserTypes]", userId ).ToArray(), () => EventTypes.Keys.ToArray() );
        }
        #endregion

        #region GetEventList
        private List<EventImpl> GetEventList([NotNullNotEmpty] string spName, bool details, params object[] parameters)
        {
            bool eventsFiltered;

            int lastEventId;

            DateTime? lastEventTime;

            bool hasMoreRecords;

            var eventList = GetUserEventList(spName, details, out eventsFiltered, out lastEventId, out lastEventTime, out hasMoreRecords, false, parameters);

            //if( eventList.Count > 0 && details )
            //  SetTacho( eventList );

            return eventList;
        }
        #endregion

        #region GetUserEventList
        private List<EventImpl> GetUserEventList([NotNullNotEmpty] string spName, bool details, out bool isEventsFilteredByUser, out int lastEventId, out DateTime? lastEventTime, out bool hasMoreRecords, bool getResolve, params object[] parameters)
        {
            var events = DB.RS.List<EventImpl>(spName, parameters);

            if (events.Count < EVENTS_COUNT_LIMIT)
                hasMoreRecords = false;
            else
                hasMoreRecords = true;

            var resultEvents = PrepareEvents(events, getResolve);

            isEventsFilteredByUser = events.Count > resultEvents.Count;

            var lastEvent = events.LastOrDefault();

            if (lastEvent != null)
            {
                lastEventId = lastEvent.Id;
                lastEventTime = lastEvent.Time;
            }
            else
            {
                lastEventId = -1;
                lastEventTime = null;
            }

            if (!details)
                for (int i = 0; i < resultEvents.Count; i++)
                    resultEvents[i].SetDetail(string.Empty);

            //for( int i = 0; i < _filterEventFuncList.Count; i++ )
            //{
            //  if( _filterEventFuncList[i] != null )
            //    resultEvents = _filterEventFuncList[i]( resultEvents.ToArray(), ISecurityManager.GetCurrentUserId() ).ConvertDown<Event, EventImpl>();
            //}

            return resultEvents;
        }
        #endregion

        //#region SetTacho
        //private void SetTacho( List<EventImpl> eventList )
        //{
        //  if( eventList.Count == 0 )
        //    return;

        //  var monObjs = new int[eventList.Count];
        //  var times = new DateTime[eventList.Count];

        //  for( int i = 0; i < eventList.Count; i++ )
        //  {
        //    monObjs[i] = eventList[i].MonObjId.GetValueOrDefault();
        //    times[i] = eventList[i].Time;
        //  }

        //  var driverIds = ITelematicsTacho.GetDriverIds( monObjs, times );

        //  if( driverIds == null || driverIds.Length != eventList.Count )
        //    throw new Exception( SR.GetString( RI.ErrorTachoReturn ) );

        //  for( int i = 0; i < eventList.Count; i++ )
        //  {
        //    if( driverIds[i] == 0 )
        //      continue;

        //    var driver = EntityCache.GetEntity<Driver>( driverIds[i], EntityQueryOption.Deleted );

        //    if( driver != null )
        //      eventList[i].DriverName = driver.GetFormattedName();
        //  }
        //}
        //#endregion

        //#region SetPhoneNumberIndicator
        //private void SetPhoneNumberIndicator( List<EventImpl> eventList )
        //{
        //  if( eventList.Count == 0 )
        //    return;

        //  var monObjIds = eventList.Where( e => e.MonObjId.HasValue ).Select( ev => ev.MonObjId.Value ).Distinct().ToArray();

        //  if( monObjIds != null && monObjIds.Any() )
        //  {
        //    var dicPhone = new Dictionary<int, string>();

        //    EntityCache.GetEntityList<Terminal>( Terminal.EntityCode, QueryFilter.Where( Terminal.MonObjIdCode ).In( monObjIds ) ).ForEach( t => dicPhone.AddOrUpdate( t.MonObjId, t.Phone ) );

        //    if( dicPhone.Any() )
        //      eventList.ForEach( ev =>
        //      {
        //        ev.HasPhoneNumber = !String.IsNullOrEmpty( dicPhone.GetValue( ev.MonObjId.GetValueOrDefault() ) );
        //      } );
        //  }
        //}
        //#endregion

        #region GetProcessRequired
        private int[] GetProcessRequired(int[] events, int userId)
        {
            var ids = DB.RS.ListScalar<int>("[Event].[GetProcessRequired]", userId, InitializeIdentifiers(events, ref _identifierEvents));

            ids.Sort();

            ids.Reverse();

            return ids.ToArray();
        }
        #endregion

        #region GetEventResolver
        private bool GetEventResolver(List<EventResolver> eventResolvers, EventImpl evt, int userId)
        {
            var resolver = _eventResolverList.GetValue(evt.TypeId);

            if (resolver != null)
            {
                EventResolver er = null;

                for (int i = 0; i < eventResolvers.Count; i++)
                {
                    if (RuntimeHelpers.GetHashCode(eventResolvers[i].Resolver) != RuntimeHelpers.GetHashCode(resolver))
                        continue;

                    er = eventResolvers[i];
                }

                if (er == null)
                    eventResolvers.Add(er = new EventResolver(resolver));

                if (er.Resolver.IsAvailable(evt, userId))
                    er.EventList.Add(evt);
                else
                    return false;
            }

            return true;
        }
        #endregion

        #region PrepareEvents
        private List<EventImpl> PrepareEvents(List<EventImpl> eventList, bool getResolve)
        {
            //var resultEvents = new List<EventImpl>();
            //var eventResolvers = new List<EventResolver>();

            //ISecurityManager.ExecForUser( userId => eventResolvers = ResolveEvents( eventList, userId, ref resultEvents ), () => eventResolvers = ResolveEvents( eventList, -1, ref resultEvents ) );

            //if( resultEvents.Count == 0 )
            //  return resultEvents;

            //SetStateEvents( resultEvents, getResolve );

            ////SetResolveUserEvents( resultEvents );

            //var currentUICulture = Thread.CurrentThread.CurrentUICulture;

            //Exec.Try( () =>
            //{
            //  Thread.CurrentThread.CurrentUICulture = ServerContext.Session.Culture;

            //  for( int i = 0; i < eventResolvers.Count; i++ )
            //    if( eventResolvers[i].Resolver != null )
            //      eventResolvers[i].Resolver.Resolve( eventResolvers[i].EventList.ToArray() );
            //}, () => Thread.CurrentThread.CurrentUICulture = currentUICulture );

            //return resultEvents;

            return eventList;
        }
        #endregion

        #region ProcessEvent
        private EventState ProcessEvent(int id, string description)
        {
            return EventState.Processed;

            //return ISecurityManager.ExecForUser( userId =>
            //{
            //  var res = DB.GetScalar( "[Event].[SetState]", id, userId, description );

            //  if( res == null || res == DBNull.Value )
            //    throw new UserPermissionEditException();

            //  var typeId = DB.GetScalar<int>( "[Event].[GetTypeId]", id );

            //  var state = (EventState) ((int) res);

            //  SendEvent( new EventStateMessage { EventId = id, TypeId = typeId, State = state } );

            //  return state;
            //} );
        }
        #endregion

        #region ResolveEvents
        private List<EventResolver> ResolveEvents(List<EventImpl> eventList, int userId, ref List<EventImpl> resultEvents)
        {
            var eventResolvers = new List<EventResolver>();

            for (int i = 0; i < eventList.Count; i++)
                if (GetEventResolver(eventResolvers, eventList[i], userId))
                    resultEvents.Add(eventList[i]);

            return eventResolvers;
        }
        #endregion

        //#region SetResolveUserEvents
        //private void SetResolveUserEvents( List<EventImpl> eventList )
        //{
        //  var resolveUsers = ISecurityUser.GetUserMultiple( eventList.Where( ev => ev.ResolveUserId.HasValue )
        //                                                             .Select( ev => ev.ResolveUserId.Value ).Distinct().ToArray() ).ToDictionary( u => u.Id );

        //  eventList.Where( ev => ev.ResolveUserId.HasValue )
        //           .ForEach( ev =>
        //           {
        //             var user = resolveUsers.GetValue( ev.ResolveUserId.Value );

        //             ev.ResolveUser = user == null ? "" : user.Login;
        //           } );
        //}
        //#endregion

        #region SetStateEvents
        private void SetStateEvents(List<EventImpl> eventList, bool getResolve)
        {
            var ids = new int[0];
            var outIds = new List<int>();
            var procReqIds = new List<int>();
            var resolveIds = new List<int>();
            var procInfoDic = new Dictionary<int, ProcessingInfo[]>();
            var resolveUserDic = new Dictionary<int, string>();

            for (int i = 0; i < eventList.Count; i++)
            {
                if (eventList[i].State == EventState.NotProcessed || eventList[i].State == EventState.PartiallyProcessed)
                    procReqIds.Add(eventList[i].Id);
                else
                  if (eventList[i].State == EventState.Processed || eventList[i].State == EventState.PartiallyProcessed)
                    resolveIds.Add(eventList[i].Id);
            }

            //ISecurityManager.ExecForUser( userId =>
            //{
            //  //var eventIdentifiers = eventList.FindAll( evt => evt.State == EventState.NotProcessed || evt.State == EventState.PartiallyProcessed ).Select( e => e.Id ).ToArray();

            //  if( procReqIds.Count > 0 )
            //    ids = GetProcessRequired( procReqIds.ToArray(), userId );
            //} );

            //if( getResolve && resolveIds.Count > 0 )
            //{
            //  procInfoDic = DB.RS.List<ProcessingInfo>( "[Event].[GetProcessingInfo]", InitializeIdentifiers( resolveIds.ToArray(), ref _identifierEvents ) ).GroupBy( p => p.EventId ).ToDictionary( k => k.Key, v => v.ToArray() );

            //  if( procInfoDic.Count > 0 )
            //    resolveUserDic = ISecurityUser.GetUserMultiple( procInfoDic.Values.SelectMany( v => v.Select( p => p.UserId ) ).Distinct().ToArray() ).ToDictionary( u => u.Id, v => v.Login );
            //}

            for (int i = 0, index = 0; i < eventList.Count; i++)
            {
                var evt = eventList[i];

                var pInfo = procInfoDic.GetValue(evt.Id);

                if (pInfo != null)
                {
                    evt.ResolveUserId = pInfo.Select(p => p.UserId).ToArray();
                    evt.ResolveTime = pInfo.Select(p => p.Time).ToArray();
                    evt.ResolveDescription = pInfo.Select(p => p.Description).ToArray();
                    evt.ResolveUser = pInfo.Select(p => resolveUserDic.GetValue(p.UserId)).ToArray();
                }

                evt.InternalState = evt.State;

                if (index >= ids.Length || evt.Id > ids[0] || evt.Id < ids[ids.Length - 1])
                    continue;
                else
                  if (evt.Id == ids[index])
                {
                    evt.SetState(EventState.ProcessRequired);

                    index++;

                    continue;
                }
                else
                    if (evt.Id < ids[index])
                {
                    for (; index < ids.Length && evt.Id < ids[index]; index++)
                        outIds.Add(ids[index]);

                    if (evt.Id == ids[index])
                        evt.SetState(EventState.ProcessRequired);
                }
                else
                      if (outIds.Contains(evt.Id))
                    evt.SetState(EventState.ProcessRequired);
            }
        }
        #endregion

        #region IEventLog
        Event IEventLog.Get(int id)
        {
            return GetEventList("[Event].[Get]", true, id).IfNotNull(events => events.Count != 0 ? events[0] : null);
        }

        CategoryDescriptor IEventLog.GetCategory(int id)
        {
            return EventCategories.GetValue(id);
        }

        CategoryDescriptor[] IEventLog.GetCategoryList()
        {
            return EventCategories.Values.ToArray();
        }

        Event[] IEventLog.GetList(TimeRange time, EventFilter filter, bool? details, int? userLimit, int? startEventID, DateTime? startEventTime, string sortOrder, bool useExpiration, bool getResolve)
        {
            var userTypes = GetEventUserTypes();

            if (userTypes == null || userTypes.Length == 0)
                return new List<EventImpl>().ToArray();

            int[] typeIds = null;

            if (filter.Types != null && filter.Types.Length > 0)
            {
                typeIds = filter.Types.Intersect(userTypes).ToArray();

                if (typeIds == null || typeIds.Length == 0)
                    return new List<EventImpl>().ToArray();
            }

            int[] monObjs = null;
            if (!string.IsNullOrWhiteSpace(filter.Plate) || !string.IsNullOrWhiteSpace(filter.GarageNumber) || (filter.MonGroupId.HasValue && filter.MonGroupId.Value > 0) || (filter.MonObjIds != null && filter.MonObjIds.Length > 0))
            {
                if (filter.MonObjIds != null && filter.MonObjIds.Length > 0)
                {
                    monObjs = filter.MonObjIds;
                }

                //if( monObjs == null && filter.MonGroupId.HasValue && filter.MonGroupId.Value > 0 )
                //{
                //  var vehicleGroup = EntityCache.GetEntity<VehicleGroup>( filter.MonGroupId.Value );

                //  if( vehicleGroup != null )
                //    monObjs = vehicleGroup.VehicleList.Select( v => v.Id ).ToArray();
                //}

                //if( monObjs == null )
                //{
                //  var plateFilter = string.IsNullOrWhiteSpace( filter.Plate ) ? null :
                //                    QueryFilter.Where( Vehicle.PlateCode ).Like( "%" + filter.Plate + "%" );

                //  var garageNumberFilter = string.IsNullOrWhiteSpace( filter.GarageNumber ) ? null :
                //                           QueryFilter.Where( Vehicle.GarageNumberCode ).Like( "%" + filter.GarageNumber + "%" );

                //  QueryFilter vehicleFilter = (plateFilter == null) ? garageNumberFilter :
                //                              (garageNumberFilter == null) ? plateFilter :
                //                              plateFilter.And( garageNumberFilter );

                //  monObjs = EntityCache.GetEntityList<Vehicle>( Vehicle.EntityCode, vehicleFilter ).Select( v => v.Id ).ToArray();
                //}

                if (monObjs.Length == 0)
                    return new List<EventImpl>().ToArray();
            }

            if (String.Compare(sortOrder, "ASC", StringComparison.OrdinalIgnoreCase) != 0)
                sortOrder = "DESC";

            var eventListParams = new object[]
            {
        time.From, time.To,
        InitializeIdentifiers( ( typeIds != null ? typeIds : userTypes ).Intersect( EventTypes.Keys ), ref _identifierTypes ),
        InitializeIdentifiers( filter.Levels != null && filter.Levels.Length > 0 ? filter.Levels : null, ref _identifierLevels ),
        InitializeIdentifiers( filter.Sources != null && filter.Sources.Length > 0 ? filter.Sources : null, ref _identifierSources ),
        InitializeIdentifiers( filter.Categories != null && filter.Categories.Length > 0 ? filter.Categories : null, ref _identifierCategories ),
        InitializeIdentifiers( filter.States != null && filter.States.Length > 0 ? filter.States : null, ref _identifierStates ),
        InitializeIdentifiers( monObjs != null && monObjs.Length > 0 ? monObjs : null, ref _identifierMonObjs ),
        filter.QueryFilter.IfNotNull( f => f.Unwrap().ToString( FilterFormatter ) ),
        null /*ISecurityManager.GetOrgGroupIdByCurrentUser()*/,
        userLimit > 0 ? EVENTS_COUNT_LIMIT : -1,
        startEventID,
        startEventTime,
        sortOrder,
        useExpiration,
        0 /*ISecurityManager.GetCurrentUserId()*/,
            };

            bool isEventsFilteredByUser;

            int lastEventId;

            DateTime? lastEventTime;

            bool hasMoreRecords;

            var eventList = GetUserEventList("[Event].[GetList]", details ?? false, out isEventsFilteredByUser, out lastEventId, out lastEventTime, out hasMoreRecords, getResolve, eventListParams);

            while (userLimit > 0 && (eventList.Count > 0 || isEventsFilteredByUser) && userLimit > eventList.Count && hasMoreRecords)
            {
                // параметр для хранимой процедуры: startEventID.
                eventListParams[11] = lastEventId;
                // параметр для хранимой процедуры: startEventTime.
                eventListParams[12] = lastEventTime;

                var extraEventList = GetUserEventList("[Event].[GetList]", details ?? false, out isEventsFilteredByUser, out lastEventId, out lastEventTime, out hasMoreRecords, getResolve, eventListParams);

                if (extraEventList.Count == 0)
                    if (isEventsFilteredByUser)
                        continue;
                    else
                        break;

                eventList.AddRange(extraEventList);
            }

            if (eventList.Count > 0 && details.HasValue && details.Value)
            {
                //SetTacho( eventList );

                //SetPhoneNumberIndicator( eventList );
            }

            if (filter.StatesProcessed != null && filter.StatesProcessed.Length > 0)
            {
                var processedStates = new HashSet<int>(filter.StatesProcessed);

                eventList.RemoveAll(ev => !processedStates.Contains((int)ev.State));
            }

            return eventList.ToArray();
        }

        Event[] IEventLog.GetResolvedList(int[] typeIds, TimeRange time)
        {
            bool isEventsFilteredByUser;

            int lastEventId;

            DateTime? lastEventTime;

            bool hasMoreRecords;

            var typeIdsTable = InitializeIdentifiers(typeIds, ref _identifierTypesFilter);

            return GetUserEventList("[Event].[GetResolvedList]", false, out isEventsFilteredByUser, out lastEventId, out lastEventTime, out hasMoreRecords, false, new object[] { typeIdsTable, EventState.Processed, time.From, time.To }).ToArray();
        }

        int IEventLog.GetListCount(TimeRange time, EventFilter filter)
        {
            return (this as IEventLog).GetList(time, filter, false, 101, null, null, null).Length;
        }

        ProcessingInfo[] IEventLog.GetProcessingInfo(int id)
        {
            return DB.RS.List<ProcessingInfo>("[Event].[GetProcessingInfo]", InitializeIdentifiers(new int[] { id }, ref _identifierEvents)).ToArray();
        }

        SourceDescriptor IEventLog.GetSource(int id)
        {
            return EventSources.GetValue(id);
        }

        SourceDescriptor[] IEventLog.GetSourceList()
        {
            return EventSources.Values.ToArray();
        }

        EventTypeDescriptor IEventLog.GetType(int id)
        {
            return EventTypes.GetValue(id);
        }

        EventTypeDescriptor[] IEventLog.GetTypeList(int rootTypeId)
        {
            Console.WriteLine($">>> IEventLog.GetTypeList({rootTypeId})");

            return Enumerable.Range(1, 20).Select(i => new EventTypeDescriptorImpl { Id = 100 + i, Category = $"Тру ла ла {i}", Name = $"Абра кадабра {i}", Type = typeof(int) }).ToArray();

            var descs = GetEventUserTypes().IfNotNull(userTypes => EventTypes.Where(type => type.Key.In(userTypes)).Select(kvp => kvp.Value).ToArray()) ?? new EventTypeDescriptorImpl[0];

            if (rootTypeId != 0)
                descs = Array.FindAll(descs, desc =>
               {
                   var type = desc.Type;

                   while (type != null && type != typeof(EventBase))
                   {
                       if (EventBase.GetCode(type) == rootTypeId)
                           return true;

                       type = type.BaseType;
                   }

                   return false;
               });

            Console.WriteLine($"<<< IEventLog.GetTypeList({rootTypeId}) = Descs ({descs?.Length}): {string.Join(", ", descs.Select(d => $"{d.Name}|{d.Category}"))}");

            return descs;
        }

        EventData IEventLog.GetWithUserAccessInfo(int id)
        {
            return null;
            //return ISecurityManager.ExecForUser( userId => GetEventList( "[Event].[Get]", true, id ).IfNotNull( events => events.Count != 0 ? new EventData { Event = events[0], Info = DB.RS.Single<EventTypeUser>( "[Access].[Get]", events[0].TypeId, userId ) } : null ) );
        }

        bool IEventLog.IsProcessRequired(int eventId)
        {
            return false;
            //return ISecurityManager.ExecForUser( userId => GetProcessRequired( new int[] { eventId }, userId ).Length > 0 );
        }

        EventState IEventLog.ProcessEvent(int id, string description)
        {
            return ProcessEvent(id, description);
        }

        EventResultState[] IEventLog.ProcessEvents(int[] ids, string description)
        {
            var result = new List<EventResultState>();

            ids.ForEach(id => result.Add(new EventResultState { EventId = id, EventState = ProcessEvent(id, description) }));

            return result.ToArray();
        }

        TestEvent IEventLog.TestGetEvent(int id)
        {
            return TestClassesUtils.CreateImplFrom(id, EventState.PartiallyProcessed);
        }

        TestEvent IEventLog.TestChangeEvent(TestEvent evt)
        {
            if (evt == null)
                return null;

            return TestClassesUtils.CreateImplFrom(evt.Id + 55555, Enum.GetValues(evt.State.GetType()).OfType<EventState?>().FirstOrDefault(st => (int)evt.State < (int)st) ?? EventState.ProcessNotRequired);
        }

        Event IEventLog.TestGetRealEvent(int id)
        {
            return GetEventList("[Event].[Get]", true, id).IfNotNull(events => events.Count != 0 ? events[0] : null).ConvertUp<EventImpl, Event>();
        }
        #endregion

        [Serializable]
        [DataContract(Name = "Event", Namespace = Constants.MODULE_NAMESPACE)]
        private class EventImpl : Event
        {
            internal void SetDetail(string detail)
            {
                this.Detail = detail;
            }

            internal void SetState(EventState state)
            {
                this.State = state;
            }
        }

        private interface IEventBaseDescriptorImpl
        {
            Type Type { get; set; }
        }

        [Serializable]
        [DataContract(Name = "EventTypeDescriptor", Namespace = Constants.MODULE_NAMESPACE)]
        private sealed class EventTypeDescriptorImpl : EventTypeDescriptor, IEventBaseDescriptorImpl
        {
            #region Properties
            [IgnoreDataMember]
            public Type Type { get; set; }
            #endregion
        }

        [Serializable]
        [DataContract(Name = "SourceDescriptor", Namespace = Constants.MODULE_NAMESPACE)]
        private sealed class SourceDescriptorImpl : SourceDescriptor, IEventBaseDescriptorImpl
        {
            #region Properties
            [IgnoreDataMember]
            public Type Type { get; set; }
            #endregion
        }

        [Serializable]
        [DataContract(Name = "CategoryDescriptor", Namespace = Constants.MODULE_NAMESPACE)]
        private sealed class CategoryDescriptorImpl : CategoryDescriptor, IEventBaseDescriptorImpl
        {
            #region Properties
            [IgnoreDataMember]
            public Type Type { get; set; }
            #endregion
        }

        private class EventResolver
        {
            #region .Properties
            public IEventResolver Resolver { get; private set; }

            public List<Event> EventList { get; private set; }
            #endregion

            #region EventResolver
            public EventResolver(IEventResolver resolver)
            {
                Resolver = resolver;
                EventList = new List<Event>();
            }
            #endregion
        }
    }
}
