using System;
using System.Collections.Generic;
using System.Reflection;

namespace AI4E.AspNetCore.Components.Extensibility
{
    public interface IAssemblySource
    {
        IReadOnlyCollection<Assembly> Assemblies { get; }
        event EventHandler AssembliesChanged;
    }
}
