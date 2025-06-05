using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Famoria.Application.Interfaces;
using Famoria.Application.Services;
using Famoria.Domain.Entities;

using FluentAssertions;

using Microsoft.Azure.Cosmos;

using Moq;

using Xunit;

namespace Famoria.Unit.Tests.Auth;

// public class CosmosIntegrationConnectionStoreTests
// {
//     [Fact]
//     public async Task UpsertAsync_ShouldEncryptTokens_BeforeStorage()
//     {
//         var key = new byte[32];
//         for (int i = 0; i < key.Length; i++) key[i] = (byte)i;
//         var crypto = new AesCryptoService(key);
//         var mockContainer = new Mock<Container>();
//         UserIntegrationConnection? stored = null;
//         mockContainer.Setup(c => c.UpsertItemAsync(
//             It.IsAny<UserIntegrationConnection>(),
//             null, null, It.IsAny<CancellationToken>()))
//             .Callback((object item, PartitionKey pk, ItemRequestOptions o, CancellationToken ct) =>
//             {
//                 stored = (UserIntegrationConnection)item;
//             })
//             .ReturnsAsync((ItemResponse<UserIntegrationConnection>)null!);

//         var repo = new TestCosmosIntegrationConnectionService(mockContainer.Object, crypto);
//         var conn = new UserIntegrationConnection
//         {
//             FamilyId = "fam001",
//             UserId = "user1",
//             Provider = "Google",
//             Source = Famoria.Domain.Enums.FamilyItemSource.Email,
//             UserEmail = "user@gmail.com",
//             AccessToken = "ya29.test",
//             RefreshToken = "refresh",
//             IsActive = true
//         };
//         await repo.UpsertAsync(conn, CancellationToken.None);
//         stored.Should().NotBeNull();
//         stored!.AccessToken.Should().NotContain("ya29");
//         var decrypted = crypto.Decrypt(stored.AccessToken!);
//         decrypted.Should().Be("ya29.test");
//     }
// }

// // Minimal stub for test
// public class TestCosmosIntegrationConnectionService : IUserIntegrationConnectionService
// {
//     private readonly Container _container;
//     private readonly AesCryptoService _crypto;
//     public TestCosmosIntegrationConnectionService(Container container, AesCryptoService crypto)
//     {
//         _container = container;
//         _crypto = crypto;
//     }
//     public async Task UpsertAsync(UserIntegrationConnection connection, CancellationToken cancellationToken)
//     {
//         connection.AccessToken = _crypto.Encrypt(connection.AccessToken!);
//         if (connection.RefreshToken != null)
//             connection.RefreshToken = _crypto.Encrypt(connection.RefreshToken);
//         await _container.UpsertItemAsync(connection, null, null, cancellationToken);
//     }
// }
