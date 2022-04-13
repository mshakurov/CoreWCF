// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ST.BusinessEntity.Server
{
    [Serializable]
    [DataContract(Namespace = "http://www.space-team.com/BusinessEntity")]
    public class ValueTypeData
    {
        /// <summary>
        /// Идентификатор.
        /// </summary>
        [DataMember]
        public int Id { get; set; }


        /// <summary>
        /// Идентификатор единиц измерения.
        /// </summary>
        [DataMember]
        public virtual short UnitId { get; set; }


        /// <summary>
        /// Название.
        /// </summary>
        [DataMember]
        public virtual string Name { get; set; }
    }
}
