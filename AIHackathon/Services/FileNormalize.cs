using AIHackathon.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Xml;

namespace AIHackathon.Services
{
    [Service(ServiceType.Hosted)]
    public class FileNormalize(FilesStorage filesStorage, IOptions<Settings> options) : IHostedService
    {
        private readonly static JsonSerializer _jsonSerializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            Formatting = Newtonsoft.Json.Formatting.None
        });

        public async ValueTask<string> NormalizeFile(Stream stream, string? type)
        {
            var fileName = Path.GetRandomFileName();
            var path = Path.Combine(options.Value.PathNormalizeFiles, fileName);
            using var newFile = await filesStorage.CreateFile(path);

            switch (type?.ToLower())
            {
                case "json":
                    NormalizeJson(stream, newFile);
                    break;
                case "xml":
                case "xaml":
                    NormalizeXamlStream(stream, newFile);
                    break;
                default:
                    await stream.CopyToAsync(newFile);
                    break;
            }
            return path;
        }

        private static void NormalizeJson(Stream inputFile, Stream outputFile)
        {
            using var reader = new StreamReader(inputFile);
            using var jsonReader = new JsonTextReader(reader);

            var deserializedJson = _jsonSerializer.Deserialize(jsonReader);

            var sortedJson = SortJson(deserializedJson);

            using var writer = new StreamWriter(outputFile);
            using var jsonWriter = new JsonTextWriter(writer);
            _jsonSerializer.Serialize(jsonWriter, sortedJson);
        }
        private static object? SortJson(object? json)
        {
            if (json is JObject jObject)
            {
                var sorted = new SortedDictionary<string, object?>();
                foreach (var property in jObject)
                {
                    sorted[property.Key] = SortJson(property.Value);
                }
                return sorted;
            }
            else if (json is JArray jArray)
            {
                var sortedArray = new List<object?>();
                foreach (var item in jArray)
                {
                    sortedArray.Add(SortJson(item));
                }
                return sortedArray;
            }

            return json;
        }


        public static void NormalizeXamlStream(Stream inputStream, Stream outputStream)
        {
            // Загружаем исходный XAML из потока
            var doc = new XmlDocument();
            doc.Load(inputStream);

            // Сортировка элементов по имени тега и их атрибутов
            if (doc.DocumentElement != null)
                SortXmlNodes(doc.DocumentElement);

            // Настройки для записи XAML без пробелов и с отступами
            var settings = new XmlWriterSettings
            {
                Indent = false,          // Отключаем отступы
                NewLineOnAttributes = false, // Убираем переносы строк для атрибутов
                OmitXmlDeclaration = true // Убираем декларацию XML
            };

            // Записываем отсортированный XAML в выходной поток
            using var writer = XmlWriter.Create(outputStream, settings);
            doc.Save(writer);
        }
        private static void SortXmlNodes(XmlNode node)
        {
            // Если у текущего узла есть дочерние элементы
            if (node.HasChildNodes)
            {
                var elements = node.ChildNodes.Cast<XmlNode>()
                    .Where(n => n.NodeType == XmlNodeType.Element)
                    .OrderBy(n => n.Name) // Сортировка по имени тега
                    .ToList();

                // Удаляем все дочерние элементы
                foreach (var child in node.ChildNodes.Cast<XmlNode>().ToList())
                {
                    node.RemoveChild(child);
                }

                // Добавляем отсортированные элементы обратно
                foreach (var element in elements)
                {
                    node.AppendChild(element);
                    // Рекурсивно сортируем дочерние элементы
                    SortXmlNodes(element);
                }
            }

            // Сортируем атрибуты для текущего узла (если они есть)
            if (node.Attributes != null)
            {
                var sortedAttributes = node.Attributes.Cast<XmlAttribute>()
                    .OrderBy(a => a.Name) // Сортировка атрибутов по имени
                    .ToList();

                // Удаляем старые атрибуты
                foreach (var attribute in sortedAttributes)
                {
                    node.Attributes.Remove(attribute);
                }

                // Добавляем атрибуты в отсортированном порядке
                foreach (var attribute in sortedAttributes)
                {
                    node.Attributes.Append(attribute);
                }
            }
        }

        public Task StartAsync(CancellationToken cancellationToken) => filesStorage.ClearFolder(options.Value.PathNormalizeFiles).AsTask();
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
