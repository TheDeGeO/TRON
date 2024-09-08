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

namespace TRON.Avalonia
{
    public partial class MainWindow : Window
    {
        private MAP.Tilemap _tilemap;
        private MAP.Tile _centerTile;
        private MAP.Player _player;
        private int direct = 1;
        private Canvas? Canvas;
        private DispatcherTimer _playerTimer;
        private DispatcherTimer _botcitoTimer;
        private int _botcitoSlowness = 300;
        private int _botcitoAmount = 5;
        private List<MAP.Botcito> _botcitos = new List<MAP.Botcito>();

        public MainWindow()
        {
            InitializeComponent();

            Width = (MAP.MAP_WIDTH + 15) * MAP.TILE_SIZE;
            Height = MAP.MAP_HEIGHT * MAP.TILE_SIZE;

            CanResize = false;

            _tilemap = new MAP.Tilemap(MAP.MAP_WIDTH, MAP.MAP_HEIGHT, (240, 0, 0, 0));
            _centerTile = _tilemap.GetCenter();
            _player = new MAP.Player(_centerTile);

            Canvas = this.FindControl<Canvas>("TilemapCanvas");

            Background = new SolidColorBrush(Colors.Black);

            _tilemap.Draw(Canvas);
            _player.Draw(Canvas);

            KeyDown += MainWindow_KeyDown;

            _playerTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(_player.slowness), DispatcherPriority.Normal, EventHandler);
            _playerTimer.Start();

            _botcitoTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(_botcitoSlowness), DispatcherPriority.Normal, botHandler);
            _botcitoTimer.Start();

            dataText.Foreground = new SolidColorBrush(Colors.White);
            dataText.Margin = new Thickness(10);

            itemsText.Foreground = new SolidColorBrush(Colors.White);
            itemsText.Margin = new Thickness(10);

            warning.Foreground = new SolidColorBrush(Colors.Red);
            warning.Margin = new Thickness(10);

            //Add botcitos
            for (int i = 0; i < _botcitoAmount; i++)
            {
                _botcitos.Add(new MAP.Botcito(_tilemap.GetRandomTile(), 5));
            }

        }

        private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
        {
            //1 = up, -1 = down, 2 = left, -2 = right
            if (e.Key == Key.W)
            {
                direct = 1;
            }
            if (e.Key == Key.S)
            {
                direct = -1;
            }
            if (e.Key == Key.A)
            {
                direct = 2;
            }
            if (e.Key == Key.D)
            {
                direct = -2;
            }

            if (e.Key == Key.Space && _player.items.Count > 0) 
            {
                _player.items.PopUse(_player);
            }

            if (e.Key == Key.Q)
            {
                _player.items.Change();
            }


        }

        private void EventHandler(object? sender, EventArgs e)
        {
            _tilemap.Draw(Canvas);
            direct = _player.Move(direct, Canvas, _tilemap, _botcitos);

            dataText.Text = $"Fuel: {_player.fuel} \nTail: {_player.Tail.Count} \nSlowness: {_player.slowness} \nShield: {_player.shield}";

            string itemString = "Items:\n";

            foreach (MAP.Item item in _player.items)
            {
                itemString += item.Type + "\n";
            }
            itemsText.Text = itemString;

            _playerTimer.Interval = TimeSpan.FromMilliseconds(_player.slowness);

            if (_player.fuel <= 10)
            {
                warning.Text = "DANGER! FUEL LOW!";
            }
            else warning.Text = "";

            foreach (MAP.Botcito botcito in _botcitos)
            {
                if (botcito.body.Count > 0)
                {
                    botcito.Draw(Canvas);
                }
                
            }
            
        }

        private void botHandler(object? sender, EventArgs e)
        {
            foreach (MAP.Botcito botcito in _botcitos)
            {
                if (botcito.body.Count > 0)
                {
                    botcito.Move(_botcitos, _player.Tail, _tilemap);
                }
                
            }
        }

    }
}