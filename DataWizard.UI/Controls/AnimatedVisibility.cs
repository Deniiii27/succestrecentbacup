using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using System;

namespace DataWizard.UI.Controls
{
    /// <summary>
    /// Helper class for smoother visibility transitions
    /// </summary>
    public static class AnimatedVisibility
    {
        public static void SetVisible(UIElement element, double duration = 0.3)
        {
            if (element == null) return;

            // Make sure the element is in the visual tree first
            element.Visibility = Visibility.Visible;
            element.Opacity = 0;

            // Create and start the animation
            var storyboard = new Storyboard();
            var fadeAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromSeconds(duration)),
                EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
            };

            Storyboard.SetTarget(fadeAnimation, element);
            Storyboard.SetTargetProperty(fadeAnimation, "Opacity");

            storyboard.Children.Add(fadeAnimation);
            storyboard.Begin();
        }

        public static void SetCollapsed(UIElement element, double duration = 0.3, bool removeFromTree = true)
        {
            if (element == null) return;

            // Create and start the animation
            var storyboard = new Storyboard();
            var fadeAnimation = new DoubleAnimation
            {
                From = element.Opacity,
                To = 0,
                Duration = new Duration(TimeSpan.FromSeconds(duration)),
                EasingFunction = new CircleEase { EasingMode = EasingMode.EaseIn }
            };

            Storyboard.SetTarget(fadeAnimation, element);
            Storyboard.SetTargetProperty(fadeAnimation, "Opacity");

            // Set the final visibility when the animation completes
            if (removeFromTree)
            {
                storyboard.Completed += (s, e) =>
                {
                    element.Visibility = Visibility.Collapsed;
                };
            }

            storyboard.Children.Add(fadeAnimation);
            storyboard.Begin();
        }
    }
}