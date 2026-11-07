using UnityEngine;

namespace MazeGame
{
    public enum CellType
    {
        Wall,
        Floor,
        Start,
        Exit,
        Coin
    }

    public struct CellPosition
    {
        public int x;
        public int y;

        public CellPosition(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public override bool Equals(object obj)
        {
            if (obj is CellPosition other)
                return x == other.x && y == other.y;
            return false;
        }

        public override int GetHashCode()
        {
            return (x * 397) ^ y;
        }
    }
}
