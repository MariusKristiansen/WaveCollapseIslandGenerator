// See https://aka.ms/new-console-template for more information
using IslandGenerator;


var k = new Generator();
k.Generate();

Console.Write(k);
Console.WriteLine(k.GetLayout());