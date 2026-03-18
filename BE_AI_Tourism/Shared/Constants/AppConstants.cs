namespace BE_AI_Tourism.Shared.Constants;

public static class AppConstants
{
    public static class Pagination
    {
        public const int DefaultPageSize = 10;
        public const int MaxPageSize = 50;
        public const int MinPageNumber = 1;
        public const int MinPageSize = 1;
    }

    public static class Api
    {
        public const string BaseRoute = "api";
    }

    public static class ErrorMessages
    {
        public const string NotFound = "Resource not found";
        public const string Unauthorized = "Unauthorized";
        public const string Forbidden = "You do not have permission to perform this action";
        public const string BadRequest = "Bad request";
        public const string InternalError = "An unexpected error occurred";
    }

    public static class HealthCheck
    {
        public const string Healthy = "Healthy";
    }

    public static class Auth
    {
        public const string EmailAlreadyExists = "Email already exists";
        public const string InvalidCredentials = "Invalid email or password";
        public const string InvalidRefreshToken = "Invalid or expired refresh token";
        public const string AccountLocked = "Account is locked";
    }

    public static class JwtClaimTypes
    {
        public const string UserId = "userId";
        public const string AdministrativeUnitId = "administrativeUnitId";
    }

    public static class Administrative
    {
        public const string CodeAlreadyExists = "Administrative unit code already exists";
        public const string ParentNotFound = "Parent administrative unit not found";
        public const string InvalidLevelHierarchy = "Invalid level hierarchy. Central→Province→Ward→Neighborhood";
        public const string HasChildren = "Cannot delete administrative unit that has children";
    }

    public static class Category
    {
        public const string SlugAlreadyExists = "Category slug already exists";
    }
}
