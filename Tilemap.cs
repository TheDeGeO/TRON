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
                    Fill = color,
                    Stroke = Brushes.White,
                    StrokeThickness = 0.2,
                };
                Canvas.SetLeft(rect, X * TILE_SIZE);
                Canvas.SetTop(rect, Y * TILE_SIZE);
                canvas?.Children.Add(rect);
            }

            public void Draw(Canvas? canvas, IBrush color, List<Rectangle> recycledRects)
            {
                if (canvas == null || recycledRects == null || recycledRects.Count == 0) 
                {
                    Draw(canvas, color);
                    return;
                }
                
                // Get a recycled rectangle
                var rect = recycledRects[recycledRects.Count - 1];
                recycledRects.RemoveAt(recycledRects.Count - 1);
                
                // Configure the rectangle
                rect.Fill = color;
                rect.Stroke = Brushes.White;
                Canvas.SetLeft(rect, X * TILE_SIZE);
                Canvas.SetTop(rect, Y * TILE_SIZE);
                
                // Add to canvas
                canvas.Children.Add(rect);
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
                DrawBaseLayer(canvas);
                DrawItems(canvas);
            }
            
            public void Draw(Canvas? canvas, List<Rectangle> recycledRects)
            {
                if (canvas == null) return;
                
                canvas.Children.Clear();
                canvas.Margin = new Thickness(leftMargin, topMargin, rightMargin, bottomMargin);
                DrawBaseLayer(canvas, recycledRects);
                DrawItems(canvas, recycledRects);
            }
            
            // Draw only the base tiles (background)
            public void DrawBaseLayer(Canvas? canvas)
            {
                if (canvas == null) return;
                
                canvas!.Margin = new Thickness(leftMargin, topMargin, rightMargin, bottomMargin);
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        _tiles[x, y].Draw(canvas, Brushes.Black);
                    }
                }
            }
            
            public void DrawBaseLayer(Canvas? canvas, List<Rectangle> recycledRects)
            {
                if (canvas == null) return;
                
                canvas.Margin = new Thickness(leftMargin, topMargin, rightMargin, bottomMargin);
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        if (recycledRects.Count > 0)
                            _tiles[x, y].Draw(canvas, Brushes.Black, recycledRects);
                        else
                            _tiles[x, y].Draw(canvas, Brushes.Black);
                    }
                }
            }
            
            // Draw only the items
            public void DrawItems(Canvas? canvas)
            {
                if (canvas == null) return;
                
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        if (_tiles[x, y] is Item item)
                        {
                            item.Draw(canvas);
                        }
                    }
                }
            }
            
            public void DrawItems(Canvas? canvas, List<Rectangle> recycledRects)
            {
                if (canvas == null) return;
                
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        if (_tiles[x, y] is Item item)
                        {
                            if (recycledRects.Count > 0)
                                item.Draw(canvas, recycledRects);
                            else
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

            public void DrawTail(Canvas? canvas, List<Rectangle> recycledRects)
            {
                if (canvas == null) return;
                
                for (int i = 0; i < Tail.Count; i++)
                {
                    if (recycledRects.Count > 0)
                        Tail[i]?.Draw(canvas, Brushes.Pink, recycledRects);
                    else
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
                Stroke = Brushes.White,
                StrokeThickness = 0.2,
                };
                Canvas.SetLeft(rect, Position.X * MAP.TILE_SIZE);
                Canvas.SetTop(rect, Position.Y * MAP.TILE_SIZE);
                canvas?.Children.Add(rect);
            }
            
            public void Draw(Canvas? canvas, List<Rectangle> recycledRects)
            {
                if (canvas == null) return;
                
                DrawTail(canvas, recycledRects);
                
                if (recycledRects.Count > 0)
                {
                    // Get a recycled rectangle
                    var rect = recycledRects[recycledRects.Count - 1];
                    recycledRects.RemoveAt(recycledRects.Count - 1);
                    
                    // Configure the rectangle
                    rect.Fill = Color;
                    rect.Stroke = Brushes.White;
                    Canvas.SetLeft(rect, Position.X * MAP.TILE_SIZE);
                    Canvas.SetTop(rect, Position.Y * MAP.TILE_SIZE);
                    
                    // Add to canvas
                    canvas.Children.Add(rect);
                }
                else
                {
                    var rect = new Rectangle()
                    {
                        Width = MAP.TILE_SIZE,
                        Height = MAP.TILE_SIZE,
                        Fill = Color,
                        Stroke = Brushes.White,
                        StrokeThickness = 0.2,
                    };
                    Canvas.SetLeft(rect, Position.X * MAP.TILE_SIZE);
                    Canvas.SetTop(rect, Position.Y * MAP.TILE_SIZE);
                    canvas.Children.Add(rect);
                }
            }

            //1 = up, -1 = down, 2 = left, -2 = right
            public int Move(int direction, Canvas? canvas, Tilemap tilemap, List<Botcito> botcitos)
            {
                return Move(direction, tilemap, botcitos);
            }
            
            public int Move(int direction, Tilemap tilemap, List<Botcito> botcitos)
            {
                // Add current position to tail and remove the last segment
                Tail.RemoveAt(Tail.Count - 1);
                Tail.Insert(0, Position);

                // Handle movement based on direction
                bool moved = false;
                while (!moved)
                {
                    switch (direction)
                    {
                        case 1: // Up
                            if (Position.Top != null)
                            {
                                Position = Position.Top;
                                moved = true;
                            }
                            else direction = -2; // If can't move up, try right
                            break;
                        case -1: // Down
                            if (Position.Bottom != null)
                            {
                                Position = Position.Bottom;
                                moved = true;
                            }
                            else direction = 2; // If can't move down, try left
                            break;
                        case 2: // Left
                            if (Position.Left != null)
                            {
                                Position = Position.Left;
                                moved = true;
                            }
                            else direction = 1; // If can't move left, try up
                            break;
                        case -2: // Right
                            if (Position.Right != null)
                            {
                                Position = Position.Right;
                                moved = true;
                            }
                            else direction = -1; // If can't move right, try down
                            break;
                    }
                }

                // Check collisions with bots
                foreach (Botcito bot in botcitos)
                {
                    if (bot.body.Contains(Position))
                    {
                        if (Position == bot.body[0]) bot.Die(tilemap);
                        Die(tilemap.GetCenter());
                    }
                }

                // Check if player picked up an item
                if (Position is Item item)
                {
                    int x = item.X;
                    int y = item.Y;
                    tilemap._tiles[x, y] = item.Take(this, tilemap.GetCenter());
                }

                // Decrease fuel
                if (fuel > 0)
                {
                    fuel -= 0.2;
                    fuel = Math.Round(fuel, 1);
                }
                else if (fuel <= 0 && shield <= 0)
                {
                    Die(tilemap.GetCenter());
                }

                // Check collision with own tail
                if (Tail.Contains(Position) && shield <= 0)
                {
                    Die(tilemap.GetCenter());
                }

                // Handle speed power-up effect decay
                if (slowness < defaultSlowness)
                {
                    slowness += 10;
                }
                
                // Decrease shield if active
                if (shield > 0)
                {
                    shield -= 1;
                }
                
                // Reset color when no power-ups are active
                if (slowness == defaultSlowness && shield == 0)
                {
                    Color = Brushes.White;
                }

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
                    Stroke = Brushes.White,
                    StrokeThickness = 0.2,
                };
                Canvas.SetLeft(rect, X * MAP.TILE_SIZE);
                Canvas.SetTop(rect, Y * MAP.TILE_SIZE);
                canvas?.Children.Add(rect);
            }

            public void Draw(Canvas? canvas, List<Rectangle> recycledRects)
            {
                if (canvas == null) return;
                
                IBrush? fill = null;
                switch (Type)
                    {
                        case "fuel": fill = Brushes.Green; break;
                        case "tail": fill = Brushes.Blue; break;
                        case "bomb": fill = Brushes.Red; break;
                        case "shield": fill = Brushes.Orange; break;
                        case "speed": fill = Brushes.Purple; break;
                        default: fill = Brushes.Yellow; break;
                    }
                
                if (recycledRects.Count > 0)
                {
                    // Get a recycled rectangle
                    var rect = recycledRects[recycledRects.Count - 1];
                    recycledRects.RemoveAt(recycledRects.Count - 1);
                    
                    // Configure the rectangle
                    rect.Fill = fill;
                    rect.Stroke = Brushes.White;
                    Canvas.SetLeft(rect, X * MAP.TILE_SIZE);
                    Canvas.SetTop(rect, Y * MAP.TILE_SIZE);
                    
                    // Add to canvas
                    canvas.Children.Add(rect);
                }
                else
                {
                    var rect = new Rectangle()
                    {
                        Width = MAP.TILE_SIZE,
                        Height = MAP.TILE_SIZE,
                        Fill = fill, 
                        Stroke = Brushes.White,
                        StrokeThickness = 0.2,
                    };
                    Canvas.SetLeft(rect, X * MAP.TILE_SIZE);
                    Canvas.SetTop(rect, Y * MAP.TILE_SIZE);
                    canvas.Children.Add(rect);
                }
            }
        }

        public class Botcito{
            public List<Tile> body = new List<Tile>();

            public Botcito(Tile head, int length)
            {
                Tile? tmp = head;
                for (int i = 0; i < length; i++)
                {
                    body.Add(tmp!);
                    tmp = tmp?.Bottom;
                }
            }

            public void Draw(Canvas? canvas)
            {
                for (int i = 0; i < body.Count; i++)
                {
                    body[i]?.Draw(canvas, i == 0 ? Brushes.OrangeRed : Brushes.Brown);
                }
            }

            public void Draw(Canvas? canvas, List<Rectangle> recycledRects)
            {
                if (canvas == null) return;
                
                for (int i = 0; i < body.Count; i++)
                {
                    if (recycledRects.Count > 0)
                        body[i]?.Draw(canvas, i == 0 ? Brushes.OrangeRed : Brushes.Brown, recycledRects);
                    else
                        body[i]?.Draw(canvas, i == 0 ? Brushes.OrangeRed : Brushes.Brown);
                }
            }

            public void Move(List<Botcito> botcitos, List<Tile> player, Tilemap map)
            {
                if (!botcitos.Contains(this) || body.Count == 0) return;

                var newHead = body[0];
                var moved = false;
                var maxTries = 20;
                var tries = 0;

                while (!moved && tries < maxTries)
                {
                    int direction = Random.Shared.Next(0, 4);
                    
                    // More efficient direction checking
                    switch (direction)
                    {
                        case 0: newHead = body[0].Top; break;
                        case 1: newHead = body[0].Bottom; break;
                        case 2: newHead = body[0].Left; break;
                        case 3: newHead = body[0].Right; break;
                    }

                    // Skip invalid moves
                    if (newHead == null || body.Contains(newHead))
                    {
                        tries++;
                        continue;
                    }

                    // Check collision with other bots
                    bool collisionWithBot = false;
                    foreach (var bot in botcitos)
                    {
                        if (bot != this && bot.body.Contains(newHead))
                        {
                            collisionWithBot = true;
                            break;
                        }
                    }
                    
                    if (collisionWithBot)
                    {
                        tries++;
                        continue;
                    }

                    // Check collision with player
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
                Console.WriteLine("A botcito died!");
            }
        }
    }
}