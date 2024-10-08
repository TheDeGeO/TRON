using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Converters;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Remote.Protocol.Input;


namespace TRON.Avalonia{
    public class MAP{

        //Tile size in pixels
        public const int TILE_SIZE = 32;
        //Tilemap size
        public const int MAP_WIDTH = 32 - 8;
        public const int MAP_HEIGHT = 22;

        public const int itemStartAmount = 20;
        public const int itemDeathAmount = 8;
        public const int defaultSlowness = 300;
        public static readonly string[] itemTypes = {"fuel", "tail", "bomb", "shield", "speed"};

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
            public Tile[,] _tiles;
            public int Width {get; set;}
            public int Height {get; set;}
            public int leftMargin = 0;
            public int topMargin = 0;
            public int rightMargin = 0;
            public int bottomMargin = 0; 

            public Tilemap(int width, int height, (int, int, int, int) margins)
            {
                Width = width;
                Height = height;
                leftMargin = margins.Item1;
                topMargin = margins.Item2;
                rightMargin = margins.Item3;
                bottomMargin = margins.Item4;
                _tiles = new Tile[width, height];

                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        _tiles[x, y] = new Tile(x, y);
                    }
                }

                GenerateItems(itemStartAmount);

                InitializeTileNeighbors();
            }

            public void InitializeTileNeighbors()
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

            public void GenerateItems(int amount)
            {
                for (int i = 0; i < amount; i++)
                {
                    int x = Random.Shared.Next(0, Width);
                    int y = Random.Shared.Next(0, Height);
                    string type = itemTypes[Random.Shared.Next(0, itemTypes.Length)];
                    _tiles[x, y] = new Item(x, y, type);
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

            public Tile GetRandomTile()
            {
                int x = Random.Shared.Next(0, Width);
                int y = Random.Shared.Next(0, Height);
                return GetTile(x, y);
            }

            public void Draw(Canvas? canvas)
            {
                canvas?.Children.Clear();
                canvas!.Margin = new Thickness(leftMargin, topMargin, rightMargin, bottomMargin); 
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        _tiles[x, y].Draw(canvas, Brushes.Black);

                        if (_tiles[x, y] is Item item)
                        {
                            item.Draw(canvas);
                        }
                    }
                }
            }
        }
        
        public class ItemStack<T> : Stack<T> where T : Item
        {
            public delegate void BeforePopHandler(T elemento);
            public event BeforePopHandler? BeforePop;

            public T PopUse(Player player)
            {
                T item = base.Peek();
                item.Use(player);
                return base.Pop();
            }

            public void Change()
            {
                if (Count <= 1) return; // No need to change if there's 0 or 1 element.

                // Create a copy of the current items.
                var topItem = Pop();    
                var tmpStack = new Stack<T>();
                var stackSize = Count;
                for (int i = 0; i < stackSize; i++)
                {
                    tmpStack.Push(Pop());
                }
                Push(topItem);
                stackSize = tmpStack.Count;
                for (int i = 0; i < stackSize; i++)
                {
                    Push(tmpStack.Pop());
                }
            }


        }
        public class Player{
            public Tile Position {get; set;}
            public List<Tile> Tail {get; set;}
            public int slowness = defaultSlowness;  
            public int shield = 0;
            public double fuel = 30;
            public ItemStack<Item> items = new();
            public IBrush Color;

            public Player(Tile position)
            {
                Position = position;
                Color = Brushes.White;
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
                    Tail[i]?.Draw(canvas, Brushes.Pink);
                }
            }

            public void Draw(Canvas? canvas)
            {
                DrawTail(canvas);

                var rect = new Rectangle()
                {
                Width = MAP.TILE_SIZE,
                Height = MAP.TILE_SIZE,
                Fill = Color,
                Stroke = Brushes.White, // Replace with your desired player border color
                StrokeThickness = 0.2,
                };
                Canvas.SetLeft(rect, Position.X * MAP.TILE_SIZE);
                Canvas.SetTop(rect, Position.Y * MAP.TILE_SIZE);
                canvas?.Children.Add(rect);
            }

            //1 = up, -1 = down, 2 = left, -2 = right
            public int Move(int direction, Canvas? canvas, Tilemap tilemap, List<Botcito> botcitos)
            {
                Tail.RemoveAt(Tail.Count - 1);
                Tail.Insert(0, Position);

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

                foreach (Botcito bot in botcitos)
                {
                    if (bot.body.Contains(Position))
                    {
                        if (Position == bot.body[0]) bot.Die(tilemap);
                        Die(tilemap.GetCenter());
                    }
                }

                if (Position is Item item)
                {
                    int x = item.X;
                    int y = item.Y;
                    tilemap._tiles[x, y] = item.Take(this, tilemap.GetCenter());
                }

                if (fuel > 0)
                {
                    fuel -= 0.2;
                    fuel = Math.Round(fuel, 1);
                }
                else if (fuel <= 0 && shield <= 0)
                {
                    Die(tilemap.GetCenter());
                }

                if (Tail.Contains(Position) && shield <= 0)
                {
                    Die(tilemap.GetCenter());
                }

                if (slowness < defaultSlowness)
                {
                    slowness += 10;
                }
                

                if (shield > 0)
                {
                    shield -= 1;
                }
                
                if (slowness == defaultSlowness && shield == 0)
                {
                    Color = Brushes.White;
                }

                Draw(canvas);

                return direction;
            }

            public void Die(Tile spawnPoint)
            {
                Console.WriteLine("YOU DIED!");
                Color = Brushes.GreenYellow;
                shield = 5;
                Position = spawnPoint;
                Tail = new List<Tile>();
                items = new();
                Tile? tmp = Position;
                Tail.Add(tmp!);
                tmp = tmp?.Bottom;
                fuel = 15;
                slowness = defaultSlowness;
            }
        }

        public class Item : Tile
        {
            public string? Type {get; set;}

            public Item(int x, int y, string type) : base(x, y)
            {
                if (!itemTypes.Contains(type))
                {
                    throw new ArgumentException("Invalid item type");
                }
                Type = type;
            }

            public Tile Take(Player player, Tile spawnPoint)
            {
                switch (Type)
                {
                    case "fuel":
                        player.fuel += 10;
                        break;
                    case "tail":
                        int addition = Random.Shared.Next(1, 3);
                        for (int i = 0; i < addition; i++)
                        {
                            player.Tail.Add(player.Tail[0]);
                        }
                        break;
                    case "bomb" :
                        if (player.shield <= 0)
                        {
                            player.Die(spawnPoint);
                        }
                        break;
                    default:
                        player.items.Push(this);
                        break;
                }

                Tile tile = new Tile(X, Y);
                tile.Top = this.Top;
                tile.Bottom = this.Bottom;
                tile.Left = this.Left;
                tile.Right = this.Right;
                return tile;
            }

            public void Use(Player player)
            {
                switch (Type)
                {
                    case "shield":
                        player.shield += 15;
                        player.Color = Brushes.GreenYellow;
                        break;
                    case "speed":
                        player.slowness = 150;
                        player.Color = Brushes.MediumPurple;
                        break;
                }
            }

            public void Draw(Canvas? canvas)
            {
                IBrush? fill = null;
                switch (Type)
                    {
                        case "fuel":
                            fill = Brushes.Green;
                            break;
                        case "tail":
                            fill = Brushes.Blue;
                            break;
                        case "bomb":
                            fill = Brushes.Red;
                            break;
                        case "shield":
                            fill = Brushes.Orange;
                            break;
                        case "speed":
                            fill = Brushes.Purple;
                            break;
                        default:
                            fill = Brushes.Yellow;
                            break;
                    }

                var rect = new Rectangle()
                {
                    Width = MAP.TILE_SIZE,
                    Height = MAP.TILE_SIZE,
                    Fill = fill, 
                    Stroke = Brushes.White, // Replace with your desired item border color
                    StrokeThickness = 0.2,
                };
                Canvas.SetLeft(rect, X * MAP.TILE_SIZE);
                Canvas.SetTop(rect, Y * MAP.TILE_SIZE);
                canvas?.Children.Add(rect);
            }
        }

        public class Botcito{
            public List<Tile> body = new List<Tile>();

            public Botcito(Tile head, int length)
            {
                body.Add(head);

                for (int i = 0; i < length; i++)
                {
                    body.Add(head.Bottom!);
                }
            }

            public void Draw(Canvas? canvas)
            {
                var isHead = true;
                foreach (var tile in body)
                {
                    if (isHead)
                    {
                        tile.Draw(canvas, Brushes.White);
                        isHead = false;
                    }
                    else
                    {
                        tile.Draw(canvas, Brushes.Aquamarine);
                    }
                }
            }

            public void Move(List<Botcito> botcitos, List<Tile> player, Tilemap map)
            {
                if (!botcitos.Contains(this)) return;

                var newHead = body[0];
                var moved = false;
                var maxTries = 20;
                var tries = 0;

                while (!moved && tries < maxTries)
                {
                    int direction = Random.Shared.Next(0, 4);

                    switch (direction)
                    {
                        case 0:
                            newHead = body[0].Top;
                            break;
                        case 1:
                            newHead = body[0].Bottom;
                            break;
                        case 2:
                            newHead = body[0].Left;
                            break;
                        case 3:
                            newHead = body[0].Right;
                            break;
                    }

                    if (newHead == null || body.Contains(newHead))
                    {
                        tries++;
                        continue;
                    }

                    foreach (var bot in botcitos)
                    {
                        if (bot.body.Contains(newHead))
                        {
                            tries++;
                            continue;
                        }
                    }

                    if (player.Contains(newHead))
                    {
                        Die(map);
                        return;
                    }

                    // Move the bot
                    body.Insert(0, newHead);
                    body.RemoveAt(body.Count - 1);
                    moved = true;
                }

                if (!moved)
                {
                    Die(map);
                }
            }

            public void Die(Tilemap map)
            {
                map.GenerateItems(itemDeathAmount);
                map.InitializeTileNeighbors();
                body.Clear();
                //botcitos.Remove(this);
                Console.WriteLine("A botcito died!");
            }
        }
    }
}