using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DiplomWork_Ivan_2026.Trends
{
    public class CanvasTrendChartRenderer
    {
        public TrendChartRenderState? Draw(
            Canvas canvas,
            List<double> xValues,
            List<ChartSeries> series,
            TrendChartViewport? viewport = null)
        {
            canvas.Children.Clear();

            if (xValues.Count < 2 || series.Count == 0)
                return null;

            double width = canvas.ActualWidth;
            double height = canvas.ActualHeight;

            if (width <= 0 || height <= 0)
                return null;

            double marginLeft = 70;
            double marginRight = 30;
            double marginTop = 70;
            double marginBottom = 50;

            double plotWidth = width - marginLeft - marginRight;
            double plotHeight = height - marginTop - marginBottom;

            if (plotWidth <= 0 || plotHeight <= 0)
                return null;

            double minX = xValues.Min();
            double maxX = xValues.Max();
            double minY = series
                .Where(s => s.Values.Count > 0)
                .Min(s => s.Values.Min());
            double maxY = series
                .Where(s => s.Values.Count > 0)
                .Max(s => s.Values.Max());

            if (viewport != null &&
                viewport.MaxX > viewport.MinX &&
                viewport.MaxY > viewport.MinY)
            {
                minX = viewport.MinX;
                maxX = viewport.MaxX;
                minY = viewport.MinY;
                maxY = viewport.MaxY;
            }

            if (Math.Abs(maxX - minX) < 0.0001)
                maxX = minX + 1;

            if (Math.Abs(maxY - minY) < 0.0001)
            {
                maxY = minY + 1;
                minY = minY - 1;
            }

            TrendChartRenderState renderState = new TrendChartRenderState
            {
                MinX = minX,
                MaxX = maxX,
                MinY = minY,
                MaxY = maxY,
                MarginLeft = marginLeft,
                MarginTop = marginTop,
                PlotWidth = plotWidth,
                PlotHeight = plotHeight
            };

            DrawAxes(canvas, renderState);
            DrawSeries(canvas, xValues, series, renderState);
            DrawLegend(canvas, series, marginLeft, 15);

            return renderState;
        }

        private static void DrawSeries(
            Canvas canvas,
            List<double> xValues,
            List<ChartSeries> series,
            TrendChartRenderState renderState)
        {
            foreach (ChartSeries chartSeries in series)
            {
                Polyline line = new Polyline
                {
                    Stroke = chartSeries.Brush,
                    StrokeThickness = 3,
                    StrokeLineJoin = PenLineJoin.Round,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    Clip = new RectangleGeometry(new Rect(
                        renderState.MarginLeft,
                        renderState.MarginTop,
                        renderState.PlotWidth,
                        renderState.PlotHeight))
                };

                int count = Math.Min(xValues.Count, chartSeries.Values.Count);

                for (int i = 0; i < count; i++)
                {
                    double x = renderState.MarginLeft +
                               (xValues[i] - renderState.MinX) /
                               (renderState.MaxX - renderState.MinX) *
                               renderState.PlotWidth;

                    double y = renderState.MarginTop +
                               renderState.PlotHeight -
                               (chartSeries.Values[i] - renderState.MinY) /
                               (renderState.MaxY - renderState.MinY) *
                               renderState.PlotHeight;

                    line.Points.Add(new Point(x, y));
                }

                canvas.Children.Add(line);
            }
        }

        private static void DrawLegend(Canvas canvas, List<ChartSeries> series, double left, double top)
        {
            double legendLeft = left + 10;
            double legendTop = top + 10;

            Border legendBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(220, 25, 25, 25)),
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8)
            };

            StackPanel legendPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            legendBorder.Child = legendPanel;

            foreach (ChartSeries item in series)
            {
                StackPanel row = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 20, 0)
                };

                Rectangle colorBox = new Rectangle
                {
                    Width = 14,
                    Height = 4,
                    Fill = item.Brush,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 6, 0)
                };

                TextBlock label = new TextBlock
                {
                    Text = item.Name,
                    Foreground = Brushes.White,
                    FontSize = 13,
                    VerticalAlignment = VerticalAlignment.Center
                };

                row.Children.Add(colorBox);
                row.Children.Add(label);

                legendPanel.Children.Add(row);
            }

            Canvas.SetLeft(legendBorder, legendLeft);
            Canvas.SetTop(legendBorder, legendTop);

            canvas.Children.Add(legendBorder);
        }

        private static void DrawAxes(Canvas canvas, TrendChartRenderState renderState)
        {
            Line yAxis = new Line
            {
                X1 = renderState.MarginLeft,
                Y1 = renderState.MarginTop,
                X2 = renderState.MarginLeft,
                Y2 = renderState.MarginTop + renderState.PlotHeight,
                Stroke = Brushes.Gray,
                StrokeThickness = 1
            };

            Line xAxis = new Line
            {
                X1 = renderState.MarginLeft,
                Y1 = renderState.MarginTop + renderState.PlotHeight,
                X2 = renderState.MarginLeft + renderState.PlotWidth,
                Y2 = renderState.MarginTop + renderState.PlotHeight,
                Stroke = Brushes.Gray,
                StrokeThickness = 1
            };

            canvas.Children.Add(yAxis);
            canvas.Children.Add(xAxis);

            DrawYTicks(canvas, renderState);
            DrawXTicks(canvas, renderState);
            DrawTimeLabel(canvas, renderState);
        }

        private static void DrawYTicks(Canvas canvas, TrendChartRenderState renderState)
        {
            int yTickCount = 6;

            for (int i = 0; i < yTickCount; i++)
            {
                double ratio = (double)i / (yTickCount - 1);
                double y = renderState.MarginTop + renderState.PlotHeight - ratio * renderState.PlotHeight;
                double value = renderState.MinY + ratio * (renderState.MaxY - renderState.MinY);

                Line tick = new Line
                {
                    X1 = renderState.MarginLeft - 5,
                    Y1 = y,
                    X2 = renderState.MarginLeft,
                    Y2 = y,
                    Stroke = Brushes.White,
                    StrokeThickness = 1
                };
                canvas.Children.Add(tick);

                Line gridLine = new Line
                {
                    X1 = renderState.MarginLeft,
                    Y1 = y,
                    X2 = renderState.MarginLeft + renderState.PlotWidth,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                    StrokeThickness = 0.8
                };
                canvas.Children.Add(gridLine);

                TextBlock label = new TextBlock
                {
                    Text = value.ToString("F1"),
                    Foreground = Brushes.White,
                    FontSize = 12
                };

                Canvas.SetLeft(label, 10);
                Canvas.SetTop(label, y - 10);
                canvas.Children.Add(label);
            }
        }

        private static void DrawXTicks(Canvas canvas, TrendChartRenderState renderState)
        {
            int xTickCount = 6;
            double visibleSpanSeconds = renderState.MaxX - renderState.MinX;

            for (int i = 0; i < xTickCount; i++)
            {
                double ratio = (double)i / (xTickCount - 1);
                double x = renderState.MarginLeft + ratio * renderState.PlotWidth;
                double value = renderState.MinX + ratio * (renderState.MaxX - renderState.MinX);

                Line tick = new Line
                {
                    X1 = x,
                    Y1 = renderState.MarginTop + renderState.PlotHeight,
                    X2 = x,
                    Y2 = renderState.MarginTop + renderState.PlotHeight + 5,
                    Stroke = Brushes.White,
                    StrokeThickness = 1
                };
                canvas.Children.Add(tick);

                Line gridLine = new Line
                {
                    X1 = x,
                    Y1 = renderState.MarginTop,
                    X2 = x,
                    Y2 = renderState.MarginTop + renderState.PlotHeight,
                    Stroke = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                    StrokeThickness = 0.8
                };
                canvas.Children.Add(gridLine);

                TextBlock label = new TextBlock
                {
                    Text = TrendTimeFormatter.FormatAxisTick(
                        value,
                        visibleSpanSeconds,
                        renderState.MaxX),
                    Foreground = Brushes.White,
                    FontSize = 12,
                    Width = 110,
                    TextAlignment = TextAlignment.Center
                };

                Canvas.SetLeft(
                    label,
                    Math.Clamp(x - 55, 0.0, Math.Max(0.0, canvas.ActualWidth - 110)));
                Canvas.SetTop(label, renderState.MarginTop + renderState.PlotHeight + 5);
                canvas.Children.Add(label);
            }
        }

        private static void DrawTimeLabel(Canvas canvas, TrendChartRenderState renderState)
        {
            TextBlock timeText = new TextBlock
            {
                Text = TrendTimeFormatter.GetAxisTitle(
                    renderState.MaxX - renderState.MinX,
                    renderState.MaxX),
                Foreground = Brushes.White,
                FontSize = 14,
                Width = 220,
                TextAlignment = TextAlignment.Center
            };

            Canvas.SetLeft(
                timeText,
                renderState.MarginLeft + renderState.PlotWidth / 2 - 110);
            Canvas.SetTop(timeText, renderState.MarginTop + renderState.PlotHeight + 25);
            canvas.Children.Add(timeText);
        }
    }
}
