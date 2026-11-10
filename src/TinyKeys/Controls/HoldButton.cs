using System.Windows;
using System.Windows.Controls;

namespace TinyKeys.Controls;

/// <summary>
/// A keycap-like button with a persistent held state.
/// The held state is managed by the window so that it survives the focus
/// switch to the playback window during a long press.
/// </summary>
public sealed class HoldButton : Button
{
    public static readonly DependencyProperty IsHeldProperty = DependencyProperty.Register(
        nameof(IsHeld),
        typeof(bool),
        typeof(HoldButton),
        new PropertyMetadata(false));

    public bool IsHeld
    {
        get => (bool)GetValue(IsHeldProperty);
        set => SetValue(IsHeldProperty, value);
    }

    /// <summary>Virtual-key code sent by this button.</summary>
    public int VirtualKey { get; set; }
}
