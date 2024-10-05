// See https://aka.ms/new-console-template for more information

using Publist_Subscribe;

var i = 0;

var simplePubSub = new PubSubscribe();
await simplePubSub.PubSub();
Console.WriteLine("*****************************************************************************");
await simplePubSub.ReqReply();
