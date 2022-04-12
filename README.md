# Application Context

The goal of the application context is to be able to have access to state of the current running context such as the current user. So from any place in the application you can access the current use like this `var user = ApplicationContext.GetFeature<IUser>();`

You can have both default features and per context features for simpler configuration.

To Configure in your app

```
var builder = WebApplication.CreateBuilder(opts);

builder.Services.AddApplicationContext( builder => {
    builder.Add<IClock>();
    builder.Add<IFileStorage>();           
});

var app = builder.Build();
await app.UseDefaultApplicatonContext();
```

To also add context information to aspnet middleware.

```
builder.Services.AddApplicationContext( builder => {
... 
}).WithMiddleware( builder => {
    // add context specific featurs
    build.Add(Guid.NewGuid());

    // if you need access to the HttpContext you can use the following
    builder.WithHttpContext( async context => {
        if (context.User?.Identity?.IsAuthenticated ?? false)
        {
            var userAccessor = context.RequestServices.GetRequiredService<IUserAccessor>();
            return await userAccessor.GetUser();
        }
        return null;
    });
});
```

There are multiple ways to add to the context using the builder.

1. `builder.Add("hello")` will add a string feature with the value 'hello'
1. `builder.Add<object>("hello")` will add a feature with the type object and the value 'hello'.
1. `builder.Add<IClock>()` will add a feature with the type IClock and get the value from the Service Provider.
1. `builder.AddFactory<IServiceProvider,IUser>( serviceProvider => serviceProvider.GetService<IUser> as IUser )` will add a feature of type IUser if the IServiceProvider was given at build time.
1. `builder.AddAsyncFactory<IServiceProvider,IUser>( async serviceProvider => await serviceProvider.GetService<IUser> as IUser )` same as AddFactory but with an async factory.
1. `builder.AddBuilderFor<T>( builder => {})` adds a builder that will be used when a Context for type T is requested.