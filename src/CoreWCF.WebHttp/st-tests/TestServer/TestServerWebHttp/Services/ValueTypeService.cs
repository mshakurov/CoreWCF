// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;

using CoreWCF;

using ST.BusinessEntity.Server;

namespace Services
{
    //[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    internal class ValueTypeService : IValueType, IValueType2
    {
        private static int _id = 0;
        private readonly int Id;

        private static Random s_random = new Random();

        public ValueTypeService()
        {
            Id = Interlocked.Increment(ref _id);
        }

        public int GetId() => Id;
        public int GetId2() => Id;

        public ValueTypeData GetValueType(int valueTypeId) => new ValueTypeData { Id = valueTypeId, UnitId = (short)(s_random.Next((int)short.MaxValue) + 2), Name = $"Name {s_random.Next()}" };

        public ValueTypeData[] GetValueTypeList() => Enumerable.Range(1, 10).Select(i => GetValueType(s_random.Next(int.MaxValue - 2) + 2)).ToArray();

        public ValueTypeData GetValueType2(int valueTypeId) => this.GetValueType(valueTypeId);
    }
}
