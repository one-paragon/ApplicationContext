using Microsoft.AspNetCore.Http;

namespace OneParagon.Infrasctucture;
    
    public class ApplicationContextMiddlewareBuilder : ApplicationContextBuilder
    {
        public ApplicationContextMiddlewareBuilder WithHttpContext<T>(Func<HttpContext, Task<T>> func)
            where T : notnull
        {
            this.AddAsyncFactory(func);
            return this;
        }
    }