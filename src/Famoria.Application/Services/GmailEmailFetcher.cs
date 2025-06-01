using System.Text;
using MailKit.Security;
using MailKit.Search;
using Famoria.Application.Interfaces;
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
        var correlationId = Guid.NewGuid().ToString();
        var context = new Context();
        context["CorrelationId"] = correlationId;

        return await _retryPolicy.ExecuteAsync(async (ctx, ct) =>
        {
            var emlList = new List<string>();
            try
            {
                await _imapClient.ConnectAsync("imap.gmail.com", 993, SecureSocketOptions.SslOnConnect, ct);
                var sasl = new SaslMechanismOAuth2(userEmail, accessToken);
                await _imapClient.AuthenticateAsync(sasl, ct);
                var inbox = await _imapClient.GetInboxAsync(ct);

                // Only fetch emails received after 'since'
                var query = SearchQuery.NotSeen.Or(SearchQuery.Recent).Or(SearchQuery.DeliveredAfter(since));
                var uids = await _imapClient.SearchAsync(inbox, query, ct);

                foreach (var uid in uids)
                {
                    try
                    {
                        var message = await _imapClient.GetMessageAsync(inbox, uid, ct);
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

                await _imapClient.DisconnectAsync(true, ct);
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
