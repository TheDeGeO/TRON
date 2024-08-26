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

namespace TRON.Avalonia
{
    public partial class MainWindow : Window
    {
        private MAP.Tilemap _tilemap;
        private MAP.Tile _centerTile;
        private MAP.Player _player;
        private int direct = 1;
        private Canvas? Canvas;
        private DispatcherTimer _timer;
        private int playerSlowness = 100;

        public MainWindow()
        {
            InitializeComponent();

            Width = MAP.MAP_WIDTH * MAP.TILE_SIZE;
            Height = MAP.MAP_HEIGHT * MAP.TILE_SIZE;

            CanResize = false;

            _tilemap = new MAP.Tilemap(MAP.MAP_WIDTH, MAP.MAP_HEIGHT);
            _centerTile = _tilemap.GetCenter();
            _player = new MAP.Player(_centerTile);

            Canvas = this.FindControl<Canvas>("TilemapCanvas");
            _tilemap.Draw(Canvas);
            _player.Draw(Canvas);

            KeyDown += MainWindow_KeyDown;

            _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(playerSlowness), DispatcherPriority.Normal, MovePlayer);
            _timer.Start();
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
        }

        private void MovePlayer(object? sender, EventArgs e)
        {
            _tilemap.Draw(Canvas);
            direct = _player.Move(direct, Canvas);
        }
    }
}