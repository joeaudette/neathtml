using System;
using System.Reflection;
using System.Runtime.CompilerServices;

// Information about this assembly is defined by the following
// attributes.
//
// change them to the information which is associated with the assembly
// you compile.
[assembly:CLSCompliant(true)]
[assembly: AssemblyTitle("NeatHtmlUnitTests")]
[assembly: AssemblyDescription("NeatHtml unit tests for NUnit..")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Dean Brettle")]
[assembly: AssemblyProduct("NeatHtml")]
[assembly: AssemblyCopyright("Copyright 2006 Dean Brettle.  Licensed under the Lesser General Public License.")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// The assembly version has following format :
//
// Major.Minor.Build.Revision
//
// You can specify all values by your own or you can build default build and revision
// numbers with the '*' character (the default):

[assembly: AssemblyVersion("1.0.*")]

[assembly: AssemblyInformationalVersion("trunk")]

// The following attributes specify the key for the sign of your assembly. See the
// .NET Framework documentation for more information about signing.
// This is not required, if you don't want signing let these attributes like they're.
[assembly: AssemblyDelaySign(false)]
[assembly: AssemblyKeyFile("")]

#if USE_LOG4NET
[assembly: log4net.Config.XmlConfigurator(ConfigFile="log4net.config", Watch=true)]
#else
#warning LOGGING DISABLED.  To enable logging, add a reference to log4net and define USE_LOG4NET.
#endif

