using System.Collections.Generic;

namespace Server.Services
{
    public class GameServic
    {
        private static String[] playerConnectId = { "", "" };
        private static bool isPlay = false;
        private static int[,] cellsVal;

        public GameServic()
        {
            cellsVal = new int[3, 3];
            setDefolatValue();
        }

        public bool isPlayGame() { return isPlay; }
        public void setIsPlay(bool isPlayGame)
        {
            isPlay = isPlayGame;
        }

        public void setDefolatValue()
        {
            isPlay = false;
            playerConnectId[0] = "";
            playerConnectId[1] = "";
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    cellsVal[i, j] = 0;
        }

        public bool isIdContains(String id)
        {
            return playerConnectId.Contains(id);
        }
        public String filedsIntToString()
        {
            String strFileds = "";
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    Console.Write(cellsVal[j, i]);
                    strFileds += cellsVal[j, i] == 0 ? "_|" :
                                    cellsVal[j, i] == 1 ? "X|" : "O|";
                }
                if (i < 2)
                    strFileds += "*";
                Console.Write('\n');
            }

            return strFileds;
        }


        public void setValueCells(int value, int x, int y)
        {
            cellsVal[x, y] = value;
        }

        public void setPlayerX(String userId) { playerConnectId[0] = userId; }
        public void setPlayerO(String userId) { playerConnectId[1] = userId; }

        public String getPlayerX() { return playerConnectId[0]; }
        public String getPlayerO() { return playerConnectId[1]; }

        public String[] getPlayers() { return playerConnectId; }
        public String GetResult()
        {
            int countPointRow, countPointCol, countPointHor = 0, countPointHorRev = 0;
            bool isZeroContains = false;
            for (int i = 0; i < 3; i++)
            {
                countPointRow = 0;
                countPointCol = 0;
                for (int j = 0; j < 3; j++)
                {
                    countPointRow += cellsVal[i, j];
                    countPointCol += cellsVal[j, i];
                    if (cellsVal[i, j] == 0)
                        isZeroContains = true;
                }
                countPointHor += cellsVal[i, i];
                countPointHorRev += cellsVal[i, 2 - i];

                if (countPointRow == 3 || countPointCol == 3 || countPointHor == 3 || countPointHorRev == 3)
                {
                    return "win_x";
                }
                if (countPointRow == -3 || countPointCol == -3 || countPointHor == -3 || countPointHorRev == -3)
                {
                    return "win_o";

                }
            }
            if (isZeroContains)
                return "next";
            return "draw";
        }
    }
}