using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using PostSharp;
using ST.Utils;
using ST.Utils.Attributes;

namespace ST.BusinessEntity.Server
{
  /// <summary>
  /// Экземпляр бизнес-сущности.
  /// </summary>
  [Serializable]
  public class Entity
  {
    #region .Constants
    private const string ATTRIBUTE_GROUP = "attribute";
    #endregion

    #region .Properties
    /// <summary>
    /// Идентификатор.
    /// </summary>
    public int Id { get; protected internal set; }

    /// <summary>
    /// Идентификатор типа бизнес-сущности.
    /// </summary>
    public int TypeId { get; protected internal set; }

    /// <summary>
    /// Время создания экземпляра.
    /// </summary>
    public DateTime Created { get; private set; }

    /// <summary>
    /// Время "удаления" экземпляра.
    /// </summary>
    public DateTime? Deleted { get; private set; }

    /// <summary>
    /// Глобальный идентификатор.
    /// </summary>
    public Guid? Guid { get; set; }
    #endregion
  }

}
