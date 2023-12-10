using Microsoft.AspNetCore.SignalR;
using Server.Services;
using System.Linq;
using System;
using Server.Contexts;

namespace Server.Hubs
{
    public class GameHub : Hub
    {
        private readonly UsrServic usersServic;
        private readonly GameServic gameServic;
        public GameHub(UsrServic usersServic, GameServic gameServic)
        {
            this.usersServic = usersServic;
            this.gameServic = gameServic;
        }

        private async Task UpDataStatus()
        {
            int countPlayer = usersServic.GetOnlineCount();
            if (countPlayer < 2)
            {
                await Clients.Caller.SendAsync("UpDataStatus", 0, ' ');
            }
            else
            {
                if (gameServic.isPlayGame())
                {
                    await Clients.Caller.SendAsync("UpDataStatus", 2, ' ');
                    await Clients.Caller.SendAsync("UploadFields", gameServic.filedsIntToString());
                }
                else
                {
                    foreach (var id in usersServic.GetOnlineUsersId())
                    {
                        await Clients.Client(id).SendAsync("UpDataStatus", 1, ' ');
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
                result = gameServic.GetResult();
                if (result == "next")
                {
                    return;
                }
                switch (result)
                {
                    case "win_x":
                        await Clients.Client(gameServic.getPlayerX()).SendAsync("CheckGameResult", "win");
                        await Clients.Client(gameServic.getPlayerO()).SendAsync("CheckGameResult", "lose"); break;
                    case "win_o":
                        await Clients.Client(gameServic.getPlayerX()).SendAsync("CheckGameResult", "lose");
                        await Clients.Client(gameServic.getPlayerO()).SendAsync("CheckGameResult", "win"); break;
                    case "draw":
                        await Clients.Client(gameServic.getPlayerX()).SendAsync("CheckGameResult", "draw");
                        await Clients.Client(gameServic.getPlayerO()).SendAsync("CheckGameResult", "draw"); break;
                }
            }
            ResulGameList resulGameList = new ResulGameList
            {
                playerNameX = usersServic.GetUserByConnectionId(gameServic.getPlayerX()),
                playerNameO = usersServic.GetUserByConnectionId(gameServic.getPlayerO()),
                resultGame = result
            };
            using (ApplicationContext db = new ApplicationContext())
            {
                db.ResulsGameList.Add(resulGameList);
                db.SaveChanges();
            }
            gameServic.setDefolatValue();
            await UpDataStatus();
            await ListLastGameResult();
        }
        public async Task ListLastGameResult()
        {
            using (ApplicationContext db = new ApplicationContext())
            {
                String sendStrList = "";
                var resulsGameList = db.ResulsGameList.ToList();
                // var resulsGameList = db.ResulsGameList.ma;
                foreach (ResulGameList r in resulsGameList)
                {
                    sendStrList += $"{r.playerNameX} | {r.resultGame} | {r.playerNameO} *";
                }
                await Clients.Client(gameServic.getPlayerX()).SendAsync("ListLastGameResult", sendStrList);

            }
        }
        public async Task SendMove(char character, int x, int y)
        {
            gameServic.setValueCells((character == 'X' ? 1 : -1), (x == 0 ? x : x / 2), y);
            CheckGameResult(false);
            await Clients.Others.SendAsync("GetPlayerMove", character, x, y);
        }
        public async Task SendRequestForGame()
        {
            if (String.IsNullOrEmpty(gameServic.getPlayerX()))
            {
                gameServic.setPlayerX(Context.ConnectionId);
                await Clients.Caller.SendAsync("UpDataStatus", 3, 'X');
                return;
            }
            if (String.IsNullOrEmpty(gameServic.getPlayerO()))
            {
                gameServic.setIsPlay(true);
                gameServic.setPlayerO(Context.ConnectionId);
                await Clients.Caller.SendAsync("UpDataStatus", 3, 'O');
                foreach (var id in usersServic.GetOnlineUsersId())
                    if (!gameServic.isIdContains(id))
                        await Clients.Client(id).SendAsync("UpDataStatus", 2, ' ');
            }
        }
        public async Task AddUserToList(String userName)
        {
            Console.WriteLine($"{DateTime.Now.ToString("dd-MM-yyyy HH-mm")}: user from id {Context.ConnectionId} add myself name = {userName}");
            usersServic.AddUserToList(userName, Context.ConnectionId);
            await UpDataStatus();
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
            if (usersServic.GetUserByConnectionId(Context.ConnectionId) != null)
            {
                usersServic.RemoveUserFromList(usersServic.GetUserByConnectionId(Context.ConnectionId));
                if (gameServic.isIdContains(Context.ConnectionId))
                {
                    await CheckGameResult(true, (gameServic.getPlayerX() == Context.ConnectionId ?
                                            gameServic.getPlayerO() : gameServic.getPlayerX())
                                    );
                }
            }
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "players");
            await base.OnDisconnectedAsync(exception);
        }
    }
}