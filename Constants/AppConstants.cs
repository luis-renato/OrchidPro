namespace OrchidPro.Constants;

/// <summary>
/// ✅ CENTRALIZAÇÃO: Constantes de animação do OrchidPro
/// Extraídas de FamiliesListPage.xaml.cs, FamilyEditPage.xaml.cs, SplashPage.xaml.cs
/// </summary>
public static class AnimationConstants
{
    #region ⏱️ Durações (em milliseconds)

    // ✅ Animações de entrada de páginas
    public const int PAGE_ENTRANCE_DURATION = 600;
    public const int PAGE_ENTRANCE_SCALE_DURATION = 600;
    public const int PAGE_ENTRANCE_TRANSLATION_DURATION = 600;

    // ✅ Animações de saída de páginas  
    public const int PAGE_EXIT_DURATION = 300;
    public const int PAGE_EXIT_SCALE_DURATION = 300;
    public const int PAGE_EXIT_TRANSLATION_DURATION = 300;

    // ✅ Animações do FAB
    public const int FAB_ENTRANCE_DURATION = 400;
    public const int FAB_ENTRANCE_DELAY = 200;
    public const int FAB_EXIT_DURATION = 200;
    public const int FAB_FEEDBACK_DURATION = 100;

    // ✅ Animações de feedback (clicks, taps)
    public const int FEEDBACK_SCALE_DOWN_DURATION = 100;
    public const int FEEDBACK_SCALE_UP_DURATION = 100;
    public const int FEEDBACK_SCALE_FAST_DURATION = 50;

    // ✅ Animações de elementos (borders, search, etc)
    public const int BORDER_FOCUS_DURATION = 150;
    public const int SEARCH_FOCUS_DURATION = 150;
    public const int STATUS_LABEL_FADE_DURATION = 150;

    // ✅ Animações do Splash
    public const int SPLASH_FADE_IN_DURATION = 500;
    public const int SPLASH_LOGO_SCALE_DURATION = 600;
    public const int SPLASH_LOGO_FADE_DURATION = 300;
    public const int SPLASH_LOADING_PULSE_DURATION = 500;

    #endregion

    #region 📐 Escalas e Transformações

    // ✅ Escalas iniciais para animações de entrada
    public const double PAGE_ENTRANCE_INITIAL_SCALE = 0.95;
    public const double FAB_ENTRANCE_INITIAL_SCALE = 0.8;
    public const double SPLASH_LOGO_INITIAL_SCALE = 0.8;

    // ✅ Escalas de feedback (clicks)
    public const double FEEDBACK_SCALE_DOWN = 0.9;
    public const double FEEDBACK_SCALE_NORMAL = 1.0;
    public const double BORDER_FOCUS_SCALE = 1.02;

    // ✅ Translations iniciais
    public const double PAGE_ENTRANCE_TRANSLATION_Y = 30;
    public const double FAB_ENTRANCE_TRANSLATION_Y = 50;
    public const double PAGE_EXIT_TRANSLATION_Y = -20;

    // ✅ Opacidades
    public const double INITIAL_OPACITY = 0;
    public const double FULL_OPACITY = 1.0;
    public const double EXIT_OPACITY = 0.8;
    public const double STATUS_FADE_OPACITY = 0.3;

    #endregion

    #region 🎯 Easing Curves

    // ✅ Curves de entrada (suaves)
    public static readonly Easing ENTRANCE_EASING = Easing.CubicOut;
    public static readonly Easing ENTRANCE_SCALE_EASING = Easing.SpringOut;

    // ✅ Curves de saída (rápidas)
    public static readonly Easing EXIT_EASING = Easing.CubicIn;

    // ✅ Curves de feedback (responsivas)
    public static readonly Easing FEEDBACK_EASING = Easing.CubicOut;

    // ✅ Curves especiais
    public static readonly Easing SPRING_EASING = Easing.SpringOut;
    public static readonly Easing LOADING_PULSE_EASING = Easing.SinInOut;

    #endregion
}

/// <summary>
/// ✅ CENTRALIZAÇÃO: Constantes de validação do OrchidPro
/// Extraídas de BaseEditViewModel.cs
/// </summary>
public static class ValidationConstants
{
    #region ⏳ Delays e Timeouts

