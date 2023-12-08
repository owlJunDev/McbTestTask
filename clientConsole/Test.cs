using System;
using Microsoft.AspNetCore.SignalR.Client;
using System.ComponentModel;
using System.Reflection.Metadata;



namespace ClientConsole
{
    class Programs
    {
        static HubConnection connection;

        static async Task SendTest()
        {
            connection.On<char, int, int>("GetTest", (character, xS, yS) =>
                {
                    System.Console.WriteLine($"work?");
                    System.Console.WriteLine($"{character}, {xS}, {yS}");
                });
            await connection.InvokeAsync("Test", 'K', 2, 1);
        }
        static void xMain(String[] args)
        {
            connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:8000/game")
                .Build();

            connection.StartAsync().ContinueWith(task => { }).Wait();
            SendTest();
            Console.ReadLine();
        }
    }
}