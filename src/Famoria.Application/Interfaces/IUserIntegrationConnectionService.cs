using Famoria.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Famoria.Application.Interfaces;

public interface IUserIntegrationConnectionService
{
    Task UpsertAsync(UserIntegrationConnection connection, CancellationToken cancellationToken);
}
