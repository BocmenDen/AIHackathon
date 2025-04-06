using System.Text.Json;

namespace AIHackathon.Utils
{
    public static class JsonHelper
    {
        public static string SerializeWithType(object obj)
        {
            var wrapper = new JsonWrapper
            {
                Type = obj.GetType().AssemblyQualifiedName!,
                Data = JsonSerializer.Serialize(obj)
            };
            return JsonSerializer.Serialize(wrapper);
        }
        public static object DeserializeWithType(string json)
        {
            var wrapper = JsonSerializer.Deserialize<JsonWrapper>(json);
            var type = Type.GetType(wrapper!.Type);

            return JsonSerializer.Deserialize(wrapper.Data, type!)!;
        }
        public static T DeserializeWithType<T>(string json)
        {
            return (T)DeserializeWithType(json);
        }

        public class JsonWrapper
        {
            public required string Type { get; init; }
            public required string Data { get; init; }
        }
    }
}
