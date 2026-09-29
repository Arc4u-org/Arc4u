using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Arc4u.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        /// <summary>Answers the challenge with a 401 status and a JSON <see cref="ProblemDetails"/> body. For an expired token, the <c>x-token-expired</c> header gives the expiration date.</summary>
        /// <param name="context">The challenge context.</param>
        /// <returns>A task that completes when the response is written.</returns>
        public override Task Challenge(JwtBearerChallengeContext context)
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

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
                Status = StatusCodes.Status403Forbidden
            }, StandardBearerEventsJsonContext.Default.ProblemDetails));
        }

        /// <inheritdoc/>
        public override Task MessageReceived(MessageReceivedContext context)
        {
            return base.MessageReceived(context);
        }

        /// <summary>
        /// Log the exception, reason why the authentication is failing.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task AuthenticationFailed(AuthenticationFailedContext context)
        {
            logger.Technical().LogException(context.Exception);

            context.Fail(context.Exception);
            context.Response.Clear();
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Save the access token in the identity.BootstrapContext
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
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
    }
}
