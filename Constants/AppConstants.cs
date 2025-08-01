namespace OrchidPro.Constants;

/// <summary>
/// Centralized animation constants for consistent visual transitions throughout OrchidPro.
/// Extracted from UI pages and standardized for enterprise-grade user experience.
/// </summary>
public static class AnimationConstants
{
    #region Duration Constants (in milliseconds)

    // Page entrance animations
    public const int PAGE_ENTRANCE_DURATION = 600;
    public const int PAGE_ENTRANCE_SCALE_DURATION = 600;
    public const int PAGE_ENTRANCE_TRANSLATION_DURATION = 600;

    // Page exit animations  
    public const int PAGE_EXIT_DURATION = 300;
    public const int PAGE_EXIT_SCALE_DURATION = 300;
    public const int PAGE_EXIT_TRANSLATION_DURATION = 300;

    // FAB (Floating Action Button) animations
    public const int FAB_ENTRANCE_DURATION = 400;
    public const int FAB_ENTRANCE_DELAY = 200;
    public const int FAB_EXIT_DURATION = 200;
    public const int FAB_FEEDBACK_DURATION = 100;

    // User feedback animations (clicks, taps)
    public const int FEEDBACK_SCALE_DOWN_DURATION = 100;
    public const int FEEDBACK_SCALE_UP_DURATION = 100;
    public const int FEEDBACK_SCALE_FAST_DURATION = 50;

    // UI element animations (borders, search, etc)
    public const int BORDER_FOCUS_DURATION = 150;
    public const int SEARCH_FOCUS_DURATION = 150;
    public const int STATUS_LABEL_FADE_DURATION = 150;

    // Splash screen animations
    public const int SPLASH_FADE_IN_DURATION = 500;
    public const int SPLASH_LOGO_SCALE_DURATION = 600;
    public const int SPLASH_LOGO_FADE_DURATION = 300;
    public const int SPLASH_LOADING_PULSE_DURATION = 500;

    #endregion

    #region Scale and Transform Constants

    // Initial scales for entrance animations
    public const double PAGE_ENTRANCE_INITIAL_SCALE = 0.95;
    public const double FAB_ENTRANCE_INITIAL_SCALE = 0.8;
    public const double SPLASH_LOGO_INITIAL_SCALE = 0.8;

    // Feedback scales (clicks)
    public const double FEEDBACK_SCALE_DOWN = 0.9;
    public const double FEEDBACK_SCALE_NORMAL = 1.0;
    public const double BORDER_FOCUS_SCALE = 1.02;

    // Initial translations
    public const double PAGE_ENTRANCE_TRANSLATION_Y = 30;
    public const double FAB_ENTRANCE_TRANSLATION_Y = 50;
    public const double PAGE_EXIT_TRANSLATION_Y = -20;

    // Opacity values
    public const double INITIAL_OPACITY = 0;
    public const double FULL_OPACITY = 1.0;
    public const double EXIT_OPACITY = 0.8;
    public const double STATUS_FADE_OPACITY = 0.3;

    #endregion

    #region Easing Curves

    // Smooth entrance curves
    public static readonly Easing ENTRANCE_EASING = Easing.CubicOut;
    public static readonly Easing ENTRANCE_SCALE_EASING = Easing.SpringOut;

    // Quick exit curves
    public static readonly Easing EXIT_EASING = Easing.CubicIn;

    // Responsive feedback curves
    public static readonly Easing FEEDBACK_EASING = Easing.CubicOut;

    // Special effect curves
    public static readonly Easing SPRING_EASING = Easing.SpringOut;
    public static readonly Easing LOADING_PULSE_EASING = Easing.SinInOut;

    #endregion
}

/// <summary>
/// Validation constants for form fields and user input validation.
/// Extracted from BaseEditViewModel for consistent validation across the application.
/// </summary>
public static class ValidationConstants
{
    #region Timing and Timeouts