    // ✅ Debounce para validação de nome (800ms padrão)
    public const int NAME_VALIDATION_DEBOUNCE_DELAY = 800;

    // ✅ Timeout para operações assíncronas
    public const int VALIDATION_TIMEOUT = 5000;

    #endregion

    #region 📏 Limites de Tamanho

    // ✅ Nome - limites de caracteres
    public const int NAME_MIN_LENGTH = 1;
    public const int NAME_MAX_LENGTH = 255;
    public const int NAME_OPTIMAL_MIN_LENGTH = 2;

    // ✅ Descrição - limites de caracteres
    public const int DESCRIPTION_MIN_LENGTH = 0;
    public const int DESCRIPTION_MAX_LENGTH = 2000;

    #endregion

    #region 📝 Mensagens de Validação

    // ✅ Templates de mensagens de erro genéricas
    public const string NAME_REQUIRED_TEMPLATE = "{0} name is required";
    public const string NAME_TOO_SHORT_TEMPLATE = "{0} name must be at least {1} characters";
    public const string NAME_TOO_LONG_TEMPLATE = "{0} name cannot exceed {1} characters";
    public const string NAME_DUPLICATE_TEMPLATE = "A {0} with this name already exists";
    public const string VALIDATION_ERROR_GENERIC = "Validation error";

    #endregion
}

/// <summary>
/// ✅ CENTRALIZAÇÃO: Constantes de layout e UI do OrchidPro
/// Extraídas dos templates XAML e páginas
/// </summary>
public static class LayoutConstants
{
    #region 📱 Dimensões de Componentes

    // ✅ FAB (Floating Action Button)
    public const double FAB_WIDTH = 160;
    public const double FAB_HEIGHT = 56;
    public const double FAB_CORNER_RADIUS = 28;
    public const double FAB_SCALE = 0.9;

    // ✅ Botões padrão
    public const double BUTTON_HEIGHT = 48;
    public const double BUTTON_CORNER_RADIUS = 24;

    // ✅ Form frames
    public const double FORM_FRAME_CORNER_RADIUS = 12;
    public const double FORM_FRAME_PADDING = 16;

    // ✅ Loading overlay frame
    public const double LOADING_FRAME_CORNER_RADIUS = 16;
    public const double LOADING_FRAME_PADDING = 32;

    // ✅ Search e action buttons
    public const double SEARCH_ACTION_BUTTON_SIZE = 44;
    public const double SEARCH_ACTION_BUTTON_CORNER_RADIUS = 22;

    // ✅ Connection status
    public const double CONNECTION_STATUS_CORNER_RADIUS = 10;

    #endregion

    #region 📏 Spacings e Margins

    // ✅ Spacings padrão
    public const double FORM_FIELD_SPACING = 8;
    public const double FORM_SETTINGS_SPACING = 16;
    public const double EMPTY_STATE_SPACING = 20;
    public const double LOADING_OVERLAY_SPACING = 16;

    // ✅ Margins do FAB
    public const double FAB_MARGIN_HORIZONTAL = 20;
    public const double FAB_MARGIN_VERTICAL = 20;
    public const double FAB_MARGIN_RIGHT = 30;
    public const double FAB_MARGIN_BOTTOM = 30;

    // ✅ Paddings padrão
    public const double EMPTY_STATE_PADDING = 40;
    public const double SEARCH_CONTAINER_PADDING = 20;
    public const double STATUS_HEADER_PADDING_HORIZONTAL = 20;
    public const double STATUS_HEADER_PADDING_VERTICAL = 15;

    #endregion

    #region 🎨 Tamanhos de Fonte

    // ✅ Form labels e campos
    public const double FORM_LABEL_FONT_SIZE = 12;
    public const double FORM_FIELD_FONT_SIZE = 16;
    public const double FORM_ERROR_FONT_SIZE = 12;

    // ✅ Botões
    public const double BUTTON_FONT_SIZE = 16;
    public const double FAB_FONT_SIZE = 14;

