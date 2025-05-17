using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace DataWizard.UI.Helpers
{
    /// <summary>
    /// Helper class for input validation in forms
    /// </summary>
    public static class InputValidation
    {
        // Regular expressions for validation
        private static readonly Regex EmailRegex = new Regex(
            @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled);

        // Minimum requirements
        private const int MinUsernameLength = 3;
        private const int MinPasswordLength = 6;

        /// <summary>
        /// Validates an email address
        /// </summary>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return EmailRegex.IsMatch(email);
        }

        /// <summary>
        /// Validates a username
        /// </summary>
        public static bool IsValidUsername(string username)
        {
            return !string.IsNullOrWhiteSpace(username) && username.Length >= MinUsernameLength;
        }

        /// <summary>
        /// Validates a password
        /// </summary>
        public static bool IsValidPassword(string password)
        {
            return !string.IsNullOrWhiteSpace(password) && password.Length >= MinPasswordLength;
        }

        /// <summary>
        /// Checks if passwords match
        /// </summary>
        public static bool DoPasswordsMatch(string password, string confirmPassword)
        {
            return !string.IsNullOrWhiteSpace(password) && password == confirmPassword;
        }

        /// <summary>
        /// Displays an error message below an input field
        /// </summary>
        public static void ShowValidationMessage(Panel parent, string message, bool isError = true)
        {
            // Create error message
            TextBlock messageBlock = new TextBlock
            {
                Text = message,
                FontSize = 12,
                Margin = new Thickness(4, 4, 0, 0),
                Foreground = new SolidColorBrush(
                    isError ?
                    Color.FromArgb(255, 220, 38, 38) : // Error (red)
                    Color.FromArgb(255, 34, 197, 94))  // Success (green)
            };

            // Add to parent
            parent.Children.Add(messageBlock);

            // Remove after delay if it's a success message
            if (!isError)
            {
                RemoveMessageAfterDelay(parent, messageBlock);
            }
        }

        /// <summary>
        /// Removes a validation message after a delay
        /// </summary>
        private static async void RemoveMessageAfterDelay(Panel parent, TextBlock message)
        {
            await Task.Delay(3000); // 3 seconds

            // Check if still in visual tree
            if (parent.Children.Contains(message))
            {
                parent.Children.Remove(message);
            }
        }

        /// <summary>
        /// Removes all validation messages from a panel
        /// </summary>
        public static void ClearValidationMessages(Panel parent)
        {
            for (int i = parent.Children.Count - 1; i >= 0; i--)
            {
                if (parent.Children[i] is TextBlock textBlock &&
                    (textBlock.Foreground is SolidColorBrush brush) &&
                    (brush.Color == Color.FromArgb(255, 220, 38, 38) ||
                     brush.Color == Color.FromArgb(255, 34, 197, 94)))
                {
                    parent.Children.RemoveAt(i);
                }
            }
        }
    }
}