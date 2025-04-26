using System;
using System.Data;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.Media;
using Avalonia.Metadata;
using TRON;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;

namespace TRON.Avalonia
{
    public partial class MainWindow : Window
    {
        private MAP.Tilemap _tilemap;
        private MAP.Tile _centerTile;
        private MAP.Player _player;
        private int direct = 1;
        private Canvas? Canvas;
        
        // Game timing variables
        private DispatcherTimer _playerTimer;
        private DispatcherTimer _botcitoTimer;
        private bool _needsFullRedraw = true;
        private readonly IBrush _blackBrush = Brushes.Black;
        
        // Preallocated collections to avoid GC
        private readonly List<Rectangle> _recycledRectangles = new List<Rectangle>(100);
        private int _botcitoAmount = 5;
        private List<MAP.Botcito> _botcitos = new List<MAP.Botcito>();

        public MainWindow()
        {
            InitializeComponent();

            Width = (MAP.MAP_WIDTH + 15) * MAP.TILE_SIZE;
            Height = MAP.MAP_HEIGHT * MAP.TILE_SIZE;

            CanResize = false;
            
            Background = new SolidColorBrush(Colors.Black);

            _tilemap = new MAP.Tilemap(MAP.MAP_WIDTH, MAP.MAP_HEIGHT, (240, 0, 0, 0));
            _centerTile = _tilemap.GetCenter();
            _player = new MAP.Player(_centerTile);

            Canvas = this.FindControl<Canvas>("TilemapCanvas");

            // Pre-create rectangles for recycling
            for (int i = 0; i < 100; i++)
            {
                _recycledRectangles.Add(new Rectangle
                {
                    Width = MAP.TILE_SIZE,
                    Height = MAP.TILE_SIZE,
                    StrokeThickness = 0.2,
                });
            }

            // Initialize UI elements
            dataText.Foreground = Brushes.White;
            dataText.Margin = new Thickness(10);

            itemsText.Foreground = Brushes.White;
            itemsText.Margin = new Thickness(10);

            warning.Foreground = Brushes.Red;
            warning.Margin = new Thickness(10);

            // Draw initial state
            _tilemap.Draw(Canvas, _recycledRectangles);
            _player.Draw(Canvas, _recycledRectangles);

            // Initialize bots
            for (int i = 0; i < _botcitoAmount; i++)
            {
                _botcitos.Add(new MAP.Botcito(_tilemap.GetRandomTile(), 5));
            }

            KeyDown += MainWindow_KeyDown;

            // Use lower priority and lower timer frequency
            _playerTimer = new DispatcherTimer(
                TimeSpan.FromMilliseconds(Math.Max(15, _player.slowness)), 
                DispatcherPriority.Background, 
                PlayerTimerTick);
            
            _botcitoTimer = new DispatcherTimer(
                TimeSpan.FromMilliseconds(MAP.defaultSlowness), 
                DispatcherPriority.Background, 
                BotTimerTick);
            
            _playerTimer.Start();
            _botcitoTimer.Start();
        }

        private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.W: direct = 1; break;    // up
                case Key.S: direct = -1; break;   // down
                case Key.A: direct = 2; break;    // left
                case Key.D: direct = -2; break;   // right
                case Key.Space:
                    if (_player.items.Count > 0) _player.items.PopUse(_player);
                    break;
                case Key.Q:
                    _player.items.Change();
                    break;
            }
        }

        private void PlayerTimerTick(object? sender, EventArgs e)
        {
            if (_needsFullRedraw)
            {
                // Clear existing elements
                Canvas?.Children.Clear();
                
                // Full redraw only when needed
                _tilemap.Draw(Canvas, _recycledRectangles);
                _needsFullRedraw = false;
            }
            else
            {
                // Just clear old player position for optimization
                ClearPlayerArea();
            }
            
            // Remember old position and state
            var oldPosition = _player.Position;
            var oldTailCount = _player.Tail.Count;
            
            // Update game state
            direct = _player.Move(direct, _tilemap, _botcitos);
            
            // Check if we need a full redraw next time
            if (oldPosition is MAP.Item || oldTailCount != _player.Tail.Count)
            {
                _needsFullRedraw = true;
            }
            
            // Update player timer interval based on current player speed
            if (_player.slowness != _playerTimer.Interval.TotalMilliseconds)
            {
                _playerTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(15, _player.slowness));
            }
            
            // Update UI text only when needed (throttled)
            string itemsInfo = string.Empty;
            foreach (MAP.Item item in _player.items)
            {
                itemsInfo += item.Type + "\n";
            }
            
            // Draw the player
            _player.Draw(Canvas, _recycledRectangles);
            
            // Draw all entities (optimized drawing)
            foreach (MAP.Botcito botcito in _botcitos)
            {
                if (botcito.body.Count > 0)
                {
                    botcito.Draw(Canvas, _recycledRectangles);
                }
            }
            
            // Update UI data
            dataText.Text = $"Fuel: {_player.fuel} \nTail: {_player.Tail.Count} \nSlowness: {_player.slowness} \nShield: {_player.shield}";
            itemsText.Text = "Items:\n" + itemsInfo;
            warning.Text = _player.fuel <= 10 ? "DANGER! FUEL LOW!" : "";
        }
        
        private void ClearPlayerArea()
        {
            // Remove only the player's elements from canvas instead of clearing everything
            for (int i = Canvas.Children.Count - 1; i >= 0; i--)
            {
                if (Canvas.Children[i] is Rectangle rect)
                {
                    double x = Canvas.GetLeft(rect);
                    double y = Canvas.GetTop(rect);
                    
                    // Check if this rectangle belongs to player or tail
                    if (IsPlayerOrTailTile(x, y))
                    {
                        Canvas.Children.RemoveAt(i);
                    }
                }
            }
        }
        
        private bool IsPlayerOrTailTile(double x, double y)
        {
            int tileX = (int)(x / MAP.TILE_SIZE);
            int tileY = (int)(y / MAP.TILE_SIZE);
            
            if (tileX == _player.Position.X && tileY == _player.Position.Y)
                return true;
            
            foreach (var tile in _player.Tail)
            {
                if (tileX == tile.X && tileY == tile.Y)
                    return true;
            }
            
            return false;
        }
        
        private void BotTimerTick(object? sender, EventArgs e)
        {
            var botDied = false;
            
            // Update bot movement
            foreach (MAP.Botcito botcito in _botcitos)
            {
                if (botcito.body.Count > 0)
                {
                    var oldBodyCount = botcito.body.Count;
                    botcito.Move(_botcitos, _player.Tail, _tilemap);
                    
                    // Check if bot died
                    if (botcito.body.Count == 0 || oldBodyCount != botcito.body.Count)
                    {
                        botDied = true;
                    }
                }
            }
            
            // Set flag for full redraw if a bot died (changes map)
            if (botDied)
            {
                _needsFullRedraw = true;
            }
        }
    }
}