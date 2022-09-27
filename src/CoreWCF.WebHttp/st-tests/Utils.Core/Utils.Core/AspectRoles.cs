using PostSharp.Aspects.Dependencies;

namespace ST.Utils
{
  /// <summary>
  /// Стандартные роли аспектов.
  /// </summary>
  public static class AspectRoles
  {
    #region .Constants
    /// <summary>
    /// См. PostSharp.Aspects.Dependencies.StandardRoles.Caching.
    /// </summary>
    public const string Caching = StandardRoles.Caching;

    /// <summary>
    /// См. PostSharp.Aspects.Dependencies.StandardRoles.DataBinding.
    /// </summary>
    public const string DataBinding = StandardRoles.DataBinding;

    /// <summary>
    /// См. PostSharp.Aspects.Dependencies.StandardRoles.ExceptionHandling.
    /// </summary>
    public const string ExceptionHandling = StandardRoles.ExceptionHandling;

    /// <summary>
    /// Изменение значения поля, свойства или параметра.
    /// </summary>
    public const string Modification = "Modification";

    /// <summary>
    /// См. PostSharp.Aspects.Dependencies.StandardRoles.PerformanceInstrumentation.
    /// </summary>
    public const string PerformanceInstrumentation = StandardRoles.PerformanceInstrumentation;

    /// <summary>
    /// См. PostSharp.Aspects.Dependencies.StandardRoles.Persistence.
    /// </summary>
    public const string Persistence = StandardRoles.Persistence;

    /// <summary>
    /// См. PostSharp.Aspects.Dependencies.StandardRoles.Security.
    /// </summary>
    public const string Security = StandardRoles.Security;

    /// <summary>
    /// См. PostSharp.Aspects.Dependencies.StandardRoles.Threading.
    /// </summary>
    public const string Threading = StandardRoles.Threading;

    /// <summary>
    /// См. PostSharp.Aspects.Dependencies.StandardRoles.Tracing.
    /// </summary>
    public const string Tracing = StandardRoles.Tracing;

    /// <summary>
    /// См. PostSharp.Aspects.Dependencies.StandardRoles.TransactionHandling.
    /// </summary>
    public const string TransactionHandling = StandardRoles.TransactionHandling;

    /// <summary>
    /// См. PostSharp.Aspects.Dependencies.StandardRoles.Validation.
    /// </summary>
    public const string Validation = StandardRoles.Validation;
    #endregion
  }
}
