namespace OrchidPro.Config;
public static class AppSettings
{
#if DEBUG
    public const string SupabaseUrl = "https://yrgxobkjrpgzoskxncto.supabase.co";
    public const string SupabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InlyZ3hvYmtqcnBnem9za3huY3RvIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDk0MjE5NzAsImV4cCI6MjA2NDk5Nzk3MH0.RlJ5Jdc7v8fVP6cM9HcY6S3-uRc0V2RVCenqmOtpiHg";
#else
    public const string SupabaseUrl = "https://yrgxobkjrpgzoskxncto.supabase.co";
    public const string SupabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InlyZ3hvYmtqcnBnem9za3huY3RvIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NDk0MjE5NzAsImV4cCI6MjA2NDk5Nzk3MH0.RlJ5Jdc7v8fVP6cM9HcY6S3-uRc0V2RVCenqmOtpiHg";
#endif
}