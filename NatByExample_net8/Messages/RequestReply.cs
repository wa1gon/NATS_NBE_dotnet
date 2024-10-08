namespace Publist_Subscribe;

public static class RequestReply
{
    public static async Task Run()
    {
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

        Log("[REQ] From sue");
        reply = await nats.RequestAsync<int, string>("greet.sue", 0, replyOpts: replyOpts);
        Log($"[REQ] {reply.Data}");

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
        catch (NatsNoRespondersException)
        {
            Log("[REQ] timed out!");
        }

// That's it! We saw how we can create a responder and request data from it. We also set
// request timeouts to make sure we can move on when there is no response to our requests.
        Log("Bye!");

        return;

        void Log(string log) => Console.WriteLine($"{stopwatch.Elapsed} {log}");
    }
}
