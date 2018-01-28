namespace WebApplication.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// This controller will receive the skype messages and handle them to the EchoBot service. 
    /// </summary>
    [Route("api/[controller]")]
    public class MessagesController : Controller
    {
        /// <summary>
        /// memoryCache
        /// </summary>
        IMemoryCache memoryCache;

        /// <summary>
        /// Bot Credentials
        /// </summary>
        BotCredentials botCredentials;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="memoryCache">memory cache service</param>
        /// <param name="botCredentials">bot credentials</param>
        public MessagesController(IMemoryCache memoryCache, IOptions<BotCredentials> botCredentials)
        {
            this.memoryCache = memoryCache;
            this.botCredentials = botCredentials.Value;
        }

        /// <summary>
        /// This method will be called every time the bot receives an activity. This is the messaging endpoint
        /// </summary>
        /// <param name="activity">The activity sent to the bot. I'm using dynamic here to simplify the code for the post</param>
        /// <returns>201 Created</returns>
        [HttpPost]
        public virtual async Task<IActionResult> Post([FromBody] dynamic activity)
        {
            // Get the conversation id so the bot answers.
            var conversationId = activity.from.id.ToString();

            // Get a valid token 
            string token = await this.GetBotApiToken();

            // send the message back
            using (var client = new HttpClient())
            {
                // I'm using dynamic here to make the code simpler
                dynamic message = new ExpandoObject();
                message.type = "message/text";
                message.text = activity.text;

                // Set the toekn in the authorization header.
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            }
            return Created(Url.Content("~/"), string.Empty);
        }

        /// <summary>
        /// Gets and caches a valid token so the bot can send messages.
        /// </summary>
        /// <returns>The token</returns>
        private async Task<string> GetBotApiToken()
        {
            // Check to see if we already have a valid token
            string token = memoryCache.Get("token")?.ToString();
            if (string.IsNullOrEmpty(token))
            {
                // we need to get a token.
                using (var client = new HttpClient())
                {
                    // Create the encoded content needed to get a token
                    var parameters = new Dictionary<string, string>
                    {
                        {"client_id", this.botCredentials.ClientId },
                        {"client_secret", this.botCredentials.ClientSecret },
                        {"scope", "https://graph.microsoft.com/.default" },
                        {"grant_type", "client_credentials" }
                    };
                    var content = new FormUrlEncodedContent(parameters);

                    // Post
                    var response = await client.PostAsync("https://login.microsoftonline.com/common/oauth2/v2.0/token", content);

                    // Get the token response
                    var tokenResponse = await response.Content.ReadAsAsync<TokenResponse>();

                    //token = tokenResponse.access_token;


                    // Cache the token fo 15 minutes.
                    memoryCache.Set(
                        "token",
                        "eisW9Ban+b6k4a1O4RkyQ3bjy24teJzK+/A99l84Hl/E+scSs9JdVb5eHycw1JhhHYVLrTwQaBpOr3StpJCy72q3wIyFx+L7YHeXrJWYM3F0uc8d0ezEzr1QxcY8F6rQcUGJ9PGjHi2byy19d9X6wgdB04t89/1O/w1cDnyilFU=",
                        new DateTimeOffset(DateTime.Now.AddMinutes(15)));
                }
            }

            return token;
        }
    }
}