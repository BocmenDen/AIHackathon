using AIHackathon.DB.Models;
using BotCore.Interfaces;
using BotCore.PageRouter.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using System.Reflection;

namespace AIHackathon.Base
{
    public abstract class PageBaseClearCache : PageBase, IGetCacheOptions, IBindService<IMemoryCache>
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        protected IMemoryCache MemoryCache { get; private set; } = null!;

        public void BindService(IMemoryCache service) => MemoryCache = service;

        protected override async Task OnExit(IUpdateContext<User> context)
        {
            await _cancellationTokenSource.CancelAsync();
            await base.OnExit(context);
        }

        public MemoryCacheEntryOptions GetCacheOptions()
        {
            var options = new MemoryCacheEntryOptions();
            PageCacheableAttribute? pageCacheableAttribute = GetType().GetCustomAttribute<PageCacheableAttribute>();
            if (pageCacheableAttribute != null)
                options.SlidingExpiration = pageCacheableAttribute.SlidingExpiration;
            options.AddExpirationToken(new CancellationChangeToken(_cancellationTokenSource.Token));
            options.RegisterPostEvictionCallback((object key, object? value, EvictionReason reason, object? state) =>
            {
                MemoryCache.Remove(key);
                _cancellationTokenSource.Dispose();
            });
            return options;
        }
    }
}
