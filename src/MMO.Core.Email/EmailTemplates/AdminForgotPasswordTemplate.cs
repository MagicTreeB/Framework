using System;

namespace MMO.Core.Email.EmailTemplates;


public static class AdminForgotPasswordTemplate
{
    public static string TemplateName => "admin-reset-password-template";
    public static string RoutingKey => "admin-reset-password";
    public static string Subject => "Password Reset";

    public enum Params
    {
        FirstName,
        LastName,
        LinkToken,
        ExpiresAt
    }
}