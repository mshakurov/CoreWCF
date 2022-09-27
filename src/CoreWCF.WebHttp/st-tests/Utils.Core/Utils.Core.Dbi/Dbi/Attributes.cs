using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using ST.Utils.Attributes;
using PostSharp.Aspects;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Dependencies;
using PostSharp.Aspects.Serialization;
using PostSharp.Extensibility;
using ST.Utils.Exceptions;
using PostSharp.Serialization;

namespace ST.Utils
{
  public partial class Dbi
  {
    [DebuggerStepThrough]
    [EditorBrowsable( EditorBrowsableState.Never )]
    public abstract class BindBaseAttribute : Attribute
    {
    }

    /// <summary>
    /// Аттрибут указывает на то, что помеченное свойство не подлежит обработке ни при воссоздании
    /// объекта ни при передаче значений свойств объекта хранимой процедуре как параметров.
    /// Имеет самый высокий приоритет из всех аттрибутов BindXxx.
    /// </summary>
    [DebuggerStepThrough]
    [AttributeUsage( AttributeTargets.Property, AllowMultiple = false, Inherited = false )]
    public class BindNoneAttribute : BindBaseAttribute
    {
      #region .Ctor
      /// <summary>
      /// Конструктор.
      /// </summary>
      public BindNoneAttribute()
      {
      }
      #endregion
    }

    /// <summary>
    /// Аттрибут указывает на то, что при передаче хранимой процедуре значений свойств объекта,
    /// помеченное свойство не подлежит обработке. Имеет приоритет выше, чем Bind, BindIn и BindInEx.
    /// </summary>
    [DebuggerStepThrough]
    [AttributeUsage( AttributeTargets.Property, AllowMultiple = false, Inherited = false )]
    public sealed class BindInNoneAttribute : BindNoneAttribute
    {
      #region .Ctor
      /// <summary>
      /// Конструктор.
      /// </summary>
      public BindInNoneAttribute()
      {
      }
      #endregion
    }

    /// <summary>
    /// Аттрибут указывает на то, что при воссоздании объекта, помеченное свойство
    /// не подлежит обработке. Имеет приоритет выше, чем Bind, BindOut и BindOutEx.
    /// </summary>
    [DebuggerStepThrough]
    [AttributeUsage( AttributeTargets.Property, AllowMultiple = false, Inherited = false )]
    public sealed class BindOutNoneAttribute : BindNoneAttribute
    {
      #region .Ctor
      /// <summary>
      /// Конструктор.
      /// </summary>
      public BindOutNoneAttribute()
      {
      }
      #endregion
    }

    [DebuggerStepThrough]
    [AttributeUsage( AttributeTargets.Property, AllowMultiple = false, Inherited = true )]
    [EditorBrowsable( EditorBrowsableState.Never )]
    public abstract class BindNonEmptyAttribute : BindBaseAttribute
    {
      #region .Fields
      private List<string> _names;
      #endregion

      #region .Properties
      /// <summary>
      /// Список синонимов названия помеченного свойства.
      /// </summary>
      public List<string> Names
      {
        get { return _names; }
      }
      #endregion

      #region .Ctor
      private BindNonEmptyAttribute()
      {
      }

      protected BindNonEmptyAttribute( string name ) : this( new string[] { name } )
      {
      }

      protected BindNonEmptyAttribute( [NotNull] params string[] names )
      {
        if( names.Any( s => string.IsNullOrWhiteSpace( s ) ) )
          throw new ArgumentException( "Name can't be empty or null.", "names" );

        _names = names.Select( n => n.ToUpper() ).ToList();
      }
      #endregion
    }

    /// <summary>
    /// Аттрибут является консолидацией аттрибутов BindIn и BindOut.
    /// Указывает названия параметров хранимых процедур, которым будет присвоено
    /// значение помеченного свойства и полей из результатов работы хранимых процедур,
    /// значения которых будут присвоены помеченному свойству.
    /// </summary>
    [DebuggerStepThrough]
    [AttributeUsage( AttributeTargets.Property, AllowMultiple = false, Inherited = true )]
    public sealed class BindAttribute : BindNonEmptyAttribute
    {
      #region .Ctor
      private BindAttribute()
      {
      }

