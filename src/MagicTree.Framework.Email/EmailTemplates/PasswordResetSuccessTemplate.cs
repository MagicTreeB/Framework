
namespace MagicTree.Framework.Email.EmailTemplates;

/// <summary>
/// Template configuration for password reset success notification emails
/// Used when a user's password has been successfully reset by admin or after OTP verification
/// </summary>
public static class PasswordResetSuccessTemplate
{
    /// <summary>
    /// Template file name (without .html extension)
    /// File: Email.Api/Templates/password-reset-success-template.html
    /// </summary>
    public static string TemplateName => "password-reset-success-template";
    
    /// <summary>
    /// RabbitMQ routing key for password reset success events
    /// </summary>
    public static string RoutingKey => "password-reset-success";
    
    /// <summary>
    /// Email subject line
    /// </summary>
    public static string Subject => "Password Reset Successful - Your Account is Secure";

    /// <summary>
    /// Template placeholder parameters
    /// </summary>
    public enum Params
    {
        /// <summary>
        /// User's first name for personalization
        /// </summary>
        FirstName,
        
        /// <summary>
        /// User's last name (optional)
        /// </summary>
        LastName,
        
        /// <summary>
        /// Timestamp when password was reset (formatted string)
        /// </summary>
        ResetAt
    }
}
