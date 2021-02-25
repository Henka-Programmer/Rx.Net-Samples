using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ReactiveUI.Drawboard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Closing += MainWindow_Closing;
            TrackMouseDrag(canv, Draw, DrawCompleted);
        }

        private void DrawCompleted()
        {
            // On draw completed
        }

        private void Draw((Polyline currentLine, Point nextPoint) drawInfo)
        {
            drawInfo.currentLine.Points.Add(drawInfo.nextPoint);
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                disposableTracking?.Dispose();
            }
            catch { }
        }


        private Polyline GetNewPolylineInstance()
        {
            return new Polyline
            {
                Stroke = Brushes.Black,
                StrokeThickness = 4
            };
        }

        public IDisposable TrackMouseDrag(Canvas canvas,
    Action<(Polyline, Point)> dragging, Action dragComplete)
        {
            var mouseDown = from evt in Observable.FromEventPattern<MouseButtonEventArgs>(canvas, "MouseDown")
                            select evt.EventArgs.GetPosition(canvas);

            var mouseMove = from evt in Observable.FromEventPattern<MouseEventArgs>(canvas, "MouseMove")
                            select evt.EventArgs.GetPosition(canvas);
            var mouseUp = Observable.FromEventPattern<MouseButtonEventArgs>(canvas, "MouseUp");

            return (from start in mouseDown.Select(s => StartNewLine(s))
                    from currentPosition in mouseMove.TakeUntil(mouseUp)
                            .Do(_ => { }, () => dragComplete())
                    select (start, new Point(currentPosition.X, currentPosition.Y)))
            .Subscribe(dragging);
        }

        private Polyline StartNewLine(Point start)
        {
            var currentLine = GetNewPolylineInstance();
            canv.Children.Add(currentLine);
            currentLine.Points.Add(start);
            return currentLine;
        }

        private IDisposable disposableTracking;

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
