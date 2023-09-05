using Aforum.Models.User;
using Microsoft.Azure.Cosmos;

namespace Aforum.Services.Cosmos
{
    public class UserCosmosDbService : ICosmosDbService
    {
        private readonly IConfiguration _configuration;
        private Microsoft.Azure.Cosmos.Database todoDatabase;
        private Microsoft.Azure.Cosmos.Container itemsContainer;

        public UserCosmosDbService(IConfiguration configuration)
        {
            _configuration = configuration;
            todoDatabase = null!;
            itemsContainer = null!;
        }
        public Container MainContainer
        {
            get
            {
                if (itemsContainer == null)  // перше звернення - треба підключитись
                {
                    var cosmos = _configuration.GetSection("Azure").GetSection("Cosmos");
                    String key = cosmos.GetValue<String>("Key");
                    String endpoint = cosmos.GetValue<String>("Endpoint");
                    String databaseId = cosmos.GetValue<String>("DatabaseId");
                    String containerId = cosmos.GetValue<String>("ContainerId");

                    todoDatabase =
                        new CosmosClient(endpoint, key,
                            new CosmosClientOptions()
                            {
                                ApplicationName = "Forum"
                            }
                        ).CreateDatabaseIfNotExistsAsync(databaseId).Result;

                    itemsContainer =
                        todoDatabase
                        .CreateContainerIfNotExistsAsync(containerId, "/partitionKey")
                        .Result;
                }
                return itemsContainer;
            }
        }
       
    }
   
}
