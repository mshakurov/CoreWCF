using System;
using System.Runtime.Serialization;

namespace ST.VideoIntegration.Server
{
  /// <summary>
  /// Контракт, описывающий невозможность получения параметров для просмотра видео.
  /// </summary>
  [Serializable]
  [DataContract( Namespace = Constants.MODULE_NAMESPACE )]
  public sealed class InvalidParamFault
  {
  }
}
