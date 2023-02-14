using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SharingCore.Common;

namespace SharingCore.Extensions
{
    public static class WebApplicationExtension
    {
#if NET6_0_OR_GREATER
        public static WebApplicationBuilder InjectSharingCore(this WebApplicationBuilder builder)
        {
            SharingCoreUtils.Configuration = builder.Configuration;

            builder.Services.AddHostedService<GenericHostedService>();

            builder.Services.AddHttpContextAccessor();

            return builder;
        }
#endif
    }
}