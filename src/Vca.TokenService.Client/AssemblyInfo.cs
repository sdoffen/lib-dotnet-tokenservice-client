using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// In SDK-style projects such as this one, several assembly attributes that were historically
// defined in this file are now automatically added during build and populated with
// values defined in project properties. For details of which attributes are included
// and how to customize this process see: https://aka.ms/assembly-info-properties

// Setting ComVisible to false makes the types in this assembly not visible to COM
// components.  If you need to access a type in this assembly from COM, set the ComVisible
// attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM.
[assembly: Guid("89ca3c74-4a2c-487a-8581-769d2e4adee3")]

// Identify that the assembly is not CLS-compliant.
[assembly: CLSCompliant(false)]

// InternalsVisibleTo attribute is used to specify that the internal types of this assembly
// are visible to another assembly. This is often used for unit testing purposes.
// The specified assembly name must match the name of the assembly that will access the internal types.
// See https://learn.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.internalsvisibletoattribute
[assembly: InternalsVisibleTo("Vca.TokenService.Client.Tests")]