    // ✅ Empty state
    public const double EMPTY_STATE_ICON_FONT_SIZE = 64;
    public const double EMPTY_STATE_TITLE_FONT_SIZE = 18;
    public const double EMPTY_STATE_SUBTITLE_FONT_SIZE = 14;

    // ✅ Loading overlay
    public const double LOADING_MESSAGE_FONT_SIZE = 16;
    public const double LOADING_ICON_FONT_SIZE = 24;

    // ✅ Statistics e status
    public const double STATISTICS_FONT_SIZE = 13;
    public const double CONNECTION_STATUS_FONT_SIZE = 10;

    // ✅ Search action icons
    public const double SEARCH_ACTION_ICON_FONT_SIZE = 16;

    #endregion

    #region 📐 Dimensões de Loading

    // ✅ SfBusyIndicator
    public const double BUSY_INDICATOR_SIZE = 50;

    #endregion
}

/// <summary>
/// ✅ CENTRALIZAÇÃO: Constantes de cores em hex do OrchidPro
/// Para casos onde cores dinâmicas não estão disponíveis
/// </summary>
public static class ColorConstants
{
    #region 🎨 Cores Principais (fallbacks)

    // ✅ Cor primária do app (Mocha Mousse - Pantone 2025)
    public const string PRIMARY_COLOR = "#A47764";
    public const string PRIMARY_DARK_COLOR = "#7A564A";

    // ✅ Cores de feedback
    public const string ERROR_COLOR = "#D32F2F";
    public const string SUCCESS_COLOR = "#4CAF50";
    public const string WARNING_COLOR = "#FF9800";
    public const string INFO_COLOR = "#2196F3";

    // ✅ Cores de texto e bordes
    public const string GRAY_300 = "#E0E0E0";
    public const string GRAY_500 = "#9E9E9E";
    public const string GRAY_600 = "#757575";

    // ✅ Overlay colors
    public const string LOADING_OVERLAY_BACKGROUND = "#80000000"; // 50% black

    #endregion

    #region 🌈 Connection Status Colors

    public static readonly Color CONNECTED_COLOR = Colors.Green;
    public static readonly Color DISCONNECTED_COLOR = Colors.Red;
    public static readonly Color CONNECTING_COLOR = Colors.Orange;

    #endregion
}

/// <summary>
/// ✅ CENTRALIZAÇÃO: Constantes de texto padrão do OrchidPro
/// </summary>
public static class TextConstants
{
    #region 📝 Mensagens de Loading

    public const string LOADING_FAMILIES = "Loading families...";
    public const string LOADING_GENERA = "Loading genera...";
    public const string LOADING_SPECIES = "Loading species...";
    public const string LOADING_DEFAULT = "Loading...";
    public const string SAVING_DEFAULT = "Saving...";

    #endregion

    #region 📄 Empty State Messages

    public const string EMPTY_FAMILIES = "No families found";
    public const string EMPTY_GENERA = "No genera found";
    public const string EMPTY_SPECIES = "No species found";
    public const string EMPTY_DEFAULT = "No items found";
    public const string EMPTY_SEARCH_HINT = "Try adjusting your search or filters";

    #endregion

    #region 🔘 Button Texts

    public const string ADD_FAMILY = "Add Family";
    public const string ADD_GENUS = "Add Genus";
    public const string ADD_SPECIES = "Add Species";
    public const string SAVE_CHANGES = "Save Changes";
    public const string CANCEL_CHANGES = "Cancel";
    public const string DELETE_ITEM = "Delete";
    public const string DELETE_SELECTED = "Delete Selected";

    #endregion

    #region 🌐 Connection Status

    public const string STATUS_CONNECTED = "Connected";
    public const string STATUS_DISCONNECTED = "Offline";
    public const string STATUS_CONNECTING = "Connecting...";
    public const string STATUS_SYNCING = "Syncing...";

    #endregion

    #region 🎭 Emojis e Ícones (text)

    public const string ICON_ORCHID = "🌿";
    public const string ICON_FAMILY = "📄";
    public const string ICON_GENUS = "🌱";
    public const string ICON_SPECIES = "🌺";
    public const string ICON_SAVE = "💾";
    public const string ICON_LOADING = "⏳";