    // Debounce delay for name validation (800ms default)
    public const int NAME_VALIDATION_DEBOUNCE_DELAY = 800;

    // Timeout for asynchronous validation operations
    public const int VALIDATION_TIMEOUT = 5000;

    #endregion

    #region Size Limits

    // Name field character limits
    public const int NAME_MIN_LENGTH = 1;
    public const int NAME_MAX_LENGTH = 255;
    public const int NAME_OPTIMAL_MIN_LENGTH = 2;

    // Description field character limits
    public const int DESCRIPTION_MIN_LENGTH = 0;
    public const int DESCRIPTION_MAX_LENGTH = 2000;

    #endregion

    #region Validation Message Templates

    // Generic error message templates
    public const string NAME_REQUIRED_TEMPLATE = "{0} name is required";
    public const string NAME_TOO_SHORT_TEMPLATE = "{0} name must be at least {1} characters";
    public const string NAME_TOO_LONG_TEMPLATE = "{0} name cannot exceed {1} characters";
    public const string NAME_DUPLICATE_TEMPLATE = "A {0} with this name already exists";
    public const string VALIDATION_ERROR_GENERIC = "Validation error";

    #endregion
}

/// <summary>
/// Layout and UI dimension constants for consistent visual design.
/// Extracted from XAML templates and pages for standardized spacing and sizing.
/// </summary>
public static class LayoutConstants
{
    #region Component Dimensions

    // FAB (Floating Action Button)
    public const double FAB_WIDTH = 160;
    public const double FAB_HEIGHT = 56;
    public const double FAB_CORNER_RADIUS = 28;
    public const double FAB_SCALE = 0.9;

    // Standard buttons
    public const double BUTTON_HEIGHT = 48;
    public const double BUTTON_CORNER_RADIUS = 24;

    // Form frames
    public const double FORM_FRAME_CORNER_RADIUS = 12;
    public const double FORM_FRAME_PADDING = 16;

    // Loading overlay frame
    public const double LOADING_FRAME_CORNER_RADIUS = 16;
    public const double LOADING_FRAME_PADDING = 32;

    // Search and action buttons
    public const double SEARCH_ACTION_BUTTON_SIZE = 44;
    public const double SEARCH_ACTION_BUTTON_CORNER_RADIUS = 22;

    // Connection status
    public const double CONNECTION_STATUS_CORNER_RADIUS = 10;

    #endregion

    #region Spacing and Margins

    // Standard spacings
    public const double FORM_FIELD_SPACING = 8;
    public const double FORM_SETTINGS_SPACING = 16;
    public const double EMPTY_STATE_SPACING = 20;
    public const double LOADING_OVERLAY_SPACING = 16;

    // FAB margins
    public const double FAB_MARGIN_HORIZONTAL = 20;
    public const double FAB_MARGIN_VERTICAL = 20;
    public const double FAB_MARGIN_RIGHT = 30;
    public const double FAB_MARGIN_BOTTOM = 30;

    // Standard paddings
    public const double EMPTY_STATE_PADDING = 40;
    public const double SEARCH_CONTAINER_PADDING = 20;
    public const double STATUS_HEADER_PADDING_HORIZONTAL = 20;
    public const double STATUS_HEADER_PADDING_VERTICAL = 15;

    #endregion

    #region Font Sizes

    // Form labels and fields
    public const double FORM_LABEL_FONT_SIZE = 12;
    public const double FORM_FIELD_FONT_SIZE = 16;
    public const double FORM_ERROR_FONT_SIZE = 12;

    // Buttons
    public const double BUTTON_FONT_SIZE = 16;
    public const double FAB_FONT_SIZE = 14;

    // Empty state
    public const double EMPTY_STATE_ICON_FONT_SIZE = 64;
    public const double EMPTY_STATE_TITLE_FONT_SIZE = 18;
    public const double EMPTY_STATE_SUBTITLE_FONT_SIZE = 14;

