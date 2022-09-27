namespace ST.Core
{
  public partial class BaseServer
  {
    //private sealed class ServiceProxy : SimpleProxy
    //{
    //  #region .Fields
    //  private readonly BaseModule _module;
    //  public readonly BaseModule _target;
    //  #endregion

    //  #region .Properties
    //  protected override object RemoteObject
    //  {
    //    get { return _target; }
    //  }
    //  #endregion

    //  #region .Ctor
    //  public ServiceProxy( BaseModule module, BaseModule target, Type service ) : base( service )
    //  {
    //    _module = module;
    //    _target = target;
    //  }
    //  #endregion

    //  #region OnBeforeInvoke
    //  [DebuggerStepThrough]
    //  protected override void OnBeforeInvoke( IMethodCallMessage methodCall, object[] args )
    //  {
    //    base.OnBeforeInvoke( methodCall, args );

    //    if( !BaseServer.ServerInstance.Modules.Contains( _module, ModuleComparer.Instance ) )
    //      throw new UnloadedModuleException( _module );

    //    if( !BaseServer.ServerInstance.Modules.Contains( _target, ModuleComparer.Instance ) )
    //      throw new UnloadedTargetModuleException( _module, _target );
    //  }
    //  #endregion
    //}
  }
}
