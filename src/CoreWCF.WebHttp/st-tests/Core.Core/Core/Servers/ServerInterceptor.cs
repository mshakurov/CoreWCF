using System.Diagnostics;
using System.Linq;
using Castle.DynamicProxy;
using ST.Utils.Reflection;

namespace ST.Core
{
  public partial class BaseServer
  {
    private sealed class ServerInterceptor : SimpleInterceptor
    {
      #region .Fields
      private readonly BaseModule _caller;
      #endregion

      #region .Ctor
      public ServerInterceptor( BaseModule module )
      {
        _caller = module;
      }
      #endregion

      #region OnBeforeInvoke
      [DebuggerStepThrough]
      protected override void OnBeforeInvoke( IInvocation invocation )
      {
        base.OnBeforeInvoke( invocation );

        if( !BaseServer.ServerInstance.Modules.Contains( _caller, ModuleComparer.Instance ) )
          throw new UnloadedModuleException( _caller );

        BaseServer._callerModule = _caller;
      }
      #endregion

      #region OnFinally
      [DebuggerStepThrough]
      protected override void OnFinally( IInvocation invocation )
      {
        base.OnFinally( invocation );

        BaseServer._callerModule = null;
      }
      #endregion
    }
  }
}
