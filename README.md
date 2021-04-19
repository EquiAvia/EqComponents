# EqComponents
A Free Blazor only Tree View control

Add the equiavia.components.Library nuget package to you ASP.NET Core or Blazor Server project.

Add the following into the Startup.cs for server hosted or server-side blazor projects
```c#
using equiavia.components.Library;

...

public void ConfigureServices(IServiceCollection services)
{
    EqComponents.Initialize(services);
    ....
}
```

Add the equiavia.components.Library nuget package to you Blazor client project.

Add the following into the Program.cs for the client-side blazor projects
```c#
using equiavia.components.Library;

...

public static async Task Main(string[] args)
{
    ...
    EqComponents.Initialize(builder.Services);
    await builder.Build().RunAsync();
}
```
