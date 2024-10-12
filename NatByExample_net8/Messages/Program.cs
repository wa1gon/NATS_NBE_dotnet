// See https://aka.ms/new-console-template for more information

using Publist_Subscribe;



var simplePubSub = new PubSubscribe();
// await simplePubSub.PubSub();
Console.WriteLine("*****************************************************************************");
Console.WriteLine("Request Reply");
Console.WriteLine("*****************************************************************************");
await RequestReply.Run();

Console.WriteLine("*****************************************************************************");
Console.WriteLine("Pub Subscribe");
Console.WriteLine("*****************************************************************************");
await simplePubSub.ReqReply();

Console.WriteLine("*****************************************************************************");
Console.WriteLine("Json Payload");
Console.WriteLine("*****************************************************************************");
await JSONPayload.Run();

// Has Runtime error
// Console.WriteLine("*****************************************************************************");
// Console.WriteLine("ProtoBuf");
// Console.WriteLine("*****************************************************************************");
// await ProtoBuf.Run();

Console.WriteLine("*****************************************************************************");
Console.WriteLine("Concurrent Message");
Console.WriteLine("*****************************************************************************");
await ConcurrentMessage.Run();

Console.WriteLine("*****************************************************************************");
Console.WriteLine("Iterating over multiple subscriptions");
Console.WriteLine("*****************************************************************************");
await MultipleSubs.Run();
