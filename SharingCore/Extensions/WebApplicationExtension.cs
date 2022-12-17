using Daily.SharingCore.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Daily.SharingCore.Extensions
{
    public static class WebApplicationExtension
    {
#if NET6_0_OR_GREATER
        public static WebApplicationBuilder InjectSharingCore(this WebApplicationBuilder builder)
        {
            InternalApp.Configuration = builder.Configuration;

            builder.Services.AddHostedService<GenericHostedService>();

            builder.Services.AddHttpContextAccessor();

            return builder;
        }
#endif
    }
}