using ST.Utils;
using ST.Utils.Attributes;

namespace ST.Core
{
  internal abstract class ServerActionException : Exception
  {
    #region .Properties
    public override string StackTrace
    {
      get { return DoNotUseInnerStackTrace ? base.StackTrace : InnerException.StackTrace; }
    }

    protected bool DoNotUseInnerStackTrace { get; set; }

    internal ushort LogEventId { get; private set; }
    #endregion

    #region .Ctor
    internal ServerActionException( [NotNullNotEmpty] string message, [NotNull] Exception exc, ushort eventId ) : base( SR.GetString( message ), exc )
    {
      LogEventId = eventId;
    }
    #endregion
  }

  internal sealed class ServerStartingException : ServerActionException
  {
    #region .Properties
    public override string StackTrace
    {
      get { return InnerException is FileNotFoundException ? null : base.StackTrace; }
    }
    #endregion

    #region .Ctor
    internal ServerStartingException( Exception exc ) : base( SR.GetString( RI.ServerStartingError, BaseServer.PID ),
                                                              exc is FileNotFoundException ? new FileNotFoundException( SR.GetString( RI.FileNotFoundError, (exc as FileNotFoundException).FileName ) ) : exc,
                                                              BaseServer.LogEventId.ServerStartingError )
    {
    }
    #endregion
  }

  internal sealed class ServerStoppingException : ServerActionException
  {
    #region .Ctor
    internal ServerStoppingException( Exception exc ) : base( SR.GetString( RI.ServerStoppingError, BaseServer.PID ), exc, BaseServer.LogEventId.ServerStoppingError )
    {
    }
    #endregion
  }

  internal sealed class ModuleOperationTimeOutException : Exception
  {
    #region .Ctor
    internal ModuleOperationTimeOutException( [NotNullNotEmpty] string message = RI.ModuleOperationTimeOutError ) : base( SR.GetString( message ) )
    {
    }
    #endregion
  }

  internal sealed class ModuleLoadException : ServerActionException
  {
    #region .Properties
    public override string StackTrace
    {
      get { return InnerException is ModuleOperationTimeOutException ? null : base.StackTrace; }
    }
    #endregion

    #region .Ctor
    internal ModuleLoadException( Exception exc, Type type ) : base( SR.GetString( RI.ModuleLoadError, type.GetDisplayName(), BaseServer.PID ), exc, BaseServer.LogEventId.ModuleLoadError )
    {
    }
    #endregion
  }

  internal sealed class ModuleUnloadException : ServerActionException
  {
    #region .Properties
    public override string StackTrace
    {
      get { return InnerException is ModuleOperationTimeOutException ? null : InnerException.StackTrace; }
    }
    #endregion

    #region .Ctor
    internal ModuleUnloadException( Exception exc, BaseModule module ) : base( SR.GetString( RI.ModuleUnloadError, module.GetDisplayName(), BaseServer.PID ), exc, BaseServer.LogEventId.ModuleUnloadError )
    {
    }
    #endregion
  }

  internal sealed class ModuleMessageHandlingException : ServerActionException
  {
    #region .Ctor
    internal ModuleMessageHandlingException( Exception exc, BaseModule module, Type msgType ) : base( SR.GetString( RI.ModuleMessageHandlingError, module.GetDisplayName(), msgType.FullName, BaseServer.PID ), exc, PublisherServer.LogEventId.ModuleMessageHandlingError )
    {
    }
    #endregion
  }

  internal sealed class DeletingSessionException : ServerActionException
  {
    #region .Ctor
    internal DeletingSessionException( Exception exc ) : base( RI.DeletingSessionError, exc, WcfServer.LogEventId.DeletingSessionError )
    {
    }
    #endregion
  }

  internal sealed class WcfServiceException : ServerActionException
  {
    #region .Properties
    public override string StackTrace
    {
      get { return null; }
    }
    #endregion

    #region .Ctor
    internal WcfServiceException( Exception exc, string hostBaseAddress0, BaseModule module ) : base( module == null ? SR.GetString( RI.WcfServiceServerError, hostBaseAddress0) :
                                                                                                                SR.GetString( RI.WcfServiceError, module.GetDisplayName(), hostBaseAddress0),
                                                                                               exc, WcfServer.LogEventId.WcfServiceError )
    {
    }
    #endregion
  }

  internal sealed class InvalidWcfServiceAddressException : Exception
  {
    #region .Ctor
    internal InvalidWcfServiceAddressException( Type service ) : base( SR.GetString( RI.InvalidWcfServiceAddressError, service.FullName ) )
    {
    }
    #endregion
  }

  // Нет возможности совместить CoreWCF и классический WCF, поэтому класс будет дотсупен только в Net Framework
#if NET6_0_OR_GREATER
#else
  internal sealed class ClientServerCommunicationException : ServerActionException
  {
  #region .Ctor
    internal ClientServerCommunicationException( Exception exc ) : base(RI.CommunicationError, exc, WcfClientServer.LogEventId.CommunicationError)
    {
    }
  #endregion
  }
#endif

  internal sealed class UnknownAuthException : ServerActionException
  {
    #region .Ctor
    internal UnknownAuthException( Exception exc, string login, IAuthModule module ) : base( SR.GetString( RI.UnknownAuthError, login, module.GetDisplayName(), BaseServer.PID ), exc, WcfServer.LogEventId.UnknownAuthError )
    {
    }
    #endregion
  }

}