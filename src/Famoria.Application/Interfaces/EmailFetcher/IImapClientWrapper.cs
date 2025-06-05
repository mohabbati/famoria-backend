using MailKit;
using MailKit.Security;
using MimeKit;
using MailKit.Search;

namespace Famoria.Application.Interfaces;

public interface IImapClientWrapper : IDisposable
{
    Task ConnectAsync(string host, int port, SecureSocketOptions options, CancellationToken cancellationToken);
    Task AuthenticateAsync(SaslMechanism saslMechanism, CancellationToken cancellationToken);
    Task<IMailFolder> GetInboxAsync(CancellationToken cancellationToken);
    Task<IList<UniqueId>> SearchAsync(IMailFolder folder, SearchQuery query, CancellationToken cancellationToken);
    Task<MimeMessage> GetMessageAsync(IMailFolder folder, UniqueId uid, CancellationToken cancellationToken);
    Task DisconnectAsync(bool quit, CancellationToken cancellationToken);
}
