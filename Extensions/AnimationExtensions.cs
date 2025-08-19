using OrchidPro.Constants;

namespace OrchidPro.Extensions;

/// <summary>
/// ✅ CONVENIENCE: Extension methods para animações usando as constantes centralizadas
/// Simplifica o uso das animações padrão do OrchidPro
/// </summary>
public static class AnimationExtensions
{
    #region 🎬 Page Animations

    /// <summary>
    /// ✅ Animação de entrada padrão para páginas
    /// Substitui: FadeTo(1, 600, Easing.CubicOut), ScaleTo(1, 600, Easing.SpringOut), etc
    /// </summary>
    public static async Task PerformStandardEntranceAsync(this VisualElement element)
    {
        // Setup inicial
        element.Opacity = AnimationConstants.INITIAL_OPACITY;
        element.Scale = AnimationConstants.PAGE_ENTRANCE_INITIAL_SCALE;
        element.TranslationY = AnimationConstants.PAGE_ENTRANCE_TRANSLATION_Y;

        // Animação combinada
        await Task.WhenAll(
            element.FadeTo(
                AnimationConstants.FULL_OPACITY,
                AnimationConstants.PAGE_ENTRANCE_DURATION,
                AnimationConstants.ENTRANCE_EASING),
            element.ScaleTo(
                AnimationConstants.FEEDBACK_SCALE_NORMAL,
                AnimationConstants.PAGE_ENTRANCE_SCALE_DURATION,
                AnimationConstants.ENTRANCE_SCALE_EASING),
            element.TranslateTo(
                0, 0,
                AnimationConstants.PAGE_ENTRANCE_TRANSLATION_DURATION,
                AnimationConstants.ENTRANCE_EASING)
        );
    }

    /// <summary>
    /// ✅ Animação de saída padrão para páginas
    /// Substitui: FadeTo(0.8, 300, Easing.CubicIn), ScaleTo(0.98, 300, Easing.CubicIn), etc
    /// </summary>
    public static async Task PerformStandardExitAsync(this VisualElement element)
    {
        await Task.WhenAll(
            element.FadeTo(
                AnimationConstants.EXIT_OPACITY,
                AnimationConstants.PAGE_EXIT_DURATION,
                AnimationConstants.EXIT_EASING),
            element.ScaleTo(
                AnimationConstants.PAGE_ENTRANCE_INITIAL_SCALE,
                AnimationConstants.PAGE_EXIT_SCALE_DURATION,
                AnimationConstants.EXIT_EASING),
            element.TranslateTo(
                0, AnimationConstants.PAGE_EXIT_TRANSLATION_Y,
                AnimationConstants.PAGE_EXIT_TRANSLATION_DURATION,
                AnimationConstants.EXIT_EASING)
        );
    }

    #endregion

    #region 🔘 FAB Animations

    /// <summary>
    /// ✅ Animação de entrada padrão para FAB
    /// Substitui: FabButton.FadeTo(1, 400, Easing.CubicOut), etc
    /// </summary>
    public static async Task PerformFabEntranceAsync(this VisualElement fabElement, int delay = 0)
    {
        // Setup inicial
        fabElement.Opacity = AnimationConstants.INITIAL_OPACITY;
        fabElement.Scale = AnimationConstants.FAB_ENTRANCE_INITIAL_SCALE;
        fabElement.TranslationY = AnimationConstants.FAB_ENTRANCE_TRANSLATION_Y;

        // Delay opcional
        if (delay > 0)
            await Task.Delay(delay);

        // Animação do FAB
        await Task.WhenAll(
            fabElement.FadeTo(
                AnimationConstants.FULL_OPACITY,
                AnimationConstants.FAB_ENTRANCE_DURATION,
                AnimationConstants.ENTRANCE_EASING),
            fabElement.ScaleTo(
                AnimationConstants.FEEDBACK_SCALE_NORMAL,
                AnimationConstants.FAB_ENTRANCE_DURATION,
                AnimationConstants.SPRING_EASING),
            fabElement.TranslateTo(
                0, 0,
                AnimationConstants.FAB_ENTRANCE_DURATION,
                AnimationConstants.ENTRANCE_EASING)
        );
    }

