using Aforum.Middleware;
using Aforum.Models;
using Aforum.Models.User;
using Aforum.Services.Cosmos;
using Aforum.Services.Hash;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Net.Http;

namespace Aforum.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHashService _hashService;
        private readonly IConfiguration _configuration;
        private readonly ICosmosDbService _cosmosDb;
       

        public HomeController(ILogger<HomeController> logger, IHashService hashService, IConfiguration configuration, ICosmosDbService cosmosDb)
        {
            _logger = logger;
            _hashService = hashService;
            _configuration = configuration;
            _cosmosDb = cosmosDb;
            
        }
        private FeedIterator<UserSignupModel> GetFeedIterator(string sqlQueryText)
        {
            var itemsContainer = _cosmosDb.MainContainer;
            var queryDefinition = new QueryDefinition(sqlQueryText);
            return itemsContainer.GetItemQueryIterator<UserSignupModel>(queryDefinition);
        }
        public IActionResult Index()
        {
              var itemsContainer = _cosmosDb.MainContainer;

              var sqlQueryText = "SELECT * FROM c WHERE c.type = 'Feedback'"; 
              QueryDefinition queryDefinition = new QueryDefinition(sqlQueryText);
              FeedIterator<UserSignupModel> queryResultSetIterator = itemsContainer.GetItemQueryIterator<UserSignupModel>(queryDefinition);

              List<UserSignupModel> feedbacks = new List<UserSignupModel>();
              while (queryResultSetIterator.HasMoreResults)
              {
                  FeedResponse<UserSignupModel> currentResultSet = queryResultSetIterator.ReadNextAsync().Result;
                  foreach (UserSignupModel item in currentResultSet)
                  {
                      feedbacks.Add(item);
                  }
              }

              return View(feedbacks);
            
        }
        
        public IActionResult AddComment([FromForm] MessageModel? message, [FromForm] string topicId)
        {
            if (string.IsNullOrEmpty(topicId))
            {
                return RedirectToAction("Error");
            }

            var itemsContainer = _cosmosDb.MainContainer;
            var sqlQueryText = "SELECT * FROM c WHERE c.type = 'Feedback'";
            var queryResultSetIterator = GetFeedIterator(sqlQueryText);

            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<UserSignupModel> currentResultSet = queryResultSetIterator.ReadNextAsync().Result;

                foreach (UserSignupModel item in currentResultSet)
                {
                    if (item.Topics != null)
                    {
                        foreach (var topic in item.Topics)
                        {
                            var topicGuid = topic.Id;
                            var inputGuid = Guid.Parse(topicId);

                            if (topicGuid == inputGuid)
                            {
                                var newMessage = new MessageModel
                                {
                                    Id = Guid.Parse(topicId),
                                    Message = message.Message,
                                    CreatedAtMessage = DateTime.Now,
                                    UserName = HttpContext.User.FindFirst(ClaimTypes.Name).Value
                            };

                                item.Messages.Add(newMessage);

                                var response = itemsContainer.ReplaceItemAsync<UserSignupModel>(
                                    item, item.id.ToString(), new PartitionKey(item.partitionKey)).Result;
                                break;
                            }
                        }
                    }
                }
            }

            return RedirectToAction("Index", "Home");
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}