using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using MimeKit;
using MailKit.Search;
using Famoria.Application.Interfaces;

namespace Famoria.Application.Services;

public class ImapClientWrapper : IImapClientWrapper
{
    private readonly ImapClient _client = new();

    public async Task ConnectAsync(string host, int port, SecureSocketOptions options, CancellationToken cancellationToken)
        => await _client.ConnectAsync(host, port, options, cancellationToken);

    public async Task AuthenticateAsync(SaslMechanism saslMechanism, CancellationToken cancellationToken)
        => await _client.AuthenticateAsync(saslMechanism, cancellationToken);

    public async Task<IMailFolder> GetInboxAsync(CancellationToken cancellationToken)
    {
        var inbox = _client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
        return inbox;
    }

    public async Task<IList<UniqueId>> SearchAsync(IMailFolder folder, SearchQuery query, CancellationToken cancellationToken)
        => await folder.SearchAsync(query, cancellationToken);

    public async Task<MimeMessage> GetMessageAsync(IMailFolder folder, UniqueId uid, CancellationToken cancellationToken)
        => await folder.GetMessageAsync(uid, cancellationToken);

    public async Task DisconnectAsync(bool quit, CancellationToken cancellationToken)
        => await _client.DisconnectAsync(quit, cancellationToken);

    public void Dispose() => _client.Dispose();
}
