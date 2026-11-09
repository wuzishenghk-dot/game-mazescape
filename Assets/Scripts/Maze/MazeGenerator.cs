using System.Collections.Generic;
using UnityEngine;

namespace MazeGame
{
    public class MazeGenerator
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public CellType[,] Grid { get; private set; }
        public Vector2Int ChallengeCenter { get; private set; }
        public CellPosition StartPos { get; private set; }
        public CellPosition ExitPos { get; private set; }
        public List<CellPosition> CoinPositions { get; private set; }

        public MazeGenerator(int width, int height)
        {
            // Ensure dimensions are odd numbers >= 9
            Width = Mathf.Max(9, width % 2 == 0 ? width + 1 : width);
            Height = Mathf.Max(9, height % 2 == 0 ? height + 1 : height);
            Grid = new CellType[Width, Height];
            CoinPositions = new List<CellPosition>();
        }

        public void Generate()
        {
            // Initialize entire grid with walls
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Grid[x, y] = CellType.Wall;
                }
            }

            StartPos = new CellPosition(1, 1);
            ExitPos = new CellPosition(Width - 2, Height - 2);

            Stack<CellPosition> stack = new Stack<CellPosition>();
            List<CellPosition> deadEnds = new List<CellPosition>();

            Grid[StartPos.x, StartPos.y] = CellType.Floor;
            stack.Push(StartPos);

            int[] dx = { 0, 0, 2, -2 };
            int[] dy = { 2, -2, 0, 0 };

            while (stack.Count > 0)
            {
                CellPosition current = stack.Peek();
                List<int> validDirections = new List<int>();

                for (int i = 0; i < 4; i++)
                {
                    int nx = current.x + dx[i];
                    int ny = current.y + dy[i];

                    if (nx > 0 && nx < Width - 1 && ny > 0 && ny < Height - 1)
                    {
                        if (Grid[nx, ny] == CellType.Wall)
                        {
                            validDirections.Add(i);
                        }
                    }
                }

                if (validDirections.Count > 0)
                {
                    int chosenDir = validDirections[Random.Range(0, validDirections.Count)];
                    int nx = current.x + dx[chosenDir];
                    int ny = current.y + dy[chosenDir];
                    int wallX = current.x + dx[chosenDir] / 2;
                    int wallY = current.y + dy[chosenDir] / 2;

                    Grid[wallX, wallY] = CellType.Floor;
                    Grid[nx, ny] = CellType.Floor;

                    stack.Push(new CellPosition(nx, ny));
                }
                else
                {
                    CellPosition popped = stack.Pop();
                    // Check if it's a dead end (only 1 open neighbor)
                    if (CountFloorNeighbors(popped.x, popped.y) == 1)
                    {
                        if (!(popped.x == StartPos.x && popped.y == StartPos.y) &&
                            !(popped.x == ExitPos.x && popped.y == ExitPos.y))
                        {
                            deadEnds.Add(popped);
                        }
                    }
                }
            }

            // Small connected chambers and shortcuts keep exploration from becoming all backtracking.
            ChallengeCenter = new Vector2Int(Width - 4, 3);
            var rooms = new[] { ChallengeCenter, new Vector2Int(3, Height - 4) };
            foreach(var room in rooms)
                for(int x=room.x-1;x<=room.x+1;x++)for(int y=room.y-1;y<=room.y+1;y++)Grid[x,y]=CellType.Floor;
            int openings=0;
            for(int attempt=0;attempt<Width*Height && openings<3;attempt++)
            {
                int x=Random.Range(2,Width-2),y=Random.Range(2,Height-2);
                if(Grid[x,y]!=CellType.Wall)continue;
                bool horizontal=Grid[x-1,y]!=CellType.Wall&&Grid[x+1,y]!=CellType.Wall;
                bool vertical=Grid[x,y-1]!=CellType.Wall&&Grid[x,y+1]!=CellType.Wall;
                if(horizontal||vertical){Grid[x,y]=CellType.Floor;openings++;}
            }
            deadEnds.Clear();
            for(int x=1;x<Width-1;x++)for(int y=1;y<Height-1;y++)
                if(Grid[x,y]!=CellType.Wall&&CountFloorNeighbors(x,y)==1&&!(x==1&&y==1)&&!(x==ExitPos.x&&y==ExitPos.y)&&!(x==ChallengeCenter.x&&y==ChallengeCenter.y))deadEnds.Add(new CellPosition(x,y));
            // Ensure exit is Floor
            Grid[ExitPos.x, ExitPos.y] = CellType.Exit;
            Grid[StartPos.x, StartPos.y] = CellType.Start;

            // Pick coin positions from dead ends or random open floor cells
            CoinPositions.Clear();
            int coinCount = Mathf.Clamp(deadEnds.Count, 3, 10);
            
            if(deadEnds.Count<6)
                for(int x=1;x<Width-1&&deadEnds.Count<6;x++)for(int y=1;y<Height-1&&deadEnds.Count<6;y++)
                {
                    var cell=new CellPosition(x,y);
                    if(Grid[x,y]==CellType.Floor && !(x==1&&y==1) && !(x==ExitPos.x&&y==ExitPos.y) && !(x==ChallengeCenter.x&&y==ChallengeCenter.y) && !deadEnds.Contains(cell))deadEnds.Add(cell);
                }
            coinCount=Mathf.Min(10,deadEnds.Count);
            // Shuffle dead ends
            for (int i = 0; i < deadEnds.Count; i++)
            {
                int r = Random.Range(i, deadEnds.Count);
                CellPosition temp = deadEnds[i];
                deadEnds[i] = deadEnds[r];
                deadEnds[r] = temp;
            }

            for (int i = 0; i < deadEnds.Count && i < coinCount; i++)
            {
                CoinPositions.Add(deadEnds[i]);
                Grid[deadEnds[i].x, deadEnds[i].y] = CellType.Coin;
            }
        }

        private int CountFloorNeighbors(int x, int y)
        {
            int count = 0;
            if (x > 0 && Grid[x - 1, y] != CellType.Wall) count++;
            if (x < Width - 1 && Grid[x + 1, y] != CellType.Wall) count++;
            if (y > 0 && Grid[x, y - 1] != CellType.Wall) count++;
            if (y < Height - 1 && Grid[x, y + 1] != CellType.Wall) count++;
            return count;
        }
    }
}
