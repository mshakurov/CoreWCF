// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;

using ST.BusinessEntity.Server;

namespace Services
{
    internal class ValueTypeService : IValueType
    {
        private static Random s_random = new Random();

        public ValueTypeData GetValueType(int valueTypeId) => new ValueTypeData { Id = valueTypeId, UnitId = (short)(s_random.Next((int)short.MaxValue) + 2), Name = $"Name {s_random.Next()}" };

        public ValueTypeData[] GetValueTypeList() => Enumerable.Range(1, 10).Select(i => GetValueType(s_random.Next(int.MaxValue - 2) + 2)).ToArray();
    }
}
