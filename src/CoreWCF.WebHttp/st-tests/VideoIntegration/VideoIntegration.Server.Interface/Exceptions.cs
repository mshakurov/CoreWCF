using System;

using CoreWCF;

namespace ST.VideoIntegration.Server
{
  /// <summary>
  /// Исключение выбрасывается при попытке просмотра видео без необходимых параметров.
  /// </summary>
  [Serializable]
  public class InvalidParamException : FaultException<InvalidParamFault>
  {
    #region .Ctor
    public InvalidParamException( string message )
      : base( new InvalidParamFault(), message )
    {
    }
    #endregion
  }

}
