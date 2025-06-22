using System.Text;

using Famoria.Application.Interfaces;

using MailKit.Search;
using MailKit.Security;

using Microsoft.Extensions.Logging;

using Polly;
using Polly.Retry;

namespace Famoria.Application.Services;

public class GmailEmailFetcher : IEmailFetcher
{
    private readonly ILogger<GmailEmailFetcher> _logger;
    private readonly IAsyncPolicy _retryPolicy;
    private readonly IImapClientWrapper _imapClient;

    public GmailEmailFetcher(ILogger<GmailEmailFetcher> logger, IAsyncPolicy retryPolicy, IImapClientWrapper imapClient)
    {
        _logger = logger;
        _retryPolicy = retryPolicy;
        _imapClient = imapClient;
    }

    public async Task<List<string>> GetNewEmailsAsync(string userEmail, string accessToken, DateTime since, CancellationToken cancellationToken)
    {
        return await _retryPolicy.ExecuteAsync(async (ctx, ct) =>
        {
            var emlList = new List<string>();
            var correlationId = Guid.NewGuid().ToString();
            var connected = false;
            try
            {
                await _imapClient.ConnectAsync("imap.gmail.com", 993, SecureSocketOptions.SslOnConnect, ct).ConfigureAwait(false);
                connected = true;
                var sasl = new SaslMechanismOAuth2(userEmail, accessToken);
                await _imapClient.AuthenticateAsync(sasl, ct).ConfigureAwait(false);
                var inbox = await _imapClient.GetInboxAsync(ct).ConfigureAwait(false);

                // Only fetch emails received after 'since'
                var query = SearchQuery.NotSeen.Or(SearchQuery.Recent).Or(SearchQuery.DeliveredAfter(since));
                var uids = await _imapClient.SearchAsync(inbox, query, ct).ConfigureAwait(false);

                foreach (var uid in uids)
                {
                    try
                    {
                        var message = await _imapClient.GetMessageAsync(inbox, uid, ct).ConfigureAwait(false);
                        using var stream = new MemoryStream();
                        await message.WriteToAsync(stream, ct).ConfigureAwait(false);
                        var emlContent = Encoding.UTF8.GetString(stream.ToArray());
                        emlList.Add(emlContent);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to fetch or process message UID {Uid}: {Message} (CorrelationId: {CorrelationId})",
                            uid, ex.Message, correlationId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching emails for {LinkedAccount}: {Message} (CorrelationId: {CorrelationId})",
                    userEmail, ex.Message, correlationId);
                throw;
            }
            finally
            {
                if (connected)
                {
                    try
                    {
                        await _imapClient.DisconnectAsync(true, ct).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to disconnect IMAP client (CorrelationId: {CorrelationId})", correlationId);
                    }
                }

                _imapClient.Dispose();
            }

            return emlList;
        }, new Context(), cancellationToken).ConfigureAwait(false);
    }
}
