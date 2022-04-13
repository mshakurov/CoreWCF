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
            Children = s_random.Next(3) == 1 ? Enumerable.Range(s_random.Next(int.MaxValue / 2 - 1), s_random.Next(10) + 1).ToArray() : null,
            Code = $"Code {s_random.Next()}",
            Name = $"Name {s_random.Next()}",
            ParentId = s_random.Next(3) == 1 ? s_random.Next() : null,
        };

        public EntityType GetEntityTypeByCode(string code, EntityQueryOption options = EntityQueryOption.Default, EntityTypeResult result = EntityTypeResult.Default) => new EntityType
        {
            Id = s_random.Next(),
            Children = s_random.Next(3) == 1 ? Enumerable.Range(s_random.Next(int.MaxValue / 2 - 1), s_random.Next(10) + 1).ToArray() : null,
            Code = code,
            Name = $"Name {s_random.Next()}",
            ParentId = s_random.Next(3) == 1 ? s_random.Next() : null,
        };

        public EntityType[] GetEntityTypeList(EntityQueryOption options = EntityQueryOption.Default) => Enumerable.Range(0, s_random.Next(6)).Select(i => GetEntityType(i, options)).ToArray();

        public void RemoveEntityType(int entityTypeId) { }

        public EntityType SetEntityType(EntityType entityType)
        {
            entityType.Id = s_random.Next();
            return entityType;
        }
    }
}
