using System;

namespace TRON.Avalonia{
    public class MAP{
        //Tile size in pixels
        public const int TILE_SIZE = 8;
        //Tilemap size
        public const int MAP_WIDTH = 125;
        public const int MAP_HEIGHT = 90;

        public class Tile{
            public int X {get; set;}
            public int Y {get; set;}
            public Tile? Top {get; set;}
            public Tile? Right {get; set;}
            public Tile? Bottom {get; set;}
            public Tile? Left {get; set;}

            public Tile(int x, int y)
            {
                X = x;
                Y = y;
                Top = null;
                Right = null;
                Bottom = null;
                Left = null;
            }
        }

        public class Tilemap{
            private Tile[,] _tiles;
            public int Width {get; set;}
            public int Height {get; set;}

            public Tilemap(int width, int height)
            {
                Width = width;
                Height = height;
                _tiles = new Tile[width, height];

                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        _tiles[x, y] = new Tile(x, y);
                    }
                }

                InitializeTileNeighbors();
            }

            private void InitializeTileNeighbors()
            {
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        Tile tile = _tiles[x, y];

                        if (x > 0) tile.Left = _tiles[--x, y];
                        if (x < Width - 1) tile.Right = _tiles[++x, y];
                        if (y > 0) tile.Top = _tiles[x, --y];
                        if (y < Height - 1) tile.Bottom = _tiles[x, ++y];
                    }
                }
            }

            public Tile GetTile(int x, int y)
            {
                if (x < 0 || x >= Width || y < 0 || y >= Height)
                {
                    throw new IndexOutOfRangeException("Tile coordinates out of range");
                }
                return _tiles[x, y];
            }
        }
    }
}