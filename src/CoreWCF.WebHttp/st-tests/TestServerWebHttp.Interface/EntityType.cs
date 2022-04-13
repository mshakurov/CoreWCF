using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

namespace ST.BusinessEntity.Server
{
    /// <summary>
    /// Тип бизнес-сущности.
    /// </summary>
    [Serializable]
    [DataContract(Namespace = Constants.MODULE_NAMESPACE)]
    [DebuggerDisplay("{Name} [{Code}]")]
    public class EntityType
    {
        private static readonly int[] _emptyChildren = new int[0];

        #region .Fields
        private int[] _children;
        #endregion

        #region .Properties
        /// <summary>
        /// Идентификатор.
        /// </summary>
        [DataMember]
        public int Id { get; set; }

        /// <summary>
        /// Идентификатор родительского типа бизнес-сущности.
        /// </summary>
        [DataMember]
        public virtual int? ParentId { get; set; }


        /// <summary>
        /// Список идентификаторов прямых наследников типа бизнес-сущности.
        /// </summary>
        [DataMember]
        public int[] Children
        {
            get { return _children == null ? _emptyChildren : _children.Clone() as int[]; }
            set { _children = value; }
        }

        /// <summary>
        /// Код.
        /// </summary>
        [DataMember]
        public virtual string Code { get; set; }

        /// <summary>
        /// Название.
        /// </summary>
        [DataMember]
        public virtual string Name { get; set; }
        #endregion
    }
}
