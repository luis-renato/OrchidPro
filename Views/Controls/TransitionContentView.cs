// Views/Controls/TransitionContentView.cs
using Microsoft.Maui.Controls;

namespace OrchidPro.Views.Controls;

/// <summary>
/// Custom content view that provides smooth fade transitions between pages
/// </summary>
public class TransitionContentView : ContentView
{
    /// <summary>
    /// Duration of the fade transition in milliseconds
    /// </summary>
    public int TransitionDuration { get; set; } = 250;

    /// <summary>
    /// Performs a fade transition when changing content
    /// </summary>
    public async Task TransitionToAsync(View newContent)
    {
        // Fade out current content
        if (Content != null)
        {
            await Content.FadeTo(0, (uint)(TransitionDuration / 2));
        }

        // Switch content
        Content = newContent;

        // Fade in new content
        if (Content != null)
        {
            Content.Opacity = 0;
            await Content.FadeTo(1, (uint)(TransitionDuration / 2));
        }
    }
}