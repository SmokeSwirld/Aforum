using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Aforum.Models.User;
using Aforum.Services.Cosmos;
using Microsoft.Azure.Cosmos;
using Microsoft.AspNetCore.Authentication;


namespace Aforum.Middleware
{
    public class SessionAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ICosmosDbService cosmosDb)
        {
            var authUserId = context.Session.GetString("AuthUserId");
            var itemsContainer = cosmosDb.MainContainer;
            if (context.Session.Keys.Contains("AuthUserId"))
            {
                
                if (!string.IsNullOrEmpty(authUserId))
                {
                    var sqlQueryText = "SELECT * FROM c WHERE c.type = 'Feedback'";
                    QueryDefinition queryDefinition = new(sqlQueryText);
                    FeedIterator<UserSignupModel> queryResultSetIterator = itemsContainer.GetItemQueryIterator<UserSignupModel>(queryDefinition);
                    
                    while (queryResultSetIterator.HasMoreResults)
                    {
                        FeedResponse<UserSignupModel> currentResultSet = queryResultSetIterator.ReadNextAsync().Result;

                        foreach (UserSignupModel item in currentResultSet)
                        {
                            if (item.id == Guid.Parse(authUserId) && item.RealName!=null)  // Перевірка на автентифікованого користувача
                            {
                                Claim[] claims = new Claim[]
                                {
                                  new Claim(ClaimTypes.Name, item.RealName),
                                  new Claim("LoginClaim", item.Login),// Ваш власний ідентифікатор клейм string? loginClaimType = "LoginClaim"; - там де потрібен цей клейм
                                  new Claim(ClaimTypes.Email, item.Email),
                                  new Claim(ClaimTypes.UserData, item.AvatarFileName ?? "")
                                };
                                // Встановлення об'єкта ClaimsPrincipal для подальшої автентифікації
                                var userIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                                var userPrincipal = new ClaimsPrincipal(userIdentity);

                                context.User = userPrincipal;

                                // Якщо знайдено автентифікованого користувача, виходимо з циклу
                                break;
                            }
                        }
                    }
                }
            }

            await _next(context);
        }
    }
    public static class SessionAuthMiddlewareExtension
    {
        public static IApplicationBuilder
            UseSessionAuth(this IApplicationBuilder app)
        {
            return app.UseMiddleware<SessionAuthMiddleware>();
        }
    }
}
