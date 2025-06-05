using System.Threading;
using System.Threading.Tasks;

using Famoria.Domain.Entities;

namespace Famoria.Application.Interfaces;

public interface IUserLinkedAccountService
{
    Task UpsertAsync(UserLinkedAccount connection, CancellationToken cancellationToken);
}
