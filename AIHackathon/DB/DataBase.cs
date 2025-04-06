using BotCore.EfDb;
using BotCore.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace AIHackathon.DB
{
    [DB]
    public class DataBase : DbContext, IDBUser<User, Telegram.Bot.Types.Chat>
    {
        private readonly IMemoryCache _memoryCache;
        private readonly TimeSpan _timeoutCacheUser;

        public DbSet<Telegram.Bot.Types.Chat> Chats { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>
        /// TODO DELETE
        /// </summary>
        public DataBase(DbContextOptions options, IMemoryCache memoryCache, IOptions<DataBaseOptions> optionsModel) : base(options)
        {
            _memoryCache = memoryCache;
            _timeoutCacheUser = optionsModel.Value.GetTimeoutCacheUserOrDefault();
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            Database.EnsureCreated();
        }

        private async ValueTask<User?> GetCacheOrSendRequest(Telegram.Bot.Types.Chat chat, Func<Telegram.Bot.Types.Chat, Task<User?>> getUser)
        {
            string key = $"user_{chat.Id}";
            if (_memoryCache.TryGetValue<User>(key, out var user))
                return user!;
            user = await getUser(chat);
            if (user != null)
                _memoryCache.Set(key, user, _timeoutCacheUser);
            return user;
        }

        public ValueTask<User> CreateUser(Telegram.Bot.Types.Chat chat) => GetCacheOrSendRequest(chat, async (chat) =>
        {
            await Chats.AddAsync(chat);
            var user = new User
            {
                TgChat = chat,
                KeyPage = Pages.StartPage.Key
            };
            await Users.AddAsync(user);
            await SaveChangesAsync();
            return user;
        })!;

        public ValueTask<User?> GetUser(Telegram.Bot.Types.Chat chat) => GetCacheOrSendRequest(chat, async (chat) =>
        {
            var user = await Users.Include(x => x.TgChat).FirstOrDefaultAsync(x => x.Id == chat.Id);
            if (user == null) return null;
            if (user.TgChat.Username != chat.Username || user.TgChat.FirstName != chat.FirstName || user.TgChat.LastName != chat.LastName)
            {
                Chats.Update(chat);
                user.TgChat = chat;
                await SaveChangesAsync();
            }
            return user;
        });

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<User>()
                .HasKey(x => x.Id);
            modelBuilder.Entity<Telegram.Bot.Types.Chat>()
                .HasKey(x => x.Id);
            modelBuilder.Entity<User>()
                .HasOne(x => x.TgChat)
                .WithOne()
                .HasForeignKey<User>(x => x.Id);
        }
    }
}