    // Loading overlay
    public const double LOADING_MESSAGE_FONT_SIZE = 16;
    public const double LOADING_ICON_FONT_SIZE = 24;

    // Statistics and status
    public const double STATISTICS_FONT_SIZE = 13;
    public const double CONNECTION_STATUS_FONT_SIZE = 10;

    // Search action icons
    public const double SEARCH_ACTION_ICON_FONT_SIZE = 16;

    #endregion

    #region Loading Dimensions

    // SfBusyIndicator
    public const double BUSY_INDICATOR_SIZE = 50;

    #endregion
}

/// <summary>
/// Color constants in hex format for fallback scenarios when dynamic colors are unavailable.
/// Provides consistent color scheme throughout the OrchidPro application.
/// </summary>
public static class ColorConstants
{
    #region Primary Colors (Fallbacks)

    // Main app color (Mocha Mousse - Pantone 2025)
    public const string PRIMARY_COLOR = "#A47764";
    public const string PRIMARY_DARK_COLOR = "#7A564A";

    // Feedback colors
    public const string ERROR_COLOR = "#D32F2F";
    public const string SUCCESS_COLOR = "#4CAF50";
    public const string WARNING_COLOR = "#FF9800";
    public const string INFO_COLOR = "#2196F3";

    // Text and border colors
    public const string GRAY_300 = "#E0E0E0";
    public const string GRAY_500 = "#9E9E9E";
    public const string GRAY_600 = "#757575";

    // Overlay colors
    public const string LOADING_OVERLAY_BACKGROUND = "#80000000"; // 50% black

    #endregion

    #region Connection Status Colors

    public static readonly Color CONNECTED_COLOR = Colors.Green;
    public static readonly Color DISCONNECTED_COLOR = Colors.Red;
    public static readonly Color CONNECTING_COLOR = Colors.Orange;

    #endregion
}

/// <summary>
/// Standard text constants for consistent messaging throughout OrchidPro.
/// Expanded to support error handling and user feedback systems.
/// </summary>
public static class TextConstants
{
    #region Loading Messages

    public const string LOADING_FAMILIES = "Loading families...";
    public const string LOADING_GENERA = "Loading genera...";
    public const string LOADING_SPECIES = "Loading species...";
    public const string LOADING_DEFAULT = "Loading...";
    public const string SAVING_DEFAULT = "Saving...";

    #endregion

    #region Empty State Messages

    public const string EMPTY_FAMILIES = "No families found";
    public const string EMPTY_GENERA = "No genera found";
    public const string EMPTY_SPECIES = "No species found";
    public const string EMPTY_DEFAULT = "No items found";
    public const string EMPTY_SEARCH_HINT = "Try adjusting your search or filters";

    #endregion

    #region Button Text

    public const string ADD_FAMILY = "Add Family";
    public const string ADD_GENUS = "Add Genus";
    public const string ADD_SPECIES = "Add Species";
    public const string SAVE_CHANGES = "Save Changes";
    public const string CANCEL_CHANGES = "Cancel";
    public const string DELETE_ITEM = "Delete";
    public const string DELETE_SELECTED = "Delete Selected";

    #endregion

    #region Connection Status

    public const string STATUS_CONNECTED = "Connected";
    public const string STATUS_DISCONNECTED = "Offline";
    public const string STATUS_CONNECTING = "Connecting...";
    public const string STATUS_SYNCING = "Syncing...";

    #endregion

    #region Operation Status for Error Handling

    // Operation status messages
    public const string STATUS_LOADING = "Loading...";
    public const string STATUS_SAVING = "Saving...";
    public const string STATUS_RETRYING = "Retrying...";
    public const string STATUS_FAILED = "Failed";
    public const string STATUS_CANCELLED = "Cancelled";
    public const string STATUS_TIMEOUT = "Timeout";
    public const string STATUS_VALIDATING = "Validating...";
    public const string STATUS_PROCESSING = "Processing...";

