using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Arc4u.Diagnostics;
using Arc4u.OAuth2.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Arc4u.OAuth2.Events
{
    [JsonSerializable(typeof(ProblemDetails))]
    internal partial class StandardBearerEventsJsonContext : JsonSerializerContext
    {
    }
    /// <summary>
    /// The default <see cref="JwtBearerEvents"/>: failures are logged and answered with a 401 status, and the bearer token is kept in the <see cref="ClaimsIdentity.BootstrapContext"/> of the validated identity.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public class StandardBearerEvents(ILogger<StandardBearerEvents> logger) : JwtBearerEvents
    {
        /// <summary>Answers the challenge with a 401 status and a <see cref="ProblemDetails"/> body (<c>application/problem+json</c>). For an expired token, the <c>x-token-expired</c> header gives the expiration date.</summary>
        /// <param name="context">The challenge context.</param>
        /// <returns>A task that completes when the response is written.</returns>
        public override Task Challenge(JwtBearerChallengeContext context)
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/problem+json";

            // Add some extra context for expired tokens.
            if (context.AuthenticateFailure is not null && context.AuthenticateFailure is SecurityTokenExpiredException authenticationException)
            {
                var expires = authenticationException.Expires.ToString("o");
                context.Response.Headers.Append("x-token-expired", expires);
                context.ErrorDescription = $"The token expired on {expires}";
            }

            return context.Response.WriteAsync(JsonSerializer.Serialize(new ProblemDetails
            {
                Title = context.Error,
                Detail = context.ErrorDescription,
                Status = StatusCodes.Status401Unauthorized
            }, StandardBearerEventsJsonContext.Default.ProblemDetails));
        }

        /// <inheritdoc/>
        public override Task MessageReceived(MessageReceivedContext context)
        {
            return base.MessageReceived(context);
        }

        /// <summary>
        /// Logs the exception, the reason why the authentication failed, fails the authentication and answers with a 401 status.
        /// </summary>
        /// <param name="context">The authentication failure context.</param>
        /// <returns>A completed task.</returns>
        public override Task AuthenticationFailed(AuthenticationFailedContext context)
        {
            logger.Technical().LogException(context.Exception);

            context.Fail(context.Exception);
            context.Response.Clear();
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Saves the access token, read from the <c>Authorization</c> header, in the <see cref="ClaimsIdentity.BootstrapContext"/> of the identity.
        /// The authentication fails with a 401 status when the header does not carry a <c>Bearer</c> token, or when the claims filler is enabled
        /// (<see cref="ClaimsFillerOptions.LoadClaimsFromClaimsFillerProvider"/>) and no claim identifying the user (see <see cref="ClaimsIdentifierOption"/>) is in the token.
        /// </summary>
        /// <param name="context">The token validated context.</param>
        /// <returns>A completed task.</returns>
        public override Task TokenValidated(TokenValidatedContext context)
        {
            if (context.SecurityToken is not null)
            {
                if (context.Principal?.Identity is ClaimsIdentity identity)
                {
                    var sToken = context.Request.Headers.Authorization.ToString();
                    if (sToken.StartsWith("Bearer ", StringComparison.InvariantCultureIgnoreCase))
                    {
                        sToken = sToken[7..];
                        identity.BootstrapContext = sToken;

                        if (!HasUserIdentifier(context, identity))
                        {
                            context.Fail("The token has no claim identifying the user.");
                            context.Response.Clear();
                            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        }
                    }
                    else
                    {
                        context.Fail("A Bearer token is expected!");
                        context.Response.Clear();
                        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    }
                }
            }

            return Task.CompletedTask;
        }

        // The claims transformation builds the cache key of the extra claims from the user identifier: check it here to answer with a 401 instead of failing later with a 500.
        private bool HasUserIdentifier(TokenValidatedContext context, ClaimsIdentity identity)
        {
            var services = context.HttpContext.RequestServices;
            if (services?.GetService<IOptions<ClaimsFillerOptions>>()?.Value.LoadClaimsFromClaimsFillerProvider != true)
            {
                return true;
            }

            var keyGenerator = services.GetService<ICacheKeyGenerator>();
            if (keyGenerator is null)
            {
                return true;
            }

            try
            {
                keyGenerator.GetClaimsKey(identity);
                return true;
            }
            catch (Exception ex)
            {
                var claimTypes = services.GetService<IOptions<ClaimsIdentifierOption>>()?.Value ?? [];
                logger.Technical().LogNoUserIdentifierClaim(string.Join(",", claimTypes), ex.Message);
                return false;
            }
        }
    }
}
