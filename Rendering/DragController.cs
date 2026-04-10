using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using ColorMusic.Model;

namespace ColorMusic.Rendering
{
    public class DragController
    {
        private bool _dragging;
        private Point _startMouse;
        private double _startX;
        private double _startY;

        private readonly Shape _shape;
        private readonly LightSettings _settings;

        public DragController(Shape shape, LightSettings settings)
        {
            _shape = shape;
            _settings = settings;

            _shape.MouseLeftButtonDown += Shape_MouseLeftButtonDown;
            _shape.MouseMove += Shape_MouseMove;
            _shape.MouseLeftButtonUp += Shape_MouseLeftButtonUp;
        }

        private void Shape_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragging = true;
            _startMouse = e.GetPosition((Canvas)_shape.Parent);

            _startX = _settings.X;
            _startY = _settings.Y;

            _shape.CaptureMouse();
        }

        private void Shape_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;

            var pos = e.GetPosition((Canvas)_shape.Parent);

            double dx = pos.X - _startMouse.X;
            double dy = pos.Y - _startMouse.Y;

            _settings.X = _startX + dx;
            _settings.Y = _startY + dy;

            Canvas.SetLeft(_shape, _settings.X);
            Canvas.SetTop(_shape, _settings.Y);
        }

        private void Shape_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _dragging = false;
            _shape.ReleaseMouseCapture();
        }
    }
}