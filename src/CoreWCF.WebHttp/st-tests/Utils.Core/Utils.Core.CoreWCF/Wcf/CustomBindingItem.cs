using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ST.Utils.Attributes;

namespace ST.Utils.Wcf
{
  /// <summary>
  /// Элемент: описывающий wcf биндинг
  /// </summary>
  [Serializable]
  [NotifyPropertyChanged]
  public class CustomBindingItem
  {
    #region .Properties
    [DisplayNameLocalized( "1. Название" )]
    public string Name { get; set; }
    [DisplayNameLocalized( "2. Транспорт" )]
    public ST.Utils.Wcf.BindingHelper.TransferType TransferType { get; set; }
    [DisplayNameLocalized( "3. Протокол" )]
    public ST.Utils.Wcf.BindingHelper.ProtocolType ProtocoType { get; set; }
    [DisplayNameLocalized( "4. Авторизация" )]
    public ST.Utils.Wcf.BindingHelper.AuthenticationType AuthenticationType { get; set; }
    [DisplayNameLocalized( "5. Контекст пользователя" )]
    public ST.Utils.Wcf.BindingHelper.ContextUserType ContextUserType { get; set; }
    [DisplayNameLocalized( "6. SSL/TLS" )]
    public bool UseTransportLevelSecurity { get; set; }
    [DisplayNameLocalized( "7. Сжатие" )]
    public ST.Utils.Wcf.BindingHelper.ZippedType ZippedType { get; set; }
    [DisplayNameLocalized( "8. Порт" )]
    public int Port { get; set; }
    #endregion

    #region .Ctor
    public CustomBindingItem()
    {
      Name = "CustomBinding";
    }
    #endregion

    #region Equals
    public override bool Equals( object obj )
    {
      var sd = obj as CustomBindingItem;

      return sd != null && sd.Name == Name;
    }
    #endregion

    #region GetHashCode
    public override int GetHashCode()
    {
      return Name.GetHashCode();
    }
    #endregion

    #region ToString
    public override string ToString()
    {
      return string.Format( "{0}: транспорт = {1}, протокол = {2}, авторизация = {3}, контекст пользователя = {4}, сжатие = {5}, SSL/TLS = {6}, порт = {7}", Name, TransferType, ProtocoType, AuthenticationType, ContextUserType, ZippedType, UseTransportLevelSecurity, Port );
    }
    #endregion
  }
}