    // User action prompts
    public const string ACTION_RETRY = "Retry";
    public const string ACTION_CANCEL = "Cancel";
    public const string ACTION_IGNORE = "Ignore";
    public const string ACTION_DISMISS = "Dismiss";

    #endregion

    #region Emoji and Text Icons

    public const string ICON_ORCHID = "🌿";
    public const string ICON_FAMILY = "📄";
    public const string ICON_GENUS = "🌱";
    public const string ICON_SPECIES = "🌺";
    public const string ICON_SAVE = "💾";
    public const string ICON_LOADING = "⏳";

    #endregion
}

/// <summary>
/// Notification system constants for consistent user feedback and messaging.
/// Centralizes toast and dialog configuration for the entire application.
/// </summary>
public static class NotificationConstants
{
    #region Toast Configuration

    // Toast display settings
    public const int TOAST_FONT_SIZE = 16;
    public const int TOAST_SUCCESS_DURATION_MS = 2000;
    public const int TOAST_ERROR_DURATION_MS = 4000;
    public const int TOAST_INFO_DURATION_MS = 3000;

    #endregion

    #region Notification Icons

    // Standardized icons for toasts and logs
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

    #region Message Templates

    // Templates for CRUD operations
    public const string CRUD_SUCCESS_TEMPLATE = "{0} {1} successfully";
    public const string CRUD_ERROR_TEMPLATE = "Failed to {0} {1}";
    public const string SYNC_SUCCESS_TEMPLATE = "Synced {0} items successfully";
    public const string SYNC_ERROR_TEMPLATE = "Sync failed. Please try again.";
    public const string FILTER_APPLIED_TEMPLATE = "Filter applied: {0} ({1} results)";

    #endregion
}

/// <summary>
/// Logging constants for standardized debugging and monitoring throughout OrchidPro.
/// Expanded to support LoggingExtensions with structured categorization.
/// </summary>
public static class LoggingConstants
{
    #region Structured Log Templates

    // Templates for structured logs with categorization
    public const string LOG_FORMAT_SUCCESS = "✅ [{0}] {1}";
    public const string LOG_FORMAT_ERROR = "❌ [{0}] {1}";
    public const string LOG_FORMAT_INFO = "ℹ️ [{0}] {1}";
    public const string LOG_FORMAT_WARNING = "⚠️ [{0}] {1}";
    public const string LOG_FORMAT_DEBUG = "🔧 [{0}] {1}";
    public const string LOG_FORMAT_PERFORMANCE = "⚡ [{0}] {1}";

    #endregion

    #region Expanded Log Categories

    // Main categories (maintained)
    public const string CATEGORY_ANIMATION = "ANIMATION";
    public const string CATEGORY_NAVIGATION = "NAVIGATION";
    public const string CATEGORY_DATA = "DATA";
    public const string CATEGORY_UI = "UI";
    public const string CATEGORY_SYNC = "SYNC";
    public const string CATEGORY_VALIDATION = "VALIDATION";
    public const string CATEGORY_COMMAND = "COMMAND";

    // New categories for extensions
    public const string CATEGORY_NETWORK = "NETWORK";
    public const string CATEGORY_AUTH = "AUTH";
    public const string CATEGORY_PERFORMANCE = "PERFORMANCE";
    public const string CATEGORY_ERROR = "ERROR";
    public const string CATEGORY_REPOSITORY = "REPOSITORY";
    public const string CATEGORY_SERVICE = "SERVICE";

    #endregion

    #region Operation Type Prefixes

