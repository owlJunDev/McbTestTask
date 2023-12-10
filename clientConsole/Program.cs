using System;
using Microsoft.AspNetCore.SignalR.Client;
using System.ComponentModel;
using System.Reflection.Metadata;



namespace ClientConsole
{
    class Program
    {
        static int x, y, gameStatus;
        static char s;
        static bool isSendMove;
        static bool[,] isEmptyCells;
        static HubConnection connection;

        static readonly String[] gameStatusText = { "waiting for player          ",
                                                    "press key R for connect game",
                                                    "you spectate                ",
                                                    "play                        "};
        static void drawField()
        {
            Console.Clear();
            Console.WriteLine("_|_|_\t\tpress Esc from leave");
            Console.WriteLine("_|_|_");
            Console.WriteLine(" | | ");
        }
        static async Task ListLastGameResult()
        {
            connection.On<String>("ListLastGameResult", (filed) =>
                {
                    int i = 0;
                    foreach (var str in filed.Split('*'))
                    {
                        Console.SetCursorPosition(10, 1 + i++);
                        System.Console.Write(str);
                    }
                });
        }
        static void Init()
        {
            x = 0;
            y = 0;
            isSendMove = false;
            isEmptyCells = new bool[3, 3];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    isEmptyCells[i, j] = true;
            drawField();
        }
        static async Task UploadFields()
        {
            connection.On<String>("UploadFields", (filed) =>
                {
                    Console.SetCursorPosition(0, 0);
                    foreach (var str in filed.Split('*'))
                    {
                        System.Console.WriteLine(str);
                    }
                });
        }
        static async Task UpDataStatus()
        {
            connection.On<int, Char>("UpDataStatus", (status, character) =>
                {
                    gameStatus = status;
                    s = character;
                    Console.SetCursorPosition(0, 4);
                    Console.WriteLine(gameStatusText[gameStatus]);
                    if (gameStatus > 1)
                    {
                        Init();
                        if (gameStatus == 3)
                        {
                            if (s == 'X')
                                isSendMove = true;
                            else
                                isSendMove = false;
                            Console.SetCursorPosition(x, y);
                        }
                    }
                });
        }
        static async Task GetPlayerMove()
        {
            connection.On<char, int, int>("GetPlayerMove", (character, xS, yS) =>
                {
                    isEmptyCells[(xS == 0 ? xS : xS / 2), yS] = false;
                    Console.SetCursorPosition(xS, yS);
                    Console.Write(character);
                    if (gameStatus == 3)
                    {
                        isSendMove = true;
                        Console.SetCursorPosition(x, y);
                    }
                    if (gameStatus == 2)
                        Console.SetCursorPosition(0, 4);

                });
        }
        static async Task CheckGameResult()
        {
            connection.On<String>("CheckGameResult", (result) =>
                {
                    Console.SetCursorPosition(0, 5);
                    Console.WriteLine(result);
                    isSendMove = false;
                });
        }
        static async Task SendName(String username)
        {
            await connection.InvokeAsync("AddUserToList", username);
        }
        static async Task SendRequestForGame()
        {
            await connection.InvokeAsync("SendRequestForGame");
        }
        static async Task SendMove()
        {
            isEmptyCells[(x == 0 ? x : x / 2), y] = false;
            Console.Write(s);
            await connection.InvokeAsync("SendMove", s, x, y);
        }
        static void Main(String[] args)
        {
            connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:8000/game")
                .Build();

            connection.StartAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                    Console.WriteLine("Connection faild");
                else
                    Console.WriteLine("Connected");
            }).Wait();


            UploadFields();
            UpDataStatus();
            Console.Write("Enter you username: ");
            SendName(Console.ReadLine());

            GetPlayerMove();
            CheckGameResult();
            ListLastGameResult();

            ConsoleKey key = ConsoleKey.None;

            do
            {
                if (key != ConsoleKey.None)
                {
                    switch (key)
                    {
                        case ConsoleKey.LeftArrow:
                            x = (x == 0 ? x : x - 2);
                            break;
                        case ConsoleKey.RightArrow:
                            x = (x == 4 ? x : x + 2);
                            break;
                        case ConsoleKey.UpArrow:
                            y = (y == 0 ? y : y - 1);
                            break;
                        case ConsoleKey.DownArrow:
                            y = (y == 2 ? y : y + 1);
                            break;
                        case ConsoleKey.Spacebar:
                            if (isSendMove && isEmptyCells[(x == 0 ? x : x / 2), y])
                            {
                                isSendMove = false;
                                SendMove();
                            }
                            break;
                        case ConsoleKey.R:
                            if (gameStatus == 1)
                                SendRequestForGame();
                            break;
                    }
                    if (gameStatus == 3)
                        Console.SetCursorPosition(x, y);
                    key = ConsoleKey.None;
                }

                if (Console.KeyAvailable)
                    key = Console.ReadKey(true).Key;

            } while (key != ConsoleKey.Escape);
        }
    }
}