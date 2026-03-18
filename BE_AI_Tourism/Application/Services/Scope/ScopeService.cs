using BE_AI_Tourism.Domain.Entities;
using BE_AI_Tourism.Domain.Interfaces;

namespace BE_AI_Tourism.Application.Services.Scope;

public class ScopeService : IScopeService
{
    private readonly IRepository<AdministrativeUnit> _repository;

    public ScopeService(IRepository<AdministrativeUnit> repository)
    {
        _repository = repository;
    }

    public async Task<bool> IsInScopeAsync(Guid userAdministrativeUnitId, Guid targetAdministrativeUnitId)
    {
        if (userAdministrativeUnitId == targetAdministrativeUnitId)
            return true;

        // Walk up from target to see if we reach the user's unit
        var currentId = targetAdministrativeUnitId;
        var visited = new HashSet<Guid>();

        while (true)
        {
            if (!visited.Add(currentId))
                break;

            var unit = await _repository.GetByIdAsync(currentId);
            if (unit?.ParentId == null)
                break;

            if (unit.ParentId == userAdministrativeUnitId)
                return true;

            currentId = unit.ParentId.Value;
        }

        return false;
    }
}