    // For quick identification of log types
    public const string PREFIX_OPERATION_START = "🚀";
    public const string PREFIX_OPERATION_SUCCESS = "✅";
    public const string PREFIX_OPERATION_FAILURE = "❌";
    public const string PREFIX_METHOD_ENTRY = "🔄";
    public const string PREFIX_METHOD_EXIT = "🏁";
    public const string PREFIX_NAVIGATION = "🧭";
    public const string PREFIX_DATA_OP = "💾";
    public const string PREFIX_ANIMATION = "🎨";
    public const string PREFIX_CONNECTIVITY = "🔌";
    public const string PREFIX_RETRY = "🔄";
    public const string PREFIX_DISPOSAL = "🗑️";

    #endregion
}

/// <summary>
/// Performance monitoring constants for optimized application behavior.
/// Expanded with configurations for extensions and advanced performance tracking.
/// </summary>
public static class PerformanceConstants
{
    #region Cache and Performance (Existing)

    // Cache sizes
    public const int DEFAULT_CACHE_SIZE = 100;
    public const int LARGE_CACHE_SIZE = 500;

    // Operation timeouts
    public const int DEFAULT_OPERATION_TIMEOUT = 30000; // 30s
    public const int QUICK_OPERATION_TIMEOUT = 5000;    // 5s
    public const int SYNC_OPERATION_TIMEOUT = 60000;    // 60s

    // Minimum UX delays
    public const int MIN_LOADING_DISPLAY_TIME = 500;    // 500ms minimum for loading display
    public const int NAVIGATION_TRANSITION_DELAY = 100; // 100ms for transitions

    #endregion

    #region Pagination and Lists (Existing)

    // Page sizes for large lists
    public const int DEFAULT_PAGE_SIZE = 50;
    public const int SMALL_PAGE_SIZE = 25;
    public const int LARGE_PAGE_SIZE = 100;

    // Search limits
    public const int SEARCH_MIN_CHARACTERS = 1;
    public const int SEARCH_DEBOUNCE_MS = 300;

    #endregion

    #region Extension Configurations

    // Performance logging thresholds
    public const int PERFORMANCE_LOG_THRESHOLD_MS = 100;     // Log if > 100ms
    public const int SLOW_OPERATION_THRESHOLD_MS = 1000;     // Warning if > 1s
    public const int CRITICAL_OPERATION_THRESHOLD_MS = 5000; // Error if > 5s

    // Debounce configurations
    public const int VALIDATION_DEBOUNCE_MS = 300;
    public const int UI_UPDATE_DEBOUNCE_MS = 100;

    // Batching and bulk operations
    public const int BULK_OPERATION_BATCH_SIZE = 50;
    public const int SYNC_BATCH_SIZE = 25;
    public const int UI_UPDATE_BATCH_SIZE = 10;

    #endregion
}

/// <summary>
/// Error handling constants for standardized error processing throughout the application.
/// Provides consistent retry logic, timeout configuration, and error messaging.
/// </summary>
public static class ErrorHandlingConstants
{
    #region Retry Configurations

    // Default retry settings
    public const int DEFAULT_MAX_RETRIES = 3;
    public const int NETWORK_MAX_RETRIES = 3;
    public const int AUTH_MAX_RETRIES = 2;
    public const int DATA_MAX_RETRIES = 3;

    // Delays between retry attempts
    public const int DEFAULT_RETRY_DELAY_MS = 1000;
    public const int NETWORK_RETRY_DELAY_MS = 2000;
    public const int AUTH_RETRY_DELAY_MS = 3000;
    public const int EXPONENTIAL_BACKOFF_BASE = 2;

    #endregion

    #region Standardized Error Messages

    // Messages by error type
    public const string ERROR_NETWORK = "Network connection error. Please check your internet connection.";
    public const string ERROR_TIMEOUT = "Operation timed out. Please try again.";
    public const string ERROR_UNAUTHORIZED = "You are not authorized to perform this operation.";
    public const string ERROR_VALIDATION = "Validation error occurred.";
    public const string ERROR_NOT_FOUND = "The requested resource was not found.";
    public const string ERROR_CONFLICT = "A conflict occurred while processing your request.";
    public const string ERROR_UNKNOWN = "An unexpected error occurred. Please try again later.";

