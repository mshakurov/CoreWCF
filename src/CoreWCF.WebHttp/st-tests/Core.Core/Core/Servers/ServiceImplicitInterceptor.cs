using System.Diagnostics;
using System.Linq;
using Castle.DynamicProxy;
using ST.Utils.Reflection;

namespace ST.Core
{
  public partial class BaseServer
  {
    private sealed class ServiceImplicitInterceptor : SimpleInterceptor
    {
      #region .Fields
      private readonly BaseModule _caller;
      private readonly BaseModule _target;
      #endregion

      #region .Ctor
      public ServiceImplicitInterceptor( BaseModule module, BaseModule target )
      {
        _caller = module;
        _target = target;
      }
      #endregion

      #region OnBeforeInvoke
      [DebuggerStepThrough]
      protected override void OnBeforeInvoke( IInvocation invocation )
      {
        base.OnBeforeInvoke( invocation );

        if( !BaseServer.ServerInstance.Modules.Contains( _caller, ModuleComparer.Instance ) )
          throw new UnloadedModuleException( _caller );

        if( !BaseServer.ServerInstance.Modules.Contains( _target, ModuleComparer.Instance ) )
          throw new UnloadedTargetModuleException( _caller, _target );
      }
      #endregion
    }
  }
}
