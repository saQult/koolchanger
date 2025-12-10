using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace KoolChanger.ClientMvvm.Behaviors
{
    public static class ScrollBehavior
    {
        public static readonly DependencyProperty EnableSmoothScrollProperty =
            DependencyProperty.RegisterAttached(
                "EnableSmoothScroll",
                typeof(bool),
                typeof(ScrollBehavior),
                new PropertyMetadata(false, OnEnableSmoothScrollChanged));

        public static void SetEnableSmoothScroll(DependencyObject element, bool value)
            => element.SetValue(EnableSmoothScrollProperty, value);

        public static bool GetEnableSmoothScroll(DependencyObject element)
            => (bool)element.GetValue(EnableSmoothScrollProperty);

        private static void OnEnableSmoothScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement element)
            {
                if ((bool)e.NewValue)
                    element.PreviewMouseWheel += ElementOnPreviewMouseWheel;
                else
                    element.PreviewMouseWheel -= ElementOnPreviewMouseWheel;
            }
        }

        public static readonly DependencyProperty AnimatedVerticalOffsetProperty =
            DependencyProperty.RegisterAttached(
                "AnimatedVerticalOffset",
                typeof(double),
                typeof(ScrollBehavior),
                new PropertyMetadata(0.0, OnAnimatedVerticalOffsetChanged));

        public static void SetAnimatedVerticalOffset(DependencyObject element, double value)
            => element.SetValue(AnimatedVerticalOffsetProperty, value);

        public static double GetAnimatedVerticalOffset(DependencyObject element)
            => (double)element.GetValue(AnimatedVerticalOffsetProperty);

        private static void OnAnimatedVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer sv)
            {
                sv.ScrollToVerticalOffset((double)e.NewValue);
            }
        }

        private static void ElementOnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not DependencyObject d)
                return;

            var scrollViewer = FindScrollViewer(d);
            if (scrollViewer == null)
                return;

            e.Handled = true;

            double currentOffset = scrollViewer.VerticalOffset;

            double delta = -e.Delta * 0.05;
            double targetOffset = currentOffset + delta;

            targetOffset = Math.Max(0, Math.Min(targetOffset, scrollViewer.ScrollableHeight));

            var animation = new DoubleAnimation
            {
                From = currentOffset,
                To = targetOffset,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            scrollViewer.BeginAnimation(AnimatedVerticalOffsetProperty, animation);
        }

        private static ScrollViewer? FindScrollViewer(DependencyObject d)
        {
            if (d is ScrollViewer sv)
                return sv;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++)
            {
                var child = VisualTreeHelper.GetChild(d, i);
                var result = FindScrollViewer(child);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}

