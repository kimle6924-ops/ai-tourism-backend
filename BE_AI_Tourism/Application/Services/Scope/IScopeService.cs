namespace BE_AI_Tourism.Application.Services.Scope;

public interface IScopeService
{
    Task<bool> IsInScopeAsync(Guid userAdministrativeUnitId, Guid targetAdministrativeUnitId);
}
