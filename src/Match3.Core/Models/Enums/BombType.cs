namespace Match3.Core.Models.Enums
{
    public enum BombType
    {
        None = 0,
        
        /// <summary>
        /// Explodes in a circular area (e.g., radius 1 = 3x3).
        /// </summary>
        Area = 1,
        
        /// <summary>
        /// Clears the entire row.
        /// </summary>
        Horizontal = 2,
        
        /// <summary>
        /// Clears the entire column.
        /// </summary>
        Vertical = 3,
        
        /// <summary>
        /// Clears all tiles of a specific color.
        /// </summary>
        Color = 4,

        /// <summary>
        /// Homing missile that targets a specific tile.
        /// </summary>
        Ufo = 5,

        /// <summary>
        /// Explodes a 5x5 square area (Radius 2).
        /// </summary>
        Square5x5 = 6
    }

    public static class BombTypeExtensions
    {
        public static bool IsRocket(this BombType type)
        {
            return type == BombType.Horizontal || type == BombType.Vertical;
        }

        public static bool IsAreaBomb(this BombType type)
        {
            return type == BombType.Area || type == BombType.Square5x5;
        }

        public static bool IsUfo(this BombType type)
        {
            return type == BombType.Ufo;
        }

        public static bool IsRainbow(this BombType type)
        {
            return type == BombType.Color;
        }
    }
}
