using System;

namespace DataWizard.Core.Services
{
    public static class SupabaseConfig
    {
        // You'll get these values from your Supabase project settings
        public static string Url { get; set; } = "";
        public static string AnonKey { get; set; } = "";
        
        public static void Initialize(string url, string anonKey)
        {
            Url = url;
            AnonKey = anonKey;
        }
    }
}