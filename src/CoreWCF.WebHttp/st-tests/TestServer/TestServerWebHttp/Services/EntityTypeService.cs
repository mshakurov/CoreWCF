// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;

using ST.BusinessEntity.Server;

namespace Services
{
    internal class EntityTypeService : IEntityType
    {
        private static Random s_random = new Random();

        public EntityType GetEntityType(int entityTypeId, EntityQueryOption options = EntityQueryOption.Default, EntityTypeResult result = EntityTypeResult.Default) => new EntityType
        {
            Id = entityTypeId,
            Children = s_random.Next(3) == 1 ? null : Enumerable.Range(s_random.Next(int.MaxValue / 2 - 1), s_random.Next(8) + 2).OrderBy(i => s_random.Next()).ToArray(),
            Code = $"Code {s_random.Next()}",
            Name = $"Name {s_random.Next()}",
            ParentId = s_random.Next(3) == 1 ? null : s_random.Next(),
        };

        public EntityType GetEntityTypeByCode(string code, EntityQueryOption options = EntityQueryOption.Default, EntityTypeResult result = EntityTypeResult.Default) => new EntityType
        {
            Id = s_random.Next(int.MaxValue - 2) + 2,
            Children = s_random.Next(3) == 1 ? null : Enumerable.Range(s_random.Next(int.MaxValue / 2 - 1), s_random.Next(7) + 3).ToArray(),
            Code = code,
            Name = $"Name {s_random.Next()}",
            ParentId = s_random.Next(3) == 1 ? null: s_random.Next(),
        };

        public EntityType[] GetEntityTypeList(EntityQueryOption options = EntityQueryOption.Default) => Enumerable.Range(s_random.Next(int.MaxValue / 2 - 1), s_random.Next(5) + 2).Select(i => GetEntityType(i, options)).ToArray();

        public void RemoveEntityType(int entityTypeId) { }

        public EntityType SetEntityType(EntityType entityType)
        {
            entityType.Id = s_random.Next(int.MaxValue - 2) + 2;
            return entityType;
        }
    }
}
