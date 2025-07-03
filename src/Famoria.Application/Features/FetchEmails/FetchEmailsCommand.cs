namespace Famoria.Application.Features.FetchEmails;

public record FetchEmailsCommand(IEnumerable<UserLinkedAccountDto> LinkedAccounts, bool ForceUpdateLastFetched = false) : IRequest<int>;
