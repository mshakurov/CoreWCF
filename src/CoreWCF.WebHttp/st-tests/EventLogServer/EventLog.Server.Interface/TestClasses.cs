// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ST.EventLog.Server
{
    /// <summary>
    /// Событие.
    /// </summary>
    [DataContract(Namespace = Constants.MODULE_NAMESPACE)]
    [Serializable]
    public class TestEvent : IEquatable<TestEvent?>
    {
        /// <summary>
        /// Идентификатор.
        /// </summary>
        [DataMember]
        public int Id { get; set; }

        /// <summary>
        /// Состояние события с учетом обработки пользователем.
        /// </summary>
        [DataMember]
        public EventState State { get; protected set; }

        public override bool Equals(object? obj) => Equals(obj as TestEvent);
        public bool Equals(TestEvent? other) => other != null && Id == other.Id && State == other.State;
        public override int GetHashCode() => HashCode.Combine(Id, State);

        public static bool operator ==(TestEvent? left, TestEvent? right) => EqualityComparer<TestEvent>.Default.Equals(left, right);
        public static bool operator !=(TestEvent? left, TestEvent? right) => !(left == right);
    }

    public static class TestClassesUtils
    {

        [Serializable]
        [DataContract(Name = "TestEvent", Namespace = Constants.MODULE_NAMESPACE)]
        private class TestEventImpl : TestEvent
        {
            #region .Properties

            /// <summary>
            /// Состояние события с учетом обработки пользователем.
            /// </summary>
            [DataMember]
            public new EventState State
            {
                get { return base.State; }
                set { base.State = value; }
            }
            #endregion
        }

        public static TestEvent CreateImplFrom(int id, EventState state)
        {
            return new TestEventImpl { Id = id, State = state };
        }

    }

}
