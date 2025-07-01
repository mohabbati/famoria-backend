namespace Famoria.Application.Features.FetchEmails;

public record FetchEmailsCommand(string FamilyId, string UserEmail, string AccessToken, DateTime Since) : IRequest<int>;
