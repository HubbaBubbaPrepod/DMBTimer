using System;
using System.Diagnostics;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace DMBTimer.Services
{
    /// <summary>
    /// Обёртка над AppNotificationManager для показа системных уведомлений.
    /// </summary>
    public static class NotificationService
    {
        public static event Action? NotificationClicked;
        public static event Action<string, string>? LocalNotificationRequested;

        private static bool _registered;
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            try
            {
                if (!AppNotificationManager.IsSupported())
                {
                    Debug.WriteLine("[NotificationService] App notifications are not supported on this OS build.");
                    return;
                }

                AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;
                AppNotificationManager.Default.Register();
                _registered = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NotificationService] Register failed: {ex.Message}");
                _registered = false;
            }
        }

        private static void OnNotificationInvoked(AppNotificationManager sender, AppNotificationActivatedEventArgs args)
        {
            NotificationClicked?.Invoke();
        }

        public static void Show(string title, string message, string? openButtonText = null)
        {
            try
            {
                if (_registered)
                {
                    var builder = new AppNotificationBuilder()
                        .AddArgument("action", "open")
                        .AddText(title)
                        .AddText(message);

                    if (!string.IsNullOrWhiteSpace(openButtonText))
                    {
                        builder.AddButton(new AppNotificationButton(openButtonText)
                            .AddArgument("action", "open"));
                    }

                    var notification = builder.BuildNotification();

                    AppNotificationManager.Default.Show(notification);
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NotificationService] Show failed: {ex.Message}");
            }

            LocalNotificationRequested?.Invoke(title, message);
        }

        public static void Uninitialize()
        {
            if (!_initialized) return;
            try
            {
                AppNotificationManager.Default.NotificationInvoked -= OnNotificationInvoked;
                if (_registered)
                    AppNotificationManager.Default.Unregister();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NotificationService] Unregister failed: {ex.Message}");
            }
            finally
            {
                _registered = false;
                _initialized = false;
            }
        }
    }
}
