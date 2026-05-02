namespace MagicTree.Framework.Email.EmailTemplates;

public static class ForgotPasswordTemplate
{
    public static string TemplateName => "forgot-password-template";
    public static string RoutingKey => "forgot-password";
    public static string Subject => "Password Reset";

    public enum Params
    {
        FirstName,
        LastName,
        LinkToken,
        ExpiresAt
    }
}