    /// <summary>
    /// ✅ Animação de saída padrão para FAB
    /// </summary>
    public static async Task PerformFabExitAsync(this VisualElement fabElement)
    {
        await Task.WhenAll(
            fabElement.FadeTo(
                AnimationConstants.INITIAL_OPACITY,
                AnimationConstants.FAB_EXIT_DURATION,
                AnimationConstants.EXIT_EASING),
            fabElement.ScaleTo(
                AnimationConstants.FAB_ENTRANCE_INITIAL_SCALE,
                AnimationConstants.FAB_EXIT_DURATION,
                AnimationConstants.EXIT_EASING)
        );
    }

    #endregion

    #region 👆 Feedback Animations

    /// <summary>
    /// ✅ Animação de feedback para toque (press)
    /// Substitui: ScaleTo(0.9, 100), ScaleTo(1, 100)
    /// </summary>
    public static async Task PerformTapFeedbackAsync(this VisualElement element)
    {
        await element.ScaleTo(
            AnimationConstants.FEEDBACK_SCALE_DOWN,
            AnimationConstants.FEEDBACK_SCALE_DOWN_DURATION,
            AnimationConstants.EXIT_EASING);

        await element.ScaleTo(
            AnimationConstants.FEEDBACK_SCALE_NORMAL,
            AnimationConstants.FEEDBACK_SCALE_UP_DURATION,
            AnimationConstants.FEEDBACK_EASING);
    }

    /// <summary>
    /// ✅ Animação de feedback para foco (Entry, Border, etc)
    /// Substitui: ScaleTo(1.02, 150), mudança de cor
    /// </summary>
    public static async Task PerformFocusFeedbackAsync(this VisualElement element, bool isFocused = true)
    {
        var targetScale = isFocused
            ? AnimationConstants.BORDER_FOCUS_SCALE
            : AnimationConstants.FEEDBACK_SCALE_NORMAL;

        await element.ScaleTo(
            targetScale,
            AnimationConstants.BORDER_FOCUS_DURATION,
            AnimationConstants.FEEDBACK_EASING);
    }

    #endregion

    #region 🏷️ Status Animations

    /// <summary>
    /// ✅ Animação de atualização de status/label
    /// Substitui: FadeTo(0.3, 150), Text = newText, FadeTo(0.8, 150)
    /// </summary>
    public static async Task UpdateTextWithFadeAsync(this Label label, string newText)
    {
        await label.FadeTo(
            AnimationConstants.STATUS_FADE_OPACITY,
            AnimationConstants.STATUS_LABEL_FADE_DURATION);

        label.Text = newText;

        await label.FadeTo(
            AnimationConstants.FULL_OPACITY,
            AnimationConstants.STATUS_LABEL_FADE_DURATION);
    }

    #endregion

    #region 🔄 Loading Animations

    /// <summary>
    /// ✅ Animação de pulse contínuo para loading indicators
    /// Substitui: ScaleTo(1.2, 500), ScaleTo(1.0, 500) em loop
    /// </summary>
    public static async Task StartPulseAnimationAsync(this VisualElement element, CancellationToken cancellationToken = default)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await element.ScaleTo(1.2,
                    AnimationConstants.SPLASH_LOADING_PULSE_DURATION,
                    AnimationConstants.LOADING_PULSE_EASING);

                if (cancellationToken.IsCancellationRequested) break;

