using System;
using System.Runtime.Serialization;
using ST.Utils.Attributes;

namespace ST.Core
{
  /// <summary>
  /// Описатель разрешения доступа.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Interface.Constants.SERVER_NAMESPACE )]
  public class PermissionDescriptor
  {
    #region .Properties
    /// <summary>
    /// Код.
    /// </summary>
    [DataMember]
    public string Code { get; private set; }

    /// <summary>
    /// Название.
    /// </summary>
    [DataMember]
    public string Name { get; private set; }

    /// <summary>
    /// Описание.
    /// </summary>
    [DataMember]
    public string Description { get; private set; }
    #endregion

    #region .Ctor
    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="type">Тип разрешения доступа.</param>
    public PermissionDescriptor( [InheritedFrom( typeof( Permission ) )] Type type ) : this( Permission.GetCode( type ), Permission.GetName( type ), Permission.GetDescription( type ) )
    {
    }

    /// <summary>
    /// Конструктор.
    /// </summary>
    /// <param name="code">Код.</param>
    /// <param name="name">Название.</param>
    /// <param name="description">Описание.</param>
    public PermissionDescriptor( [NotNullNotEmpty] string code, [NotNullNotEmpty] string name, string description = null )
    {
      Code = code;
      Name = name;
      Description = description;
    }
    #endregion

    #region ToString
    /// <summary>
    /// Возвращает строковое представление описателя разрешения доступа.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
      return string.Format( "{0}: {1}", Code, Name );
    }
    #endregion
  }
}