      /// <summary>
      /// Конструктор.
      /// </summary>
      /// <param name="name">Название параметра хранимой процедуры.</param>
      public BindAttribute( string name ) : base( name )
      {
      }

      /// <summary>
      /// Конструктор.
      /// </summary>
      /// <param name="names">Названия параметров хранимых процедур.</param>
      public BindAttribute( params string[] names ) : base( names )
      {
      }
      #endregion
    }

    /// <summary>
    /// Атрибут указывает названия параметров хранимых процедур, которым будет присвоено
    /// значение помеченного свойства. Имеет приоритет выше чем у аттрибута Bind.
    /// </summary>
    [DebuggerStepThrough]
    [AttributeUsage( AttributeTargets.Property, AllowMultiple = false, Inherited = true )]
    public sealed class BindInAttribute : BindNonEmptyAttribute
    {
      #region .Ctor
      private BindInAttribute()
      {
      }

      /// <summary>
      /// Конструктор.
      /// </summary>
      /// <param name="name">Название параметра хранимой процедуры.</param>
      public BindInAttribute( string name ) : base( "@" +  name )
      {
      }

      /// <summary>
      /// Конструктор.
      /// </summary>
      /// <param name="names">Названия параметров хранимых процедур.</param>
      public BindInAttribute( params string[] names ) : base( names.Select( n => "@" + n ).ToArray() )
      {
      }
      #endregion
    }

    /// <summary>
    /// Атрибут указывает названия полей из результатов работы хранимых процедур,
    /// значения которых будут присвоены помеченному свойству. Имеет приоритет выше чем у аттрибута Bind.
    /// </summary>
    [DebuggerStepThrough]
    [AttributeUsage( AttributeTargets.Property, AllowMultiple = false, Inherited = true )]
    public sealed class BindOutAttribute : BindNonEmptyAttribute
    {
      #region .Ctor
      private BindOutAttribute()
      {
      }

      /// <summary>
      /// Конструктор.
      /// </summary>
      /// <param name="name">Название поля из результата работы хранимой процедуры.</param>
      public BindOutAttribute( string name ) : base( name )
      {
      }

      /// <summary>
      /// Конструктор.
      /// </summary>
      /// <param name="names">Названия полей из результатов работы хранимых процедур.</param>
      public BindOutAttribute( params string[] names ) : base( names )
      {
      }
      #endregion
    }

    [DebuggerStepThrough]
    [AttributeUsage( AttributeTargets.Property, AllowMultiple = true, Inherited = false )]
    [EditorBrowsable( EditorBrowsableState.Never )]
    public abstract class BindExAttribute : BindNonEmptyAttribute
    {
      #region .Fields
      private string _spName;
      #endregion

      #region .Properties
      /// <summary>
      /// Название хранимой процедуры.
      /// </summary>
      public string SPName
      {
        get { return _spName; }
      }
      #endregion

      #region .Ctor
      protected BindExAttribute( [NotNullNotEmpty] string spName, string name ) : base( name )
      {
        _spName = Utils.GetNormalizedName( spName );
      }
      #endregion
    }

    /// <summary>
    /// Атрибут указывает для заданной хранимой процедуры название параметра, которому будет присвоено
    /// значение помеченного свойства. Имеет приоритет выше чем у аттрибута BindIn.
    /// </summary>
    [DebuggerStepThrough]
    [AttributeUsage( AttributeTargets.Property, AllowMultiple = true, Inherited = false )]
    public sealed class BindInExAttribute : BindExAttribute
    {
      #region .Ctor
      /// <summary>
      /// Конструктор.
      /// </summary>
      /// <param name="spName">Название хранимой процедуры.</param>
      /// <param name="name">Название параметра хранимой процедуры.</param>
      public BindInExAttribute( string spName, string name ) : base( spName, "@" + name )
      {
      }
      #endregion
    }

