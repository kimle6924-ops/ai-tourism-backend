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
        // Messages
        public const string EmailAlreadyExists = "Email already exists";
        public const string InvalidCredentials = "Invalid email or password";
        public const string InvalidRefreshToken = "Invalid or expired refresh token";
        public const string AccountLocked = "Account is locked";
        public const string CannotRegisterAdmin = "Cannot register as Admin";
        public const string ContributorRequiresAdminUnit = "AdministrativeUnitId is required for Contributor";
        public const string AdminUnitNotFound = "AdministrativeUnit not found";
        public const string AccountPendingApproval = "Account is pending approval";
    }

    public static class ErrorCodes
    {
        // Auth
        public const string EmailAlreadyExists = "EMAIL_ALREADY_EXISTS";
        public const string InvalidCredentials = "INVALID_CREDENTIALS";
        public const string InvalidRefreshToken = "INVALID_REFRESH_TOKEN";
        public const string AccountLocked = "ACCOUNT_LOCKED";
        public const string AccountPendingApproval = "ACCOUNT_PENDING_APPROVAL";
        public const string CannotRegisterAdmin = "CANNOT_REGISTER_ADMIN";
        public const string ContributorRequiresAdminUnit = "CONTRIBUTOR_REQUIRES_ADMIN_UNIT";
        public const string AdminUnitNotFound = "ADMIN_UNIT_NOT_FOUND";

        // General
        public const string NotFound = "NOT_FOUND";
        public const string BadRequest = "BAD_REQUEST";
        public const string Unauthorized = "UNAUTHORIZED";
        public const string Forbidden = "FORBIDDEN";
        public const string ValidationFailed = "VALIDATION_FAILED";
        public const string InternalError = "INTERNAL_ERROR";

        // Administrative
        public const string AdminCodeAlreadyExists = "ADMIN_CODE_ALREADY_EXISTS";
        public const string ParentNotFound = "PARENT_NOT_FOUND";
        public const string InvalidLevelHierarchy = "INVALID_LEVEL_HIERARCHY";
        public const string HasChildren = "HAS_CHILDREN";

        // Category
        public const string SlugAlreadyExists = "SLUG_ALREADY_EXISTS";

        // Chat
        public const string ConversationNotFound = "CONVERSATION_NOT_FOUND";
        public const string InvalidConversationId = "INVALID_CONVERSATION_ID";

        // Admin User
        public const string UserNotPendingApproval = "USER_NOT_PENDING_APPROVAL";
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
        public const string InvalidLevelHierarchy = "Invalid level hierarchy. Province→Ward";
        public const string HasChildren = "Cannot delete administrative unit that has children";
    }

    public static class Category
    {
        public const string SlugAlreadyExists = "Category slug already exists";
    }

    public static class AdminUser
    {
        public const string UserNotPendingApproval = "User is not pending approval";
    }

    public static class Chat
    {
        public const string ConversationNotFound = "Cuộc trò chuyện không tồn tại hoặc không thuộc về bạn";
        public const string InvalidConversationId = "ID cuộc trò chuyện không hợp lệ. Vui lòng truyền đúng định dạng UUID";
    }
}
