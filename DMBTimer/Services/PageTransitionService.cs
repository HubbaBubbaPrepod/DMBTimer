using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DMBTimer.Services;

public sealed class PageTransitionService
{
    private CancellationTokenSource? _transitionCancellation;

    public async Task SwitchAsync(UIElement pageToShow, params UIElement[] pagesToHide)
    {
        _transitionCancellation?.Cancel();
        _transitionCancellation?.Dispose();
        _transitionCancellation = new CancellationTokenSource();
        var token = _transitionCancellation.Token;

        var visiblePages = pagesToHide
            .Where(page => page != pageToShow && page.Visibility == Visibility.Visible)
            .ToArray();

        pageToShow.Visibility = Visibility.Visible;
        var animations = visiblePages
            .Select(page => AnimateAsync(page, page.Opacity, 0, 0, -16, token))
            .Append(AnimateAsync(pageToShow, 0, 1, 20, 0, token));

        try
        {
            await Task.WhenAll(animations);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        foreach (var page in visiblePages)
        {
            page.Visibility = Visibility.Collapsed;
            page.Opacity = 0;
        }

        pageToShow.Opacity = 1;
    }

    private static Task AnimateAsync(
        UIElement element,
        double fromOpacity,
        double toOpacity,
        double fromX,
        double toX,
        CancellationToken token)
    {
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var transform = new TranslateTransform { X = fromX };
        element.RenderTransform = transform;
        element.Opacity = fromOpacity;

        var storyboard = new Storyboard();
        var opacity = new DoubleAnimation
        {
            From = fromOpacity,
            To = toOpacity,
            Duration = TimeSpan.FromMilliseconds(180),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(opacity, element);
        Storyboard.SetTargetProperty(opacity, "Opacity");

        var translation = new DoubleAnimation
        {
            From = fromX,
            To = toX,
            Duration = TimeSpan.FromMilliseconds(220),
            EnableDependentAnimation = true,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(translation, transform);
        Storyboard.SetTargetProperty(translation, "X");

        storyboard.Children.Add(opacity);
        storyboard.Children.Add(translation);
        CancellationTokenRegistration registration = default;
        storyboard.Completed += (_, _) =>
        {
            registration.Dispose();
            completion.TrySetResult(true);
        };

        registration = token.Register(() =>
        {
            storyboard.Stop();
            completion.TrySetCanceled(token);
        });

        storyboard.Begin();
        return completion.Task;
    }
}
