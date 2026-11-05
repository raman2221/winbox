// Records with init-only setters need this type, which .NET Framework 4.8 does not ship.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
