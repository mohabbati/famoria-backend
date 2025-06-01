using System.Text;
using MailKit.Net.Imap;
using MailKit.Security;
using MailKit.Search;
using MailKit;
using Famoria.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Famoria.Application.Services;

public class GmailEmailFetcher : IEmailFetcher
{
    private readonly ILogger<GmailEmailFetcher> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public GmailEmailFetcher(ILogger<GmailEmailFetcher> logger, AsyncRetryPolicy retryPolicy)
    {
        _logger = logger;
        _retryPolicy = retryPolicy;
    }

    public async Task<List<string>> GetNewEmailsAsync(string userEmail, string accessToken, DateTime since, CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid().ToString();
        var context = new Context();
        context["CorrelationId"] = correlationId;

        return await _retryPolicy.ExecuteAsync(async (ctx, ct) =>
        {
            var emlList = new List<string>();
            try
            {
                using var client = new ImapClient();
                await client.ConnectAsync("imap.gmail.com", 993, SecureSocketOptions.SslOnConnect, ct);
                var sasl = new SaslMechanismOAuth2(userEmail, accessToken);
                await client.AuthenticateAsync(sasl, ct);
                var inbox = client.Inbox;
                await inbox.OpenAsync(FolderAccess.ReadOnly, ct);

                // Only fetch emails received after 'since'
                var query = SearchQuery.NotSeen.Or(SearchQuery.Recent).Or(SearchQuery.DeliveredAfter(since));
                var uids = await inbox.SearchAsync(query, ct);

                foreach (var uid in uids)
                {
                    try
                    {
                        var message = await inbox.GetMessageAsync(uid, ct);
                        using var stream = new MemoryStream();
                        await message.WriteToAsync(stream, ct);
                        var emlContent = Encoding.UTF8.GetString(stream.ToArray());
                        emlList.Add(emlContent);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to fetch or process message UID {Uid}: {Message} (CorrelationId: {CorrelationId})",
                            uid, ex.Message, context.ContainsKey("CorrelationId") ? context["CorrelationId"] : "N/A");
                    }
                }

                await client.DisconnectAsync(true, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching emails for {UserEmail}: {Message} (CorrelationId: {CorrelationId})",
                    userEmail, ex.Message, context.ContainsKey("CorrelationId") ? context["CorrelationId"] : "N/A");
                throw;
            }
            return emlList;
        }, context, cancellationToken);
    }
} 