    /// <summary>
    /// Атрибут указывает название поля из результата работы хранимой процедуры,
    /// значение которого будет присвоено помеченному свойству. Имеет приоритет выше чем у аттрибута BindOut.
    /// </summary>
    [DebuggerStepThrough]
    [AttributeUsage( AttributeTargets.Property, AllowMultiple = true, Inherited = false )]
    public sealed class BindOutExAttribute : BindExAttribute
    {
      #region .Ctor
      /// <summary>
      /// Конструктор.
      /// </summary>
      /// <param name="spName">Название хранимой процедуры.</param>
      /// <param name="name">Название поля из результата работы хранимой процедуры.</param>
      public BindOutExAttribute( string spName, string name ) : base( spName, name )
      {
      }
      #endregion
    }

    [PSerializable]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    [MulticastAttributeUsage(MulticastTargets.Method, TargetMemberAttributes = MulticastAttributes.Public | MulticastAttributes.Instance | MulticastAttributes.NonAbstract, AllowMultiple = false, Inheritance = MulticastInheritance.None)]
    [MethodInterceptionAspectConfiguration(SerializerType = typeof(MsilAspectSerializer))]
    [ProvideAspectRole(StandardRoles.ExceptionHandling)]
    [AspectRoleDependency(AspectDependencyAction.Order, AspectDependencyPosition.After, StandardRoles.Validation)]
    internal sealed class SqlExceptionHandlerAttribute : OnExceptionAspect
    {
      #region CompileTimeValidate
      public override bool CompileTimeValidate(MethodBase method)
      {
        if (!typeof(IConnectionHolder).IsAssignableFrom(method.DeclaringType))
          return AspectHelper.Fail(1, "The type {0} should realize the interface IConnectionHolder in order to apply SqlExceptionHandlerAttribute.", method.DeclaringType);

        return true;
      }
      #endregion

      #region GetExceptionType
      public override Type GetExceptionType(MethodBase targetMethod)
      {
        return typeof(SqlException);
      }
      #endregion

      #region OnException
      [DebuggerStepThrough]
      public override void OnException(MethodExecutionArgs args)
      {
        var exc = (SqlException)args.Exception;

        var ch = (IConnectionHolder)args.Instance;

        var server = ch.Connection.DataSource.GetEmptyOrTrimmed();
        var database = ch.Connection.InitialCatalog.GetEmptyOrTrimmed();

        if (DatabaseConnectionException.IsConnectionError(exc.Number))
          throw new DatabaseConnectionException(exc, server, database) as Exception;

        if (exc.Number >= 50000)
          throw new RaiseException(exc, server, database) as Exception;

        switch (exc.Number)
        {
          case 2601:
            throw new UniqueIndexViolationException_2601(exc, server, database) as Exception;
          case 2627:
            throw new UniqueConstraintViolationException_2627(exc, server, database) as Exception;
          case 1919:
            throw new InvalidAsKeyColumnInIndexException_1919(exc, server, database) as Exception;
          case 547:
            throw new ForeignKeyOrReferenceConstraintViolationException_547(exc, server, database) as Exception;
          case 515:
            throw new ColumnNotNullViolationException_515(exc, server, database) as Exception;
          case 245:
            throw new TypeConversionFailedException_245(exc, server, database) as Exception;
          case 206:
            throw new ImplicitConversionFailedException_206(exc, server, database) as Exception;
          case 257:
            throw new ImplicitConversionFailedException_257(exc, server, database) as Exception;
          case 8114:
            throw new CannotConvertException_8114(exc, server, database) as Exception;
          case 8152:
            throw new StringTruncationException_8152(exc, server, database) as Exception;
        }

        throw new DatabaseGenericException(exc, server, database) as Exception;
      }
      #endregion
    }
  }
}
