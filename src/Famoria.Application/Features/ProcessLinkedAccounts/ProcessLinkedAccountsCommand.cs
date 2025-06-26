namespace Famoria.Application.Features.ProcessLinkedAccounts;

public record ProcessLinkedAccountsCommand(IntegrationProvider Provider) : IRequest<int>;
