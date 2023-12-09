using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR;
using Server.Services;
using System.Threading.Tasks;

namespace Server.Hubs
{
    public class GameHub : Hub
    {
        private readonly GameServices gameServices;
        public GameHub(GameServices gameServices)
        {
            this.gameServices = gameServices;
        }

        public static String[] playerConnectId = { "", "" };
        public static bool isPlayGame = false;
        public static int[,] cellsVal;
        public async Task Test(char character, int x, int y)
        {
            System.Console.WriteLine("test");
            System.Console.WriteLine($"{character}, {x}, {y}");

            await Clients.Caller.SendAsync("GetTest", character, x, y);
        }


        private async Task SendStatus()
        {
            int countPlayer = gameServices.GetOnlineCount();
            if (countPlayer < 2)
            {
                await Clients.Caller.SendAsync("GetStatus", 0, ' ');
                Console.WriteLine($"{DateTime.Now.ToString("dd-MM-yyyy HH-mm")}: id =  {Context.ConnectionId} first player");
            }
            else
            {
                if (isPlayGame)
                {
                    Console.WriteLine($"{DateTime.Now.ToString("dd-MM-yyyy HH-mm")}: id =  {Context.ConnectionId} spectate");
                    await Clients.Caller.SendAsync("GetStatus", 2, ' ');
                    await Clients.Caller.SendAsync("GetField", "XXXOOOXXX");
                }
                else
                {
                    foreach (var id in gameServices.GetOnlineUsersId())
                    {
                        Console.WriteLine($"{DateTime.Now.ToString("dd-MM-yyyy HH-mm")}: id =  {id} waiting");
                        await Clients.Client(id).SendAsync("GetStatus", 1, ' ');
                    }
                }

            }
        }

        private async Task GetResult(bool isAutoWin)
        {
            if (isAutoWin)
            {
                await Clients.Client(playerConnectId[0]).SendAsync("GetResult", true);
            }
            else
            {
                int countPointRow, countPointCol, countPointHor = 0, countPointHorRev = 0;
                bool isWin = false;
                for (int i = 0; i < 3; i++)
                {
                    countPointRow = 0;
                    countPointCol = 0;
                    for (int j = 0; j < 3; j++)
                    {
                        countPointRow += cellsVal[i, j];
                        countPointCol += cellsVal[j, i];
                    }
                    countPointHor += cellsVal[i, i];
                    countPointHorRev += cellsVal[i, 2 - i];

                    if (countPointRow == 3 || countPointCol == 3 || countPointHor == 3 || countPointHorRev == 3)
                    {
                        await Clients.Client(playerConnectId[0]).SendAsync("GetResult", true);
                        await Clients.Client(playerConnectId[1]).SendAsync("GetResult", false);
                        isWin = true;
                        break;
                    }
                    if (countPointRow == -3 || countPointCol == -3 || countPointHor == -3 || countPointHorRev == -3)
                    {
                        await Clients.Client(playerConnectId[0]).SendAsync("GetResult", false);
                        await Clients.Client(playerConnectId[1]).SendAsync("GetResult", true);
                        isWin = true;
                        break;
                    }
                }
                if (!isWin)
                    return;
            }
            playerConnectId[0] = "";
            playerConnectId[1] = "";
            isPlayGame = false;
            await SendStatus();
        }
        public async Task SendMove(char character, int x, int y)
        {
            cellsVal[(x == 0 ? x : x / 2), y] = character == 'X' ? 1 : -1;
            Console.WriteLine($"{DateTime.Now.ToString("dd-MM-yyyy HH-mm")}: id =  {Context.ConnectionId} move");
            Console.WriteLine("\tcheck: " + Context.ConnectionId);
            GetResult(false);
            await Clients.Others.SendAsync("GetPlayerMove", character, x, y);
        }

        public async Task SendRequestForGame()
        {
            Console.WriteLine($"{DateTime.Now.ToString("dd-MM-yyyy HH-mm")}: id =  {Context.ConnectionId} connect for play");

            if (String.IsNullOrEmpty(playerConnectId[0]))
            {
                playerConnectId[0] = Context.ConnectionId;
                await Clients.Caller.SendAsync("GetStatus", 3, 'X');
                return;
            }
            if (String.IsNullOrEmpty(playerConnectId[1]))
            {
                cellsVal = new int[3, 3];
                for (int i = 0; i < 3; i++)
                    for (int j = 0; j < 3; j++)
                        cellsVal[i, j] = 0;

                isPlayGame = true;
                playerConnectId[1] = Context.ConnectionId;
                await Clients.Caller.SendAsync("GetStatus", 3, 'O');
                await Clients.AllExcept(playerConnectId).SendAsync("GetStatus", 2, ' ');
            }
        }

        public async Task AddUserToList(String userName)
        {
            Console.WriteLine($"{DateTime.Now.ToString("dd-MM-yyyy HH-mm")}: user from id {Context.ConnectionId} add myself name = {userName}");
            gameServices.AddUserToList(userName, Context.ConnectionId);
            await SendStatus();
        }
        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"{DateTime.Now.ToString("dd-MM-yyyy HH-mm")}: id the user connected = {Context.ConnectionId}");
            await Groups.AddToGroupAsync(Context.ConnectionId, "TicTacToe");
            await Clients.Caller.SendAsync("UserConnected");
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            Console.WriteLine($"{DateTime.Now.ToString("dd-MM-yyyy HH-mm")}: id the user disconnected = {Context.ConnectionId}");
            if (gameServices.GetUserByConnectionId(Context.ConnectionId) != null)
                gameServices.RemoveUserFromList(gameServices.GetUserByConnectionId(Context.ConnectionId));

            if (playerConnectId.Contains(Context.ConnectionId))
            {
                playerConnectId[0] = playerConnectId[0] == Context.ConnectionId ? playerConnectId[1] : playerConnectId[0];
                playerConnectId[1] = "";
                GetResult(true);
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "TicTacToe");
            await base.OnDisconnectedAsync(exception);
        }
    }
}