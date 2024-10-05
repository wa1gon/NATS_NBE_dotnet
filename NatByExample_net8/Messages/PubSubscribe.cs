namespace Publist_Subscribe;

public class PubSubscribe
{
    public async Task ReqReply()
    {
        // Install `NATS.Net` from NuGet.


        var stopwatch = Stopwatch.StartNew();

// `NATS_URL` environment variable can be used to pass the locations of the NATS servers.
        var url = Environment.GetEnvironmentVariable("NATS_URL") ?? "127.0.0.1:4222";
        Log($"[CON] Connecting to {url}...");

// Connect to NATS server. Since connection is disposable at the end of our scope we should flush
// our buffers and close connection cleanly.
        var opts = NatsOpts.Default with { Url = url };
        await using var nats = new NatsConnection(opts);

// Create a message event handler and then subscribe to the target
// subject which leverages a wildcard `greet.*`.
// When a user makes a "request", the client populates
// the reply-to field and then listens (subscribes) to that
// as a subject.
// The responder simply publishes a message to that reply-to.
        await using var sub = await nats.SubscribeCoreAsync<int>("greet.*");

        var reader = sub.Msgs;
        var responder = Task.Run(async () =>
        {
            await foreach (var msg in reader.ReadAllAsync())
            {
                var name = msg.Subject.Split('.')[1];
                Log($"[REP] Received {msg.Subject}");
                await Task.Delay(500);
                await msg.ReplyAsync($"Hello {name}!");
            }
        });

// Make a request and wait a most 1 second for a response.
        var replyOpts = new NatsSubOpts { Timeout = TimeSpan.FromSeconds(2) };

        Log("[REQ] From joe");
        var reply = await nats.RequestAsync<int, string>("greet.joe", 0, replyOpts: replyOpts);
        Log($"[REQ] {reply.Data}");

        Log("----");

        Log("[REQ] From sue");
        reply = await nats.RequestAsync<int, string>("greet.sue", 0, replyOpts: replyOpts);
        Log($"[REQ] {reply.Data}");
        Log("----");

        Log("[REQ] From bob");
        reply = await nats.RequestAsync<int, string>("greet.bob", 0, replyOpts: replyOpts);
        Log($"[REQ] {reply.Data}");

// Once we unsubscribe there will be no subscriptions to reply.
        await sub.UnsubscribeAsync();

        await responder;

// Now there is no responder our request will timeout.

        try
        {
            reply = await nats.RequestAsync<int, string>("greet.joe", 0, replyOpts: replyOpts);
            Log($"[REQ] {reply.Data} - This will timeout. We should not see this message.");
        }
        catch (NatsNoRespondersException e)
            // catch (Exception e)
        {
            Log($"[REQ] timed out! Mesaage: {e.Message}");
        }

// That's it! We saw how we can create a responder and request data from it. We also set
// request timeouts to make sure we can move on when there is no response to our requests.
        Log("Bye!");

        return;

        void Log(string log) => Console.WriteLine($"{stopwatch.Elapsed} {log}");
    }

    public async Task PubSub()
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger("NATS-by-Example");

// `NATS_URL` environment variable can be used to pass the locations of the NATS servers.
        var url = Environment.GetEnvironmentVariable("NATS_URL") ?? "127.0.0.1:4222";

// Connect to NATS server. Since connection is disposable at the end of our scope we should flush
// our buffers and close connection cleanly.
        var opts = new NatsOpts
        {
            Url = url,
            LoggerFactory = loggerFactory,
            SerializerRegistry = NatsJsonSerializerRegistry.Default,
            Name = "NATS-by-Example",
        };
        await using var nats = new NatsConnection(opts);

// Subscribe to a subject and start waiting for messages in the background.
        await using var sub = await nats.SubscribeCoreAsync<Order>("orders.>");

        logger.LogInformation("Waiting for messages...");
        var task = Task.Run(async () =>
        {
            await foreach (var msg in sub.Msgs.ReadAllAsync())
            {
                var order = msg.Data;
                logger.LogInformation("Subscriber received {Subject}: {Order}", msg.Subject, order);
            }

            logger.LogInformation("Unsubscribed");
        });

// Let's publish a few orders.
        for (int i = 0; i < 5; i++)
        {
            logger.LogInformation("Publishing order {Index}...", i);
            await nats.PublishAsync($"orders.new.{i}", new Order(OrderId: i));
            await Task.Delay(500);
        }

// We can unsubscribe now all orders are published. Unsubscribing or disposing the subscription
// should complete the message loop and exit the background task cleanly.
        await sub.UnsubscribeAsync();
        await task;

// That's it! We saw how we can subscribe to a subject and publish messages that would
// be seen by the subscribers based on matching subjects.
        logger.LogInformation("Bye!");
    }

    public record Order(int OrderId);
}
