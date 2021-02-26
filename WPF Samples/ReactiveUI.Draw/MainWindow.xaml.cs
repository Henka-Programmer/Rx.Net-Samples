using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ReactiveUI.Drawboard
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IDisposable _disposableTracking;

        public MainWindow()
        {
            InitializeComponent();
            Closing += MainWindow_Closing;
            _disposableTracking = TrackMouseDrag(canv, Draw, DrawCompleted);
        }

        private void DrawCompleted()
        {
            // On draw completed
        }

        private static void Draw((Polyline currentLine, Point nextPoint) drawInfo)
        {
            drawInfo.currentLine.Points.Add(drawInfo.nextPoint);
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                _disposableTracking?.Dispose();
            }
            catch
            {
                // ignored
            }
        }


        private static Polyline GetNewPolylineInstance()
        {
            return new Polyline
            {
                Stroke = Brushes.Black,
                StrokeThickness = 4
            };
        }

        public IDisposable TrackMouseDrag(Canvas canvas, Action<(Polyline, Point)> dragging, Action dragComplete)
        {
            var mouseDownStream =
                from evt in Observable.FromEventPattern<MouseButtonEventArgs>(canvas, nameof(MouseDown))
                select evt.EventArgs.GetPosition(canvas);

            var mouseMoveStream = from evt in Observable.FromEventPattern<MouseEventArgs>(canvas, nameof(MouseMove))
                select evt.EventArgs.GetPosition(canvas);

            var mouseUpStream = Observable.FromEventPattern<MouseButtonEventArgs>(canvas, nameof(MouseUp));

            return mouseDownStream
                .Select(StartNewLine)
                .SelectMany(start => mouseMoveStream
                        .TakeUntil(mouseUpStream)
                        .Do(_ => { }, dragComplete),
                    (start, currentPosition) => (start, new Point(currentPosition.X, currentPosition.Y)))
                .Subscribe(dragging);
        }

        private Polyline StartNewLine(Point start)
        {
            var currentLine = GetNewPolylineInstance();
            canv.Children.Add(currentLine);
            currentLine.Points.Add(start);
            return currentLine;
        }

        #region Buttons click event

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            canv.Children.Clear();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            log.Clear();
        }

        #endregion
    }
}