    #endregion
}

/// <summary>
/// ✅ CENTRALIZAÇÃO: Constantes para sistema de notificações do OrchidPro
/// </summary>
public static class NotificationConstants
{
    #region 🍞 Toast Configuration

    // ✅ Configuração de toasts
    public const int TOAST_FONT_SIZE = 16;
    public const int TOAST_SUCCESS_DURATION_MS = 2000;
    public const int TOAST_ERROR_DURATION_MS = 4000;
    public const int TOAST_INFO_DURATION_MS = 3000;

    #endregion

    #region 🎨 Ícones de Notificação

    // ✅ Ícones padronizados para toasts e logs
    public const string ICON_SUCCESS = "✅";
    public const string ICON_ERROR = "❌";
    public const string ICON_INFO = "ℹ️";
    public const string ICON_WARNING = "⚠️";
    public const string ICON_DELETE = "🗑️";
    public const string ICON_FAVORITE = "⭐";
    public const string ICON_SYNC = "🔄";
    public const string ICON_OFFLINE = "📵";
    public const string ICON_LOADING = "⏳";

    #endregion

    #region 📝 Templates de Mensagem

    // ✅ Templates para ações CRUD
    public const string CRUD_SUCCESS_TEMPLATE = "{0} {1} successfully";
    public const string CRUD_ERROR_TEMPLATE = "Failed to {0} {1}";
    public const string SYNC_SUCCESS_TEMPLATE = "Synced {0} items successfully";
    public const string SYNC_ERROR_TEMPLATE = "Sync failed. Please try again.";
    public const string FILTER_APPLIED_TEMPLATE = "Filter applied: {0} ({1} results)";

    #endregion
}

/// <summary>
/// ✅ CENTRALIZAÇÃO: Constantes para logging padronizado do OrchidPro
/// </summary>
public static class LoggingConstants
{
    #region 📋 Templates de Log

    // ✅ Templates para logs estruturados
    public const string LOG_FORMAT_SUCCESS = "✅ [{0}] {1}";
    public const string LOG_FORMAT_ERROR = "❌ [{0}] {1}";
    public const string LOG_FORMAT_INFO = "ℹ️ [{0}] {1}";
    public const string LOG_FORMAT_WARNING = "⚠️ [{0}] {1}";
    public const string LOG_FORMAT_DEBUG = "🔧 [{0}] {1}";

    #endregion

    #region 🏷️ Categorias de Log

    // ✅ Categorias padronizadas
    public const string CATEGORY_ANIMATION = "ANIMATION";
    public const string CATEGORY_NAVIGATION = "NAVIGATION";
    public const string CATEGORY_DATA = "DATA";
    public const string CATEGORY_UI = "UI";
    public const string CATEGORY_SYNC = "SYNC";
    public const string CATEGORY_VALIDATION = "VALIDATION";
    public const string CATEGORY_COMMAND = "COMMAND";

    #endregion
}
public static class PerformanceConstants
{
    #region ⚡ Cache e Performance

    // ✅ Tamanhos de cache
    public const int DEFAULT_CACHE_SIZE = 100;
    public const int LARGE_CACHE_SIZE = 500;

    // ✅ Timeouts de operações
    public const int DEFAULT_OPERATION_TIMEOUT = 30000; // 30s
    public const int QUICK_OPERATION_TIMEOUT = 5000;    // 5s
    public const int SYNC_OPERATION_TIMEOUT = 60000;    // 60s

    // ✅ Delays mínimos para UX
    public const int MIN_LOADING_DISPLAY_TIME = 500;    // 500ms mínimo para mostrar loading
    public const int NAVIGATION_TRANSITION_DELAY = 100; // 100ms para transições

    #endregion

    #region 📊 Pagination e Lists

    // ✅ Tamanhos de página para listas grandes
    public const int DEFAULT_PAGE_SIZE = 50;
    public const int SMALL_PAGE_SIZE = 25;
    public const int LARGE_PAGE_SIZE = 100;

    // ✅ Limites de busca
    public const int SEARCH_MIN_CHARACTERS = 1;
    public const int SEARCH_DEBOUNCE_MS = 300;

    #endregion
}