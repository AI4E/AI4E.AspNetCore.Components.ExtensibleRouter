# AI4E.AspNetCore.Components.Extensions
Provides extensions for Asp.Net Core Razor Components (and Blazor).  

## AI4E.AspNetCore.Components.Routing
This is a fork of the built-in Asp.Net Core Razor Components routing system that makes available a base class `ExtensibleRouter` to implement custom routers on top of it. There are also router implementations that can be used directly.

### Install
Install the nuget package via the Visual studio GUI or  
```
Install-Package AI4E.AspNetCore.Components.Routing
```

For using a built-in router, an `addTagHelper` directive has to be places in yout component or in a `_ViewImports file` in the project to make available the router components.  
```
@addTagHelper *, AI4E.AspNetCore.Components.Routing
<DefaultRouter AppAssembly="typeof(Startup).Assembly" FallbackComponent="typeof(_404)"/>
```
