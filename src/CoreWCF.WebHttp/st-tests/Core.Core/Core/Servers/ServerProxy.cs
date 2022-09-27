using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting;
using ST.Utils.Reflection;

namespace ST.Core
{
  public partial class BaseServer
  {
    //private sealed class ServerProxy : SimpleProxy, IRemotingTypeInfo
    //{
    //  #region .Fields
    //  private readonly BaseModule _caller;
    //  #endregion

    //  #region .Properties
    //  protected override object RemoteObject
    //  {
    //    get { return BaseServer.ServerInstance; }
    //  }
    //  #endregion

    //  #region .Ctor
    //  public ServerProxy( BaseModule module ) : base( typeof( IBaseServer ) )
    //  {
    //    _caller = module;
    //  }
    //  #endregion

    //  #region OnBeforeInvoke
    //  [DebuggerStepThrough]
    //  protected override void OnBeforeInvoke( IMethodCallMessage methodCall, object[] args )
    //  {
    //    base.OnBeforeInvoke( methodCall, args );

    //    if( !BaseServer.ServerInstance.Modules.Contains( _caller, ModuleComparer.Instance ) )
    //      throw new UnloadedModuleException( _caller );

    //    BaseServer._callerModule = _caller;
    //  }
    //  #endregion

    //  #region OnFinally
    //  [DebuggerStepThrough]
    //  protected override void OnFinally( IMethodCallMessage methodCall )
    //  {
    //    BaseServer._callerModule = null;
    //  }
    //  #endregion

    //  #region IRemotingTypeInfo
    //  bool IRemotingTypeInfo.CanCastTo( Type fromType, object o )
    //  {
    //    return BaseServer.ServerInstance._supportedInterfaces.Contains( fromType );
    //  }

    //  string IRemotingTypeInfo.TypeName
    //  {
    //    get { return typeof( IBaseServer ).FullName; }
    //    set { }
    //  }
    //  #endregion
    //}
  }
}
