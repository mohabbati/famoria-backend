using Famoria.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Famoria.Application.Interfaces;

public interface IUserLinkedAccountService
{
    Task UpsertAsync(UserLinkedAccount connection, CancellationToken cancellationToken);
}
