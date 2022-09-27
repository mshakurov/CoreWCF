using PostSharp;
using PostSharp.Constraints;
using PostSharp.Extensibility;
using PostSharp.Reflection;
using PostSharp.Reflection.MethodBody;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ST.Utils;

namespace ST.Utils.Attributes
{
  /// <summary>
  /// Атрибут ограничения, гарантирующий, что тип значения, передаваемого при вызове метода, в параметр, помеченный данным атрибутом, не входит в список запрещенных типов (указанных в конструкторе данного атрибута)
  /// </summary>
  [MulticastAttributeUsage( MulticastTargets.Parameter, Inheritance = MulticastInheritance.Strict )]
  public class MethodCallParameterValueNotInheritedFromAttribute : ReferentialConstraint
  {
    private readonly Type[] ProhibitedTypes;

    private MethodCallParameterValueNotInheritedFromAttribute()
    {

    }

    /// <summary>
    /// Конструткор ограничения
    /// </summary>
    /// <param name="prohibitedTypes">Список запрещенных типов для значениия, передаваемого в параметр, помеченный данным атрибутом</param>
    public MethodCallParameterValueNotInheritedFromAttribute( params Type[] prohibitedTypes )
    {
      ProhibitedTypes = prohibitedTypes;
    }

    public override void ValidateCode( object target, Assembly assembly )
    {
      var parameter = (ParameterInfo)target;

      var usages = ReflectionSearch.GetMethodsUsingDeclaration( parameter.Member );

      //var sb = new StringBuilder();

      foreach ( var mu in usages )
      {
        var mi = mu.UsingMethod;

        var service = PostSharpEnvironment.CurrentProject.GetService<IMethodBodyService>( true );
        var body = service.GetMethodBody( mi, MethodBodyAbstractionLevel.ExpressionTree );

        var visitor = new Visitor( parameter, ProhibitedTypes/*, sb*/ );
        visitor.VisitMethodBody( body );
      }

      //Message.Write( parameter, SeverityType.Warning, "777", string.Format( "[{0}]. {1}", parameter.ToString(), string.Join( ", ", usages.Select( u => string.Format( "{0}-{1}-{2}-{3} | {4}", u.Instructions, u.UsedDeclaration, u.UsedType, u.UsingMethod, sb ) ) ) ) );

      base.ValidateCode( target, assembly );
    }

    private class Visitor : MethodBodyVisitor
    {
      private readonly ParameterInfo Parameter;
      //private readonly StringBuilder sb;
      private readonly Type[] ProhibitedTypes;

      public Visitor( ParameterInfo parameter, Type[] prohibitedTypes/*, StringBuilder stringBuilder*/ )
      {
        this.Parameter = parameter;
        //this.sb = stringBuilder;
        this.ProhibitedTypes = prohibitedTypes;
      }

      public override object VisitMethodCallExpression( IMethodCallExpression expression )
      {
        if ( Parameter.Member == expression.Method )
        {
          //sb.AppendFormat( "Expression: {0} ({1}) {2}", expression, expression.Method, expression.Arguments[Parameter.Position].ReturnType ).AppendLine();
          var retType = expression.Arguments[Parameter.Position].ReturnType;
          foreach ( var type in ProhibitedTypes )
            if ( retType.IsInheritedFrom( type ) )
              Message.Write( MessageLocation.Of( expression ), SeverityType.Error, "PROHPARTYPE", string.Format( "Нельзя вызывать метод '{0}' класса '{1}' со значением параметра {2} ('{3}'), имеющим тип {4}{5}", Parameter.Member, Parameter.Member.DeclaringType.FullName, Parameter.Position + 1, Parameter.Name, retType.FullName, type == retType ? string.Empty : string.Format( ", наследованный от {0}", type.FullName ) ) );
        }

        return base.VisitMethodCallExpression( expression );
      }
    }
  }
}
