using AIHackathon.DB.Models;
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
        public DbSet<Command> Commands { get; set; } = null!;
        public DbSet<Participant> Participants { get; set; } = null!;
        public DbSet<MetricParticipant> Metrics { get; set; } = null!;

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
            var user = await Users.Include(x => x.TgChat).Include(x => x.Participant).ThenInclude(x => x!.Command).FirstOrDefaultAsync(x => x.Id == chat.Id);
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
            modelBuilder.Entity<Telegram.Bot.Types.Chat>()
                .HasKey(x => x.Id);
            modelBuilder.Entity<User>()
                .HasOne(x => x.TgChat)
                .WithOne()
                .HasForeignKey<User>(x => x.Id);
            modelBuilder.Entity<RatingInfo<Command>>().HasNoKey();
            modelBuilder.Entity<RatingInfo<Participant>>().HasNoKey();
        }

        private IQueryable<RatingInfo<T>> GetQueryRating<T>(string particantGetId) where T : class, new()
        {
            var query = $@"
SELECT 
    p.{particantGetId} AS {nameof(RatingInfo<object>.SubjectId)},
    COUNT(*) AS {nameof(RatingInfo<object>.CountMetric)},
    MAX(m.{nameof(MetricParticipant.Accuracy)}) AS {nameof(RatingInfo<object>.Metric)},
    RANK() OVER (ORDER BY MAX(m.{nameof(MetricParticipant.Accuracy)}) DESC) AS {nameof(RatingInfo<object>.Rating)},
    ROW_NUMBER() OVER (ORDER BY MAX(m.{nameof(MetricParticipant.Accuracy)}) DESC) AS {nameof(RatingInfo<object>.Position)}
FROM {nameof(Participants)} p
JOIN {nameof(Metrics)} m ON p.{nameof(Participant.Id)} = m.{nameof(MetricParticipant.ParticipantId)}
WHERE (m.{nameof(MetricParticipant.Error)} IS NULL OR m.{nameof(MetricParticipant.Error)} = '')
GROUP BY p.{particantGetId}
ORDER BY MAX(m.{nameof(MetricParticipant.Accuracy)}) DESC
";
            return Set<RatingInfo<T>>().FromSqlRaw(query).Include(x => x.Subject);
        }

        public IQueryable<RatingInfo<Command>> GetCommandsRating() => GetQueryRating<Command>(nameof(Participant.CommandId));
        public IQueryable<RatingInfo<Participant>> GetParticipantsRating() => GetQueryRating<Participant>(nameof(Participant.Id));
    }
}
