using Microsoft.AspNetCore.Authorization;

namespace BE_AI_Tourism.Infrastructure.Authorization;

public class ScopeRequirement : IAuthorizationRequirement
{
    public string Scope { get; }

    public ScopeRequirement(string scope)
    {
        Scope = scope;
    }
}
