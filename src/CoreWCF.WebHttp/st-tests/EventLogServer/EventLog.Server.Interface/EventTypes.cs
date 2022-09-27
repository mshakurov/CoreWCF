using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

using ST.Core;
using ST.Utils;
using ST.Utils.Attributes;

namespace ST.EventLog.Server
{
    /// <summary>
    /// Базовый класс события.
    /// </summary>
    [Serializable]
    [DataContract(Namespace = Constants.MODULE_NAMESPACE)]
    [KnownType("GetSubtypes")]
    public abstract class EventBase
    {
        #region .Fields
        private static Dictionary<int, Type>? _eventTypes;
        #endregion

        #region ClearEventTypes
        /// <summary>
        /// Предназначен только для внутреннего использования.
        /// </summary>
        /// <returns></returns>
        [CallsAllowedFrom("ST.EventLog.Server")]
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void ClearEventTypes()
        {
            _eventTypes = null;
        }
        #endregion

        #region GetCode
        /// <summary>
        /// Возвращает код события.
        /// </summary>
        /// <returns>Код события.</returns>
        public int GetCode()
        {
            return GetCode(this.GetType());
        }

        /// <summary>
        /// Возвращает код события.
        /// </summary>
        /// <typeparam name="T">Тип события.</typeparam>
        /// <returns>Код события.</returns>
        public static int GetCode<T>()
          where T : EventBase
        {
            return GetCode(typeof(T));
        }

        /// <summary>
        /// Возвращает код события.
        /// </summary>
        /// <param name="type">Тип события.</param>
        /// <returns>Код события.</returns>
        public static int GetCode(Type type)
        {
            return type.GetUniqueHash();
        }
        #endregion

        #region GetDetail
        /// <summary>
        /// Возвращает объект с деталями события.
        /// </summary>
        /// <typeparam name="T">Тип события.</typeparam
        /// <param name="detail">Детали события</param>
        /// <returns>Событие.</returns>
        public static T GetDetail<T>(string detail)
          where T : EventBase, new()
        {
            return Serializer.DeserializeXml<T>(detail, Constants.ROOT_ELEMENT_NAME);
        }

        /// <summary>
        /// Возвращает объект с деталями события.
        /// </summary>
        /// <param name="type">Тип события</param>
        /// <param name="detail">Детали события</param>
        /// <returns>Событие.</returns>
        public static EventBase GetDetail(Type type, string detail)
        {
            if (type == null)
                return null;

            return Serializer.DeserializeXml(type, detail, Constants.ROOT_ELEMENT_NAME) as EventBase;
        }
        #endregion

        #region GetEventTypes
        /// <summary>
        /// Предназначен только для внутреннего использования.
        /// </summary>
        /// <returns></returns>
        [CallsAllowedFrom("ST.EventLog.Server")]
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static Dictionary<int, Type> GetEventTypes()
        {
            return _eventTypes ?? (_eventTypes = GetSubtypes().Where(t => t.IsDefined<SerializableAttribute>()).ToDictionary(GetCode));
        }
        #endregion

        #region GetSubtypes
        /// <summary>
        /// Возвращает список всех найденных в сборках домена типов событий.
        /// </summary>
        /// <returns>Список типов событий.</returns>
        public static IEnumerable<Type> GetSubtypes()
        {
            return AssemblyHelper.GetSubtypes(false, new[] { typeof(EventBase) }, typeof(PlatformAssemblyAttribute));
        }
        #endregion

        #region GetAvailableUserIdsId
        /// <summary>
        /// Возвращает идентификаторы пользователей, которым доступно данное событие.
        /// </summary>
        /// <returns>Идентификаторы пользователей.</returns>
        public virtual int[] GetAvailableUserIds()
        {
            return null;
        }
        #endregion

        #region GetOrgGroupId
        /// <summary>
        /// Возвращает идентификатор группы организации.
        /// </summary>
        /// <returns>Идентификатор группы организации.</returns>
        public virtual int? GetOrgGroupId()
        {
            return null;
        }
        #endregion

        #region IsAvailableForUser
        /// <summary>
        /// Возвращает признак доступности события.
        /// </summary>
        /// <param name="userId">Идентификатор события.</param>
        /// <returns>Признак доступности события.</returns>
        public virtual bool IsAvailableForUser(int userId)
        {
            return true;
        }
        #endregion
    }

    /// <summary>
    /// Атрибут, указывающий тип объекта, который позволяет получать дополнительную информацию по событиям.
    /// </summary>
    [Serializable]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class EventResolverAttribute : Attribute
    {
        #region .Properties
        /// <summary>
        /// Тип объекта, который позволяет получать дополнительную информацию по событиям.
        /// </summary>
        public Type EventResolverType { get; private set; }
        #endregion

        #region .Ctor
        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="type">Тип объекта, который позволяет получать дополнительную информацию по событиям. 
        /// Должен реализовывать интерфейс IEventResolver и иметь открытый конструктор без параметров.</param>
        public EventResolverAttribute([InheritedFrom(typeof(IEventResolver))] Type type)
        {
            if (type.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, new ParameterModifier[0]) == null)
                throw new ArgumentException("Type should have a public instance parameterless constructor.");

            EventResolverType = type;
        }
        #endregion
    }
}
