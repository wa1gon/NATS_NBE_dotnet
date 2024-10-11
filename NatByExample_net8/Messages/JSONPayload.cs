using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Publist_Subscribe;

namespace Publist_Subscribe
{
    public static class JSONPayload
    {
        public static async Task Run()
        {
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger("NATS-by-Example");

            var url = Environment.GetEnvironmentVariable("NATS_URL") ?? "127.0.0.1:4222";

            var opts = new NatsOpts
            {
                Url = url,
                LoggerFactory = loggerFactory,
                Name = "NATS-by-Example",
            };
            await using var nats = new NatsConnection(opts);

            await nats.ConnectAsync();

            // Use the generated JSON serializer to deserialize the JSON payload
            var mySerializer = new NatsJsonContextSerializer<MyData>(MyJsonContext.Default);

            var subIterator1 = await nats.SubscribeCoreAsync<MyData>("data", serializer: mySerializer);

            var subTask1 = Task.Run(async () =>
            {
                logger.LogInformation("Waiting for messages...");
                await foreach (var msg in subIterator1.Msgs.ReadAllAsync())
                {
                    if (msg.Data is null)
                    {
                        logger.LogInformation("Received empty payload: End of messages");
                        break;
                    }

                    var data = msg.Data;
                    logger.LogInformation("Received deserialized object {Data}", data);
                }
            });

            var subIterator2 = await nats.SubscribeCoreAsync<NatsMemoryOwner<byte>>("data");

            var subTask2 = Task.Run(async () =>
            {
                logger.LogInformation("Waiting for messages...");
                await foreach (var msg in subIterator2.Msgs.ReadAllAsync())
                {
                    using var memoryOwner = msg.Data;

                    if (memoryOwner.Length == 0)
                    {
                        logger.LogInformation("Received empty payload: End of messages");
                        break;
                    }

                    var json = Encoding.UTF8.GetString(memoryOwner.Span);
                    logger.LogInformation("Received raw JSON {Json}", json);
                }
            });

            await nats.PublishAsync<MyData>(subject: "data", data: new MyData { Id = 1, Name = "Bob" }, serializer: mySerializer);
            await nats.PublishAsync<byte[]>(subject: "data", data: Encoding.UTF8.GetBytes("""{"id":2,"name":"Joe"}"""));

            var alice = """{"id":3,"name":"Alice"}""";
            var bw = new NatsBufferWriter<byte>();
            var byteCount = Encoding.UTF8.GetByteCount(alice);
            var memory = bw.GetMemory(byteCount);
            Encoding.UTF8.GetBytes(alice, memory.Span);
            bw.Advance(byteCount);
            await nats.PublishAsync<NatsBufferWriter<byte>>(subject: "data", data: bw);

            await nats.PublishAsync(subject: "data");
            await Task.WhenAll(subTask1, subTask2);
            logger.LogInformation("Bye!");
        }
    }
    [JsonSerializable(typeof(MyData))]
    internal partial class MyJsonContext : JsonSerializerContext;
    // internal class MyJsonContext : JsonSerializerContext
    // {
    //     public static MyJsonContext Default { get; } = new MyJsonContext(new JsonSerializerOptions());
    //
    //     public MyJsonContext(JsonSerializerOptions options) : base(options)
    //     {
    //     }

        // public override JsonTypeInfo? GetTypeInfo(Type type)
        // {
        //     if (type == typeof(MyData))
        //     {
        //         var context = MyJsonContext.Default;
        //         return JsonTypeInfo.CreateJsonTypeInfo<MyData>(context.Options, context);
        //     }
        //     return null;
        // }

    //     protected override JsonSerializerOptions? GeneratedSerializerOptions => new JsonSerializerOptions
    //     {
    //         PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    //         WriteIndented = true
    //     };
    // }

    public record MyData
    {
        [JsonPropertyName("id")] public int Id { get; set; }

        [JsonPropertyName("name")] public string? Name { get; set; }
    }
}
