using BE_AI_Tourism.Domain.Entities;
using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Domain.Interfaces;
using BE_AI_Tourism.Shared.Constants;
using Microsoft.AspNetCore.Authorization;

namespace BE_AI_Tourism.Infrastructure.Authorization;

public class ScopeAuthorizationHandler : AuthorizationHandler<ScopeRequirement>
{
    private readonly IRepository<AdministrativeUnit> _adminUnitRepository;

    public ScopeAuthorizationHandler(IRepository<AdministrativeUnit> adminUnitRepository)
    {
        _adminUnitRepository = adminUnitRepository;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, ScopeRequirement requirement)
    {
        var roleClaim = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (roleClaim == UserRole.Admin.ToString())
        {
            context.Succeed(requirement);
            return;
        }

        if (roleClaim != UserRole.Contributor.ToString())
            return;

        var unitIdClaim = context.User.FindFirst(AppConstants.JwtClaimTypes.AdministrativeUnitId)?.Value;
        if (string.IsNullOrEmpty(unitIdClaim) || !Guid.TryParse(unitIdClaim, out var userUnitId))
            return;

        if (!Guid.TryParse(requirement.Scope, out var requiredUnitId))
            return;

        // Check if user's unit is the same or an ancestor of the required unit
        if (await IsUnitInScope(userUnitId, requiredUnitId))
            context.Succeed(requirement);
    }

    private async Task<bool> IsUnitInScope(Guid userUnitId, Guid targetUnitId)
    {
        if (userUnitId == targetUnitId)
            return true;

        // Walk up the tree from target to see if we reach the user's unit
        var currentId = targetUnitId;
        var visited = new HashSet<Guid>();

        while (true)
        {
            if (!visited.Add(currentId))
                break;

            var unit = await _adminUnitRepository.GetByIdAsync(currentId);
            if (unit?.ParentId == null)
                break;

            if (unit.ParentId == userUnitId)
                return true;

            currentId = unit.ParentId.Value;
        }

        return false;
    }
}
