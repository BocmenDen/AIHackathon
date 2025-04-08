using AIHackathon.DB.Models;
using AIHackathon.Utils;
using BotCore.Services;
using Newtonsoft.Json;

namespace AIHackathon.DB
{
    [Service(ServiceType.Singleton, typeof(IDBUserPageKey), typeof(IDBUserPageModel), typeof(IDBUserPageParameter))]
    internal class DBStorageUtil(ConditionalPooledObjectProvider<DataBase> db) :
        IDBUserPageKey,
        IDBUserPageModel,
        IDBUserPageParameter
    {
        private readonly ConditionalPooledObjectProvider<DataBase> _db = db;

        string? IDBUserPageKey.GetKeyPage(User user) => user.KeyPage;

        Task IDBUserPageKey.SetKeyPage(User user, string? key)
        {
            return _db.TakeObject(async (db) =>
            {
                user.KeyPage = key;
                db.Users.Update(user);
                await db.SaveChangesAsync();
                db.ChangeTracker.Clear();
            });
        }

        BotCore.PageRouter.Models.StorageModel<T> IDBUserPageModel.GetModel<T>(User user)
        {
            var model = user.ModelPage == null ? new T() : JsonConvert.DeserializeObject<T>(user.ModelPage) ?? new T();
            return new BotCore.PageRouter.Models.StorageModel<T>(model, (value) => _db.TakeObject(async (db) =>
            {
                user.ModelPage = JsonConvert.SerializeObject(value);
                db.Users.Update(user);
                await db.SaveChangesAsync();
                db.ChangeTracker.Clear();
            }));
        }

        object? IDBUserPageParameter.GetParameter(User user)
        {
            return user.ParameterPage == null ? null : JsonHelper.DeserializeWithType(user.ParameterPage);
        }

        Task IDBUserPageParameter.SetParameter(User user, object? parameter)
        {
            return _db.TakeObject(async (db) =>
            {
                user.ParameterPage = parameter == null ? null : JsonHelper.SerializeWithType(parameter);
                db.Users.Update(user);
                await db.SaveChangesAsync();
                db.ChangeTracker.Clear();
            });
        }
    }
}