    // Templates for specific messages
    public const string ERROR_TEMPLATE_OPERATION_FAILED = "{0} operation failed: {1}";
    public const string ERROR_TEMPLATE_RETRY_EXHAUSTED = "Failed after {0} attempts: {1}";
    public const string ERROR_TEMPLATE_VALIDATION_FAILED = "{0} validation failed: {1}";

    #endregion

    #region Operation-Specific Timeouts

    // Timeouts by operation type
    public const int TIMEOUT_QUICK_OPERATION_MS = 5000;      // 5s for quick operations
    public const int TIMEOUT_STANDARD_OPERATION_MS = 30000;  // 30s default
    public const int TIMEOUT_LONG_OPERATION_MS = 60000;      // 60s for sync
    public const int TIMEOUT_CRITICAL_OPERATION_MS = 120000; // 2min for critical operations

    #endregion
}

/// <summary>
/// Operation result constants for consistent status reporting and user feedback.
/// Provides standardized success and failure messaging across the application.
/// </summary>
public static class ResultConstants
{
    #region Status Messages

    // Standard status messages
    public const string STATUS_SUCCESS = "Operation completed successfully";
    public const string STATUS_FAILED = "Operation failed";
    public const string STATUS_CANCELLED = "Operation was cancelled";
    public const string STATUS_IN_PROGRESS = "Operation in progress";
    public const string STATUS_RETRYING = "Retrying operation";

    // Templates for specific results
    public const string RESULT_TEMPLATE_SUCCESS = "{0} completed successfully";
    public const string RESULT_TEMPLATE_FAILED = "{0} failed: {1}";
    public const string RESULT_TEMPLATE_PARTIAL = "{0} partially completed: {1}/{2} items";
    public const string RESULT_TEMPLATE_RETRY = "{0} retry {1}/{2}";

    #endregion

    #region Success Indicators

    // Success indicators by operation type
    public const string SUCCESS_DATA_SAVED = "Data saved successfully";
    public const string SUCCESS_DATA_LOADED = "Data loaded successfully";
    public const string SUCCESS_DATA_DELETED = "Data deleted successfully";
    public const string SUCCESS_SYNC_COMPLETED = "Synchronization completed";
    public const string SUCCESS_VALIDATION_PASSED = "Validation passed";
    public const string SUCCESS_CONNECTION_ESTABLISHED = "Connection established";

    #endregion
}

/// <summary>
/// Debugging constants for development and troubleshooting support.
/// Provides structured debugging levels and templates for comprehensive logging.
/// </summary>
public static class DebuggingConstants
{
    #region Debug Levels

    // Debug mode levels
    public const string DEBUG_LEVEL_MINIMAL = "MINIMAL";
    public const string DEBUG_LEVEL_STANDARD = "STANDARD";
    public const string DEBUG_LEVEL_VERBOSE = "VERBOSE";
    public const string DEBUG_LEVEL_TRACE = "TRACE";

    #endregion

    #region Debug Templates

    // Templates for structured debugging
    public const string DEBUG_TEMPLATE_METHOD = "[{0}::{1}] {2}";
    public const string DEBUG_TEMPLATE_PERFORMANCE = "[PERF] {0}: {1}ms";
    public const string DEBUG_TEMPLATE_STATE = "[STATE] {0} = {1}";
    public const string DEBUG_TEMPLATE_TRANSITION = "[TRANSITION] {0} -> {1}";

    #endregion

    #region Performance Tracking

    // Performance tracking markers
    public const string PERF_MARKER_START = "OPERATION_START";
    public const string PERF_MARKER_END = "OPERATION_END";
    public const string PERF_MARKER_CHECKPOINT = "CHECKPOINT";
    public const string PERF_MARKER_MILESTONE = "MILESTONE";

    #endregion
}