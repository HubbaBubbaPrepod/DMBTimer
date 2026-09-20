using DMBTimer.Models;
using DMBTimer.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace DMBTimer.Dialogs;

public sealed partial class CustomMilestoneDialog : ContentDialog
{
    public CustomMilestoneEditorViewModel ViewModel { get; }
    public CustomMilestone? Result { get; private set; }

    public CustomMilestoneDialog(CustomMilestoneEditorViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = ViewModel;
    }

    private void ContentDialog_PrimaryButtonClick(
        ContentDialog sender,
        ContentDialogButtonClickEventArgs args)
    {
        if (!ViewModel.CanSave)
        {
            args.Cancel = true;
            return;
        }

        Result = ViewModel.CreateResult();
    }
}
