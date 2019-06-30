# AI4E.AspNetCore.Components.Extensions
'AI4E.AspNetCore.Components.Extensions' provides extensions and utilities for use with Asp.Net Core Razor Components and Blazor.
For installation and usage instructions see the respective project description in the [wiki](https://github.com/AI4E/AI4E.AspNetCore.Components.Extensions/wiki/Home).

# Versions and Roadmap

| Release date | Version | Pre-release | Blazor/Asp.Net Core version |
| --- | --- | --- | --- |
| 2019-02-28 | 0.1.0 | YES | Blazor 0.7.0 |
| 2019-04-20 | 0.2.0 | YES | Asp.Net Core 3.0 preview 4 |
| 2019-05-25 | 0.3.0 | YES | Asp.Net Core 3.0 preview 5 |
| 2019-06-30 | 0.3.1 | YES | Asp.Net Core 3.0 preview 6 |
| 2019 | 0.4.0 | YES | >= Asp.Net Core 3.0 preview 6 |
| 2019 | 1.0.0 | NO | Asp.Net Core 3.0 |

# Nuget packages
The projects are uploaded to nuget with the respective project name:
* [AI4E.AspNetCore.Blazor.Logging](https://www.nuget.org/packages/AI4E.AspNetCore.Blazor.Logging/)
* [AI4E.AspNetCore.Components.Routing](https://www.nuget.org/packages/AI4E.AspNetCore.Components.Routing/)
* [AI4E.AspNetCore.Components.Extensibility](https://www.nuget.org/packages/AI4E.AspNetCore.Components.Extensibility/)

|  :bulb: Although the builds should be stable, these are pre-releases and should not be used in production code |
| --- |

# AI4E.AspNetCore.Components.Routing
This is a fork of the built-in Asp.Net Core Razor Components routing system that makes available a base class ExtensibleRouter to implement custom routers on top of it. There are also router implementations that can be used directly.
* [Installation and usage](https://github.com/AI4E/AI4E.AspNetCore.Components.Extensions/wiki/AI4E.AspNetCore.Components.Routing#install-and-usage)
* [Implementing a custom router](https://github.com/AI4E/AI4E.AspNetCore.Components.Extensions/wiki/AI4E.AspNetCore.Components.Routing#implementing-a-custom-router)

# AI4E.AspNetCore.Blazor.Logging
AI4E.AspNetCore.Blazor.Logging contains a console logger for client-side blazor projects.
* [Installation and usage](https://github.com/AI4E/AI4E.AspNetCore.Components.Extensions/wiki/AI4E.AspNetCore.Blazor.Logging)

# AI4E.AspNetCore.Components.Extensibility
The project provides extensions and utilities for Asp.Net Core Razor component based projects to be split into modules/plugins. It implements a custom router that is able to update its route table when the set of installed modules change. Additionally there is a concept called view extensions that can extend existing components with additional content, for example to render nav-menu entries or extend the edit form for an entity in a modular (microservice oriented) way.
* [Module installation](https://github.com/AI4E/AI4E.AspNetCore.Components.Extensions/wiki/AI4E.AspNetCore.Components.Extensibility#module-installation)
* [Installation](https://github.com/AI4E/AI4E.AspNetCore.Components.Extensions/wiki/AI4E.AspNetCore.Components.Extensibility#installation)
* [View extensions](https://github.com/AI4E/AI4E.AspNetCore.Components.Extensions/wiki/AI4E.AspNetCore.Components.Extensibility#view-extensions)