                await element.ScaleTo(1.0,
                    AnimationConstants.SPLASH_LOADING_PULSE_DURATION,
                    AnimationConstants.LOADING_PULSE_EASING);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
    }

    #endregion

    #region 🎨 Combined Animations

    /// <summary>
    /// ✅ Animação completa de página: entrada + FAB com delay
    /// Combina page entrance + FAB entrance de forma otimizada
    /// </summary>
    public static async Task PerformCompletePageEntranceAsync(this VisualElement pageElement, VisualElement fabElement)
    {
        // Iniciar animação da página
        var pageTask = pageElement.PerformStandardEntranceAsync();

        // Aguardar página + delay, depois FAB
        await pageTask;
        await fabElement.PerformFabEntranceAsync(AnimationConstants.FAB_ENTRANCE_DELAY);
    }

    /// <summary>
    /// ✅ Animação completa de saída: FAB + página
    /// </summary>
    public static async Task PerformCompletePageExitAsync(this VisualElement pageElement, VisualElement fabElement)
    {
        // Executar simultaneamente
        await Task.WhenAll(
            pageElement.PerformStandardExitAsync(),
            fabElement.PerformFabExitAsync()
        );
    }

    #endregion
}

/// <summary>
/// ✅ CONVENIENCE: Extension methods para validação usando as constantes centralizadas
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// ✅ Valida nome usando constantes centralizadas
    /// </summary>
    public static (bool IsValid, string ErrorMessage) ValidateName(this string name, string entityName)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, string.Format(ValidationConstants.NAME_REQUIRED_TEMPLATE, entityName));
        }

        if (name.Length < ValidationConstants.NAME_OPTIMAL_MIN_LENGTH)
        {
            return (false, string.Format(ValidationConstants.NAME_TOO_SHORT_TEMPLATE, entityName, ValidationConstants.NAME_OPTIMAL_MIN_LENGTH));
        }

        if (name.Length > ValidationConstants.NAME_MAX_LENGTH)
        {
            return (false, string.Format(ValidationConstants.NAME_TOO_LONG_TEMPLATE, entityName, ValidationConstants.NAME_MAX_LENGTH));
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// ✅ Cria timer de debounce usando constantes centralizadas
    /// </summary>
    public static Timer CreateValidationTimer(Func<Task> validationAction)
    {
        return new Timer(async _ => await validationAction(), null,
            ValidationConstants.NAME_VALIDATION_DEBOUNCE_DELAY,
            Timeout.Infinite);
    }
}

/// <summary>
/// ✅ CONVENIENCE: Extension methods para UI usando as constantes centralizadas
/// </summary>
public static class UIExtensions
{
    /// <summary>
    /// ✅ Aplica dimensões padrão do FAB
    /// </summary>
    public static T ConfigureAsFab<T>(this T button) where T : Button
    {
        button.WidthRequest = LayoutConstants.FAB_WIDTH;
        button.HeightRequest = LayoutConstants.FAB_HEIGHT;
        button.CornerRadius = (int)LayoutConstants.FAB_CORNER_RADIUS;
        button.FontSize = LayoutConstants.FAB_FONT_SIZE;
        button.Scale = LayoutConstants.FAB_SCALE;
        return button;
    }

    /// <summary>
    /// ✅ Aplica dimensões padrão de botão primary
    /// </summary>
    public static T ConfigureAsPrimaryButton<T>(this T button) where T : Button
    {
        button.HeightRequest = LayoutConstants.BUTTON_HEIGHT;
        button.CornerRadius = (int)LayoutConstants.BUTTON_CORNER_RADIUS;
        button.FontSize = LayoutConstants.BUTTON_FONT_SIZE;
        return button;
    }

    /// <summary>
    /// ✅ Aplica cor de fallback quando ResourceDictionary não está disponível
    /// </summary>
    public static Color GetColorWithFallback(this ResourceDictionary resources, string key, string fallbackHex)
    {
        if (resources.TryGetValue(key, out var resource) && resource is Color color)
        {
            return color;
        }
        return Color.FromArgb(fallbackHex);
    }
}