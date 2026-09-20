using DMBTimer.Models;
using DMBTimer.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DMBTimer.Dialogs;

public sealed partial class OnboardingDialog : ContentDialog
{
    private readonly bool _required;

    public OnboardingViewModel ViewModel { get; }
    public OnboardingData? Result { get; private set; }

    public OnboardingDialog(OnboardingViewModel viewModel, bool required, string cancelText)
    {
        ViewModel = viewModel;
        _required = required;
        InitializeComponent();
        DataContext = ViewModel;
        if (!required)
            CloseButtonText = cancelText;
    }

    private void ContentDialog_PrimaryButtonClick(
        ContentDialog sender,
        ContentDialogButtonClickEventArgs args)
    {
        if (ViewModel.CurrentStep < 3)
        {
            args.Cancel = true;
            ViewModel.MoveNext();
            return;
        }

        Result = ViewModel.CreateResult();
    }

    private void ContentDialog_SecondaryButtonClick(
        ContentDialog sender,
        ContentDialogButtonClickEventArgs args)
    {
        args.Cancel = true;
        ViewModel.MoveBack();
    }

    private void ContentDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        if (_required && Result is null && args.Result == ContentDialogResult.None)
            args.Cancel = true;
    }
}
