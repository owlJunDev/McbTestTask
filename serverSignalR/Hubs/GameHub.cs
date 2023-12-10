using Microsoft.AspNetCore.SignalR;
using Server.Services;
using System.Linq;
using System;
using Server.Contexts;

namespace Server.Hubs
{
    public class GameHub : Hub
    {
        private readonly UsrService usersService;
        private readonly GameService gameService;
        public GameHub(UsrService usersServic, GameService gameServic)
        {
            this.usersService = usersServic;
            this.gameService = gameServic;
        }

        private async Task UpdateStatus()
        {
            int countPlayer = usersService.GetOnlineCount();
            if (countPlayer < 2)
            {
                await Clients.Caller.SendAsync("UpdateStatus", 0, ' ');
            }
            else
            {
                if (gameService.isPlayGame())
                {
                    await Clients.Caller.SendAsync("UpdateStatus", 2, ' ');
                    await Clients.Caller.SendAsync("UploadFields", gameService.filedsIntToString());
                }
                else
                {
                    foreach (var id in usersService.GetOnlineUsersId())
                    {
                        await Clients.Client(id).SendAsync("UpdateStatus", 1, ' ');
                    }
                }

            }
        }
        private async Task CheckGameResult(bool isAutoWin, String idAutowin = "")
        {
            String result;
            if (isAutoWin)
            {
                await Clients.Client(idAutowin).SendAsync("CheckGameResult", "autoWin");
                result = "autoWin";
            }
            else
            {
                result = gameService.GetResult();
                if (result == "next")
                {
                    return;
                }
                switch (result)
                {
                    case "win_x":
                        await Clients.Client(gameService.getPlayerX()).SendAsync("CheckGameResult", "win");
                        await Clients.Client(gameService.getPlayerO()).SendAsync("CheckGameResult", "lose"); break;
                    case "win_o":
                        await Clients.Client(gameService.getPlayerX()).SendAsync("CheckGameResult", "lose");
                        await Clients.Client(gameService.getPlayerO()).SendAsync("CheckGameResult", "win"); break;
                    case "draw":
                        await Clients.Client(gameService.getPlayerX()).SendAsync("CheckGameResult", "draw");
                        await Clients.Client(gameService.getPlayerO()).SendAsync("CheckGameResult", "draw"); break;
                }
            }
            String playerX = usersService.GetUserByConnectionId(gameService.getPlayerX()),
                    playerO = usersService.GetUserByConnectionId(gameService.getPlayerO());
            ResultGameList resultGameList = new ResultGameList
            {
                playerNameX = (String.IsNullOrEmpty(playerX) ? "anonim" : playerX) + " (X)",
                playerNameO = (String.IsNullOrEmpty(playerO) ? "anonim" : playerO) + " (O)",
                resultGame = result
            };
            using (ApplicationContext db = new ApplicationContext())
            {
                db.ResultGameLists.Add(resultGameList);
                db.SaveChanges();
            }
            gameService.setDefaultValue();
            await UpdateStatus();
            await ListLastGameResult();
        }
        public async Task ListLastGameResult()
        {
            using (ApplicationContext db = new ApplicationContext())
            {
                String sendStrList = "";
                var resultGameList = db.ResultGameLists.ToList();
                foreach (ResultGameList r in resultGameList)
                {
                    sendStrList += $"{r.playerNameX} | {r.resultGame} | {r.playerNameO} *";
                }
                await Clients.All.SendAsync("ListLastGameResult", sendStrList);
            }
        }
        public async Task SendMove(char character, int x, int y)
        {
            gameService.setValueCells((character == 'X' ? 1 : -1), (x == 0 ? x : x / 2), y);
            CheckGameResult(false);
            await Clients.Others.SendAsync("GetPlayerMove", character, x, y);
        }
        public async Task SendRequestForGame()
        {
            if (String.IsNullOrEmpty(gameService.getPlayerX()))
            {
                gameService.setPlayerX(Context.ConnectionId);
                await Clients.Caller.SendAsync("UpdateStatus", 3, 'X');
                return;
            }
            if (String.IsNullOrEmpty(gameService.getPlayerO()))
            {
                gameService.setIsPlay(true);
                gameService.setPlayerO(Context.ConnectionId);
                await Clients.Caller.SendAsync("UpdateStatus", 3, 'O');
                foreach (var id in usersService.GetOnlineUsersId())
                    if (!gameService.isIdContains(id))
                        await Clients.Client(id).SendAsync("UpdateStatus", 2, ' ');
            }
        }
        public async Task AddUserToList(String userName)
        {
            Console.WriteLine($"{DateTime.Now.ToString("dd-MM-yyyy HH-mm")}: user from id {Context.ConnectionId} add myself name = {userName}");
            usersService.AddUserToList(userName, Context.ConnectionId);
            await UpdateStatus();
        }
        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"{DateTime.Now.ToString("dd-MM-yyyy HH-mm")}: id the user connected = {Context.ConnectionId}");
            await Groups.AddToGroupAsync(Context.ConnectionId, "players");
            await Clients.Caller.SendAsync("UserConnected");
        }
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            Console.WriteLine($"{DateTime.Now.ToString("dd-MM-yyyy HH-mm")}: id the user disconnected = {Context.ConnectionId}");
            if (usersService.GetUserByConnectionId(Context.ConnectionId) != null)
            {
                usersService.RemoveUserFromList(usersService.GetUserByConnectionId(Context.ConnectionId));
                if (gameService.isPlayGame() && gameService.isIdContains(Context.ConnectionId))
                {
                    await CheckGameResult(true, (gameService.getPlayerX() == Context.ConnectionId ?
                                            gameService.getPlayerO() : gameService.getPlayerX())
                                    );
                }
            }
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "players");
            await base.OnDisconnectedAsync(exception);
        }
    }
}