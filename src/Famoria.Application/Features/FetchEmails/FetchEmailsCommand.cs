using MediatR;

namespace Famoria.Application.Features.FetchEmails;

public record FetchEmailsCommand(string FamilyId, string UserEmail, string AccessToken) : IRequest<int>;
