using System.Diagnostics;
using System.Linq;
using Castle.DynamicProxy;
using ST.Utils.Reflection;

namespace ST.Core
{
  public partial class BaseServer
  {
    private sealed class ServiceInterceptor : SimpleInterceptor
    {
      #region .Fields
      private readonly BaseModule _caller;
      #endregion

      #region .Ctor
      public ServiceInterceptor( BaseModule module )
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

        if( !BaseServer.ServerInstance.Modules.Contains( invocation.InvocationTarget as BaseModule, ModuleComparer.Instance ) )
          throw new UnloadedTargetModuleException( _caller, invocation.InvocationTarget as BaseModule );
      }
      #endregion
    }
  }
}
