using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Qx.SignalR
{
    public static class QxDependencyInjectionExtensions
    {
        public static ISignalRServerBuilder AddQx(this ISignalRServerBuilder builder, Func<QxOptions, QxOptions>? optionsBuilder = null)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.Services.AddSingleton<IQxService>(sp => new DefaultQxService(
                options: optionsBuilder?.Invoke(QxOptions.Default) ?? QxOptions.Default,
                authorizationService: sp.GetRequiredService<IAuthorizationService>(),
                authorizationPolicyProvider: sp.GetRequiredService<IAuthorizationPolicyProvider>()));

            return builder;
        }
    }
}
