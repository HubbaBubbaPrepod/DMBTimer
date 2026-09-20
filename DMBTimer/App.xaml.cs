using DMBTimer.Services;
using Microsoft.UI.Xaml;
using System;
using Windows.ApplicationModel;

namespace DMBTimer
{
    public partial class App : Application
    {
        private Window? _window;

        public static MainWindow? MainWindowInstance { get; private set; }

        public App()
        {
            InitializeComponent();
           
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            LocalizationService.Initialize();
            NotificationService.Initialize();
            NotificationService.NotificationClicked += OnNotificationClicked;
            NotificationService.LocalNotificationRequested += OnLocalNotificationRequested;

            _window = new MainWindow();
            MainWindowInstance = (MainWindow)_window;
            _window.Activate();
        }

        private void OnNotificationClicked()
        {
            MainWindowInstance?.DispatcherQueue.TryEnqueue(() =>
            {
                MainWindowInstance.BringToFront();
            });
        }

        private void OnLocalNotificationRequested(string title, string message)
        {
            MainWindowInstance?.DispatcherQueue.TryEnqueue(() =>
            {
                MainWindowInstance.ShowNotification(title, message);
            });
        }
    }
}
