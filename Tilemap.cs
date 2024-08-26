using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Controls.Shapes;
using Avalonia.Media;


namespace TRON.Avalonia{
    public class MAP{

        //Tile size in pixels
        public const int TILE_SIZE = 16;
        //Tilemap size
        public const int MAP_WIDTH = 64;
        public const int MAP_HEIGHT = 44;

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

            public void Draw(Canvas? canvas, IBrush color)
            {
                var rect = new Rectangle()
                {
                    Width = TILE_SIZE,
                    Height = TILE_SIZE,
                    Fill = color, // Replace with your desired tile color
                    Stroke = Brushes.White, // Replace with your desired tile border color
                    StrokeThickness = 0.2,
                };
                Canvas.SetLeft(rect, X * TILE_SIZE);
                Canvas.SetTop(rect, Y * TILE_SIZE);
                canvas?.Children.Add(rect);
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

                        if (x > 0) tile.Left = _tiles[x-1, y];
                        if (x < Width - 1) tile.Right = _tiles[x+1, y];
                        if (y > 0) tile.Top = _tiles[x, y-1];
                        if (y < Height - 1) tile.Bottom = _tiles[x, y+1];
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

            public Tile GetCenter()
            {
                int CenterX = MAP_WIDTH/2;
                int CenterY = MAP_HEIGHT/2;
                return GetTile(CenterX, CenterY);
            }

            public void Draw(Canvas? canvas)
            {
                canvas?.Children.Clear();
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        var tile = GetTile(x, y);
                        var rect = new Rectangle()
                        {
                            Width = MAP.TILE_SIZE,
                            Height = MAP.TILE_SIZE,
                            Fill = Brushes.Black, // Replace with your desired tile color
                            Stroke = Brushes.White, // Replace with your desired tile border color
                            StrokeThickness = 0.2,
                        };
                        Canvas.SetLeft(rect, x * MAP.TILE_SIZE);
                        Canvas.SetTop(rect, y * MAP.TILE_SIZE);
                        canvas?.Children.Add(rect);
                    }
                }
            }
        }

        public class Player{
            public Tile Position {get; set;}
            public List<Tile> Tail {get; set;}
            public int slowness = 250;  
            public int fuel = 30;
            //public Item[,]? items;

            public Player(Tile position)
            {
                Position = position;
                Tail = new List<Tile>();
                Tile? tmp = Position;
                for (int i = 0; i < 3 + 1; i++)
                {
                    Tail.Add(tmp!);
                    tmp = tmp?.Bottom;
                }
            }

            public void DrawTail(Canvas? canvas)
            {
                for (int i = 0; i < Tail.Count; i++)
                {
                    Tail[i]?.Draw(canvas, Brushes.LightBlue);
                }
            }

            public void Draw(Canvas? canvas)
            {
                DrawTail(canvas);

                var rect = new Rectangle()
                {
                Width = MAP.TILE_SIZE,
                Height = MAP.TILE_SIZE,
                Fill = Brushes.White, // Replace with your desired player color
                Stroke = Brushes.White, // Replace with your desired player border color
                StrokeThickness = 0.2,
                };
                Canvas.SetLeft(rect, Position.X * MAP.TILE_SIZE);
                Canvas.SetTop(rect, Position.Y * MAP.TILE_SIZE);
                canvas?.Children.Add(rect);
            }

            //1 = up, -1 = down, 2 = left, -2 = right
            public int Move(int direction, Canvas? canvas)
            {
                bool moved = false;
                while (!moved)
                {
                    switch (direction)
                    {
                        case 1:
                            if (Position.Top != null)
                            {
                                Position = Position.Top;
                                moved = true;
                            }
                            else direction = -2;
                            break;
                        case -1:
                            if (Position.Bottom != null)
                            {
                                Position = Position.Bottom;
                                moved = true;
                            }
                            else direction = 2;
                            break;
                        case 2:
                            if (Position.Left != null)
                            {
                                Position = Position.Left;
                                moved = true;
                            }
                            else direction = 1;
                            break;
                        case -2:
                            if (Position.Right != null)
                            {
                                Position = Position.Right;
                                moved = true;
                            }
                            else direction = -1;
                            break;
                    }
                }

                Tail.RemoveAt(Tail.Count - 1);
                Tail.Insert(0, Position);

                Draw(canvas);

                return direction;
            }

            public void Die()
            {
                Environment.Exit(0);
                Console.WriteLine("YOU DIED!");
            }
        }

        public class Item : Tile
        {
            public string? Type {get; set;}

            public Item(int x, int y, string type) : base(x, y)
            {
                Type = type;
            }

            public void Take(Player player)
            {
                switch (Type)
                {
                    case "fuel":
                        player.fuel = 30;
                        break;
                    case "tail":
                        int addition = Random.Shared.Next(1, 3);
                        for (int i = 0; i < addition; i++)
                        {
                            player.Tail.Insert(0, player.Tail[0].Bottom!);
                        }
                        break;
                    case "bomb" :
                        player.Die();
                        break;
                }
            }
        }
    }
}