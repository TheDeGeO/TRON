using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using TRON;

namespace TRON.Avalonia
{
    public partial class MainWindow : Window
    {
        private MAP.Tilemap _tilemap;

        public MainWindow()
        {
            InitializeComponent();

            Width = 1000;
            Height = 720;

            CanResize = false;

            _tilemap = new MAP.Tilemap(MAP.MAP_WIDTH, MAP.MAP_HEIGHT);

            DrawTilemap();
        }

        private void DrawTilemap()
        {
            var canvas = this.FindControl<Canvas>("TilemapCanvas");
            canvas?.Children.Clear();

            for (int x = 0; x < _tilemap.Width; x++)
            {
                for (int y = 0; y < _tilemap.Height; y++)
                {
                    var tile = _tilemap.GetTile(x, y);
                    var rect = new Rectangle()
                    {
                        Width = MAP.TILE_SIZE,
                        Height = MAP.TILE_SIZE,
                        Fill = Brushes.White, // Replace with your desired tile color
                        Stroke = Brushes.Black, // Replace with your desired tile border color
                        StrokeThickness = 0.2,
                    };
                    Canvas.SetLeft(rect, x * MAP.TILE_SIZE);
                    Canvas.SetTop(rect, y * MAP.TILE_SIZE);
                    canvas?.Children.Add(rect);
                }
            }
        }
    }
}