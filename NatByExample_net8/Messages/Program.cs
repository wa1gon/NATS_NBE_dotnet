// See https://aka.ms/new-console-template for more information

using Publist_Subscribe;

var i = 0;

var simplePubSub = new PubSubscribe();
// await simplePubSub.PubSub();
await RequestReplay.Run();
Console.WriteLine("*****************************************************************************");
await simplePubSub.ReqReply();
