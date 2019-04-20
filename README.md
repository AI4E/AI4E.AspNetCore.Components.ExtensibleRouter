# AI4E.AspNetCore.Components.Extensions
Provides extensions for Asp.Net Core Razor Components (and Blazor).  

## AI4E.AspNetCore.Components.Routing
This is a fork of the built-in Asp.Net Core Razor Components routing system that makes available a base class `ExtensibleRouter` to implement custom routers on top of it. There are also router implementations that can be used directly.

### Install and usage
Install the nuget package via the Visual studio GUI or  
```
Install-Package AI4E.AspNetCore.Components.Routing
```

For using a built-in router, a `using` directive has to be placed in your component or in a `_Imports.razor` file in the project to make available the `DefaultRouter` component.  
```
@using AI4E.AspNetCore.Components.Routing
<DefaultRouter AppAssembly="typeof(Startup).Assembly" FallbackComponent="typeof(_404)"/>
```

### Implementing a custom router
The `ExtensibleRouter` base type has virtual methods that can be overriden to plug-in custom routing behavior.  

The `void OnInit()` method is called when the router is initialized before any routing operation is performed.

The router calls the abstract method `IEnumerable<Type> ResolveRoutableComponents()` when it needs to update its route table. The method should return all routable component type. The `DefaultRouter` loads these types via the publically available `ComponentResolver` type.  

Custom action can be registered before and after each render operation by overriden the methods ´void OnBeforeRefresh(string locationPath)´ and ´void OnAfterRefresh(bool success)´ respectively. ´locationPath´ contains the location the user navigated to, ´success´ is a boolean value indicating whether the routing operation was successful.  

The router can trigger an update of the route table by invoking the `void UpdateRouteTable()` method and can initiate a routing operation by invoking `void Refresh()`. Be aware that `UpdateRouteTable` does not refresh and `Refresh` does not update the route table. To do both, call `UpdateRouteTable` first and Refresh thereafter.

To override the actual render operation, there is a virtual method `void Render(RenderTreeBuilder builder, Type handler, IDictionary<string, object> parameters)` that is called to render the page of type `handler` with the specified parameters. Be aware that this sets the layout by default and the layout has to be set manually when overriden.  

When the router is disposed, its calls the `void Dispose(bool disposing)` method. If this is overriden in a derviced class, the base implementation should be called to guarantee safe cleanup.  

## AI4E.AspNetCore.Blazor.Logging
AI4E.AspNetCore.Blazor.Logging contains a console logger for client-side blazor projects. 

Install the nuget package via the Visual studio GUI or 
```
Install-Package AI4E.AspNetCore.Blazor.Logging
```

To enable the logger, add it to the logging builder in the *client-side* `ConfigureServices` method of the `Startup` class.

```
public void ConfigureServices(IServiceCollection services)
{
   services.AddLogging(builder =>
   {
      builder.AddBrowserConsole();
      builder.SetMinimumLevel(LogLevel.Trace);
   });
}
```

## AI4E.AspNetCore.Components.Extensibility
The project provides extensions and utilities for Asp.Net Core Razor component based projects to be split into modules/plugins. It implements a custom router that is able to update its route table when the set of installed modules change. Additionally there is a concept called view extensions that can extend existing components with additional content, for example to render nav-menu entries or extend the edit form for an entity in a modular (microservice oriented) way.

## Module installation
The project does *NOT* implement the installation of modules/plugin. You can use the AI4E.AspNetCore.Modularity package for this. It can be found [here](https://github.com/AI4E/AI4E/tree/master/src/AI4E.AspNetCore.Components.Modularity).  
<aside class="warning">
The AI4E.AspNetCore.Components.Modularity is in actively development currently and is not ready for production use.
</aside>
To implement your own module loader you can base on the [PluginManager](https://github.com/AI4E/AI4E.AspNetCore.Components.Extensions/blob/master/samples/Routing.ModularRouterSample/Routing.ModularRouterSample/Services/PluginManager.cs) type in the sample project.  
<aside class="warning">
The PluginManager can be used for server-side blazor projects only.
</aside>

## Installation
Install the nuget package via the Visual studio GUI or  
```
Install-Package AI4E.AspNetCore.Components.Extensibility
```

## Integration in the module host
Replace the default router with the `ModularRouter` in the `App.razor` file.  

```
@using AI4E.AspNetCore.Components.Routing
<ModularRouter />
```

To make available the modular capabilities of the project, add an instance of the `IAssemblySource` interface to the service collection in the `ConfigureServices` method of the `Startup` class.

```
public void ConfigureServices(IServiceCollection services)
{
   services.AddSingleton<IAssemblySource, YourAssemblySourceImplementation >();
}
```

A default implementation of the `IAssemblySource` interface can be found on the [sample project](https://github.com/AI4E/AI4E.AspNetCore.Components.Extensions/blob/master/samples/Routing.ModularRouterSample/Routing.ModularRouterSample/Services/AssemblyManager.cs).

## View extensions
A view extension consists of three parts.
* A view extension definition that is a name for the view extension
* A view extension implementation
* A view extension placeholder that defined the place the view extensions are rendered

In order that the view extension is detected, the assembly that implements the view extension implementation has to be part of the collection of assemblies returned from the `IAssemblySource.Assemblies` property.

### View extension definition
A view extension definition is an interface that provides a name for the view extension. The interface should inherit the `AI4E.AspNetCore.Components.Extensibility.IViewExtensionDefinition` interface. To be accessible for both, the implementation and the place it is rendered, it should be placed in a shared project.  

A view extension definition looks like in the following snippet.
```
public interface IMenuViewExtensionDefinition : IViewExtensionDefinition { }
```

To pass data from the placeholder to the implementation, the definition can implement the generic `AI4E.AspNetCore.Components.Extensibility.IViewExtensionDefinition<TContext>` interface instead.

This looks like:
```
public interface IIndexPageViewExtensionDefinition : IViewExtensionDefinition<IndexPageViewExtensionContext> { }

public sealed class IndexPageViewExtensionContext
{
   public string Message { get; set; }
   public int Number { get; set; }
}
```

### View extension implementation
To implement a view extension, the component should implement the view definition interface, like in the following snippet.  

```
@implements IMenuViewExtensionDefinition
<li class="nav-item px-3">
    <NavLink class="nav-link" href="plugin" Match="NavLinkMatch.All">
        <span class="oi oi-home" aria-hidden="true"></span> PluginPage
    </NavLink>
</li>
```

If the view extension definition includes a context, the implementation should define a parameter property with a matching type.

```
@implements IIndexPageViewExtensionDefinition
@functions {
    [Parameter] private IndexPageViewExtensionContext Context { get; set; }
}

<div>
    <h2>View extension rendered by the plugin!</h2>

    @if (Context != null)
    {
        <div>
            Passed view extension context:
        </div>
        <div>
            Message is: @Context.Message
        </div>
        <div>
            Number is: @Context.Number
        </div>
    }
    else
    {
        <div>View extension context is not present.</div>
    }
</div>
```
### View extension placeholder
To render all view extensions of a type, place a `AI4E.AspNetCore.Components.Extensibility.ViewExtensionPlaceholder<TViewExtension>` component at the place in the markup where the view extensions shall be rendered and specify the view extension defintion as generic parameter.

```
<ViewExtensionPlaceholder TViewExtension="IMenuViewExtensionDefinition"/>
```

To pass a context to the view extension, use the Context parameter of the `ViewExtensionPlaceholder` type.
```
