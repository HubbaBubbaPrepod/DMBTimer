using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Diagnostics.CodeAnalysis;

namespace DMBTimer
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public class HoverContent : ContentControl
    {
        protected override void OnPointerEntered(PointerRoutedEventArgs e)
        {
            base.OnPointerEntered(e);
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
        }

        protected override void OnPointerExited(PointerRoutedEventArgs e)
        {
            base.OnPointerExited(e);
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        }
    }
}