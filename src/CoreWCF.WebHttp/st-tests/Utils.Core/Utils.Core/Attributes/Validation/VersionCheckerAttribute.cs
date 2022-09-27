using PostSharp.Aspects;
using PostSharp.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ST.Utils.Attributes
{
  [AttributeUsage( AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Parameter | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event )]
  [MulticastAttributeUsage( MulticastTargets.Class | MulticastTargets.StaticConstructor | MulticastTargets.Parameter | MulticastTargets.Method | MulticastTargets.Property | MulticastTargets.Field | MulticastTargets.Event )]
  public class VersionCheckerAttribute : Aspect
  {
    private Version _allowedVersion;

    public Version AllowedVersion
    {
      get
      {
        return _allowedVersion;
      }
    }

    public VersionCheckerAttribute( string alowedVersion )
    {
      _allowedVersion = Version.Parse( alowedVersion );
    }

    public static IEnumerable<Enum> GetFlags( Enum input )
    {
      foreach ( Enum value in Enum.GetValues( input.GetType() ) )
        if ( input.HasFlag( value ) )
          yield return value;
    }

    public override bool CompileTimeValidate( object target )
    {
      Type declaringType = null;

      var allowedTargets = string.Join( ",", this.GetType().GetCustomAttributesData().IfNotNull( data => GetFlags( (AttributeTargets)data.First( cad => cad.Constructor.DeclaringType == typeof( AttributeUsageAttribute ) && cad.Constructor.GetParameters().First().ParameterType == typeof( AttributeTargets ) ).ConstructorArguments.First().Value ).Select( v => v.ToString() ).Concat( GetFlags( (MulticastTargets)data.First( cad => cad.Constructor.DeclaringType == typeof( MulticastAttributeUsageAttribute ) && cad.Constructor.GetParameters().First().ParameterType == typeof( MulticastTargets ) ).ConstructorArguments.First().Value ).Select( v => v.ToString() ) ).Distinct( StringComparer.InvariantCultureIgnoreCase ).OrderBy( n => n, StringComparer.InvariantCultureIgnoreCase ) ) );

      //AspectHelper.Fail( 11, target.ToString() + "-" + target.GetType().Name + "-" + target.GetType().IsClass.ToString() );

      //AspectHelper.Fail( 11,
      //   target.ToString() + "-" + target.GetType().Name + "-" + target.GetType().IsPublic.ToString() + "|"
      //  //c1.ToString() + " | " +
      //  //+ string.Join( "; ", this.GetType().GetCustomAttributesData().Select( cad => string.Format( "{5} - Constructor: {0}, ConstructorArguments ({1}): '{2}', NamedArguments ({3}): '{4}'", cad.Constructor, cad.ConstructorArguments.Count, string.Join( ",", cad.ConstructorArguments.Select( cata => string.Format( "{1}[{0}]", cata.ArgumentType.Name, cata.Value ) ) ), cad.NamedArguments.Count, string.Join( ",", cad.NamedArguments.Select( cana => string.Format( "{0}-{1}", cana.MemberInfo, string.Format( "{1}[{0}]", cana.TypedValue.ArgumentType.Name, cana.TypedValue.Value ) ) ) ), cad.Constructor.DeclaringType.Name ) ) ) 
      //);

      target.IfIs<Type>( type =>
        {
          declaringType = type;
        }
        , () =>
          target.IfIs<MemberInfo>( mi =>
          {
            //AspectHelper.Fail( 3, "@" + mi.ToString() );
            declaringType = mi.DeclaringType;
          },
          () =>
            target.IfIs<ParameterInfo>( pi =>
            {
              declaringType = pi.Member.DeclaringType;
              //AspectHelper.Fail( 4, "4." + declaringType.ToString() );
            },
            () =>
            {
              AspectHelper.Fail( 1, "Attribute '" + GetType().Name + "' applied to the not allowed target: " + target.GetType().FullName + ". Allowed targets: " + allowedTargets );
            } ) ) );

      if ( declaringType == null )
        AspectHelper.Fail( 666, "Type of the target '" + target.ToString() + "' doesn't resolved" );
      else
      {
        var asmName = declaringType.Assembly.GetName();
        var curVer = asmName.Version;
        if ( curVer != _allowedVersion )
        {
          if ( _allowedVersion.Revision > -1 || curVer.Major != _allowedVersion.Major || curVer.Minor != _allowedVersion.Minor || curVer.Build != _allowedVersion.Build )
            return AspectHelper.Fail( 1, "Target '" + target.ToString() + "' can be used only in the project with the version" + ( _allowedVersion.Revision == -1 ? "s" : string.Empty ) + ": " + _allowedVersion.ToString() + ( _allowedVersion.Revision == -1 ? ".*" : string.Empty ) + ". Project version: " + curVer.ToString() + ", Assembly: " + asmName.ToString() + ", Type: " + declaringType.ToString() );
        }
      }

      return base.CompileTimeValidate( target );
    }
  }
}
