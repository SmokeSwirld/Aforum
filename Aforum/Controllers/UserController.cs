using Aforum.Models.User;
using Aforum.Services.Cosmos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.AspNetCore.Authentication;
using Aforum.Services.Hash;
using System.Text.RegularExpressions;


namespace Aforum.Controllers
{
    public class UserController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ICosmosDbService _cosmosDb;
        private readonly IHashService _hashService;

        [FromForm(Name = "user-avatar")]
        public IFormFile AvatarFileName1 { get; set; } = null!;

        [FromForm(Name = "user-PhotoFileName")]
        public IFormFile PhotoFileName1 { get; set; } = null!;
        public UserController(IConfiguration configuration, ICosmosDbService cosmosDb, IHashService hashService) 
        {
            _configuration = configuration;
            _cosmosDb = cosmosDb;
            _hashService = hashService;
            
        }
        private FeedIterator<UserSignupModel> GetFeedIterator(string sqlQueryText)
        {
            var itemsContainer = _cosmosDb.MainContainer;
            var queryDefinition = new QueryDefinition(sqlQueryText);
            return itemsContainer.GetItemQueryIterator<UserSignupModel>(queryDefinition);
        }

        public IActionResult SignUp([FromForm] UserSignupModel? model)
        {
            var itemsContainer = _cosmosDb.MainContainer;
            if (model == null)
            {
                string successMessage = "Дані не передані";
                ViewData["form"] = successMessage;
                return View();
            }
            if (String.IsNullOrEmpty(model.Login))
            {
                string successMessage = "Логін не може бути порожнім";
                ViewData["form"] = successMessage;
                return View();
            }
            if (model != null && IsUserExistsAsync(model.Login).Result)
            {
                string errorMessage = "Користувач з таким логіном вже існує";
                ViewData["form"] = errorMessage;
                return View();
            }

            if (model != null && !IsPasswordValid(model.Password))
            {
                string errorMessage = "Пароль не відповідає вимогам складності. Пароль повинен містити принаймні одну малу літеру, одну велику літеру, одну цифру і мати довжину не менше 8 символів.";
                ViewData["form"] = errorMessage;
                return View();
            }
            if (model != null && model.Password != model.RepeatPassword)
            {
                string errorMessage = "Пароль та повтор паролю не співпадають";
                ViewData["form"] = errorMessage;
                return View();
            }
            if (model != null && !model.Agree)
            {
                string errorMessage = "Необхідно дотримуватись правил сайту";
                ViewData["form"] = errorMessage;
                return View();
            }

            string newFileName = null!;
             
            if (model != null )
            {
                // Перевіряємо, чи дозволено тип файлу
                string[] allowedFileTypes = { ".jpg", ".jpeg", ".png" };
                

                // Створюємо каталог, якщо він не існує
                string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                // Створюємо нове унікальне ім'я файлу
                newFileName = Guid.NewGuid().ToString() + ".jpg";

                // Зберігаємо файл до каталогу завантажень
                string filePath = Path.Combine(uploadPath, newFileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    AvatarFileName1.CopyTo(stream);
                }
                // Зберігаємо шлях до аватарки у моделі
                model.AvatarFileName = newFileName;
            }

            if (model?.Login != null && model.Password != null)  // дані форми
            {
                model.id = Guid.NewGuid();
                model.partitionKey = model.id.ToString();
                model.moment = DateTime.Now;

                model.Password = _hashService.HashString(model.Password);
                var response = itemsContainer.CreateItemAsync<UserSignupModel>(
                    model, new PartitionKey(model.partitionKey)).Result;

                string successMessage = "Реєстрація пройшла успішно!";
                ViewData["form"] = successMessage;
            }

            return View();
        }



        public async Task<bool> IsUserExistsAsync(string login) //перевірка чи вже існує такий логін в Cosmos DB
        {
           
            var query = new QueryDefinition("SELECT VALUE COUNT(1) FROM c WHERE c.Login = @login")
                .WithParameter("@login", login);

            var queryIterator = _cosmosDb.MainContainer.GetItemQueryIterator<int>(query);
            if (queryIterator.HasMoreResults)
            {
                var count = (await queryIterator.ReadNextAsync()).SingleOrDefault();
                return count > 0;
            }

            return false;
        }
        // Метод для перевірки складності паролю
        private bool IsPasswordValid(string password)
        {
            string passwordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$";

            return Regex.IsMatch(password, passwordPattern);
        }


        public IActionResult Index()//все про юзерів
        {
            var sqlQueryText = "SELECT * FROM c WHERE c.type = 'Feedback'";
            var queryResultSetIterator = GetFeedIterator(sqlQueryText);
            
                List<UserSignupModel> feedbacks = new();
                while (queryResultSetIterator.HasMoreResults)
                {
                    FeedResponse<UserSignupModel> currentResultSet =
                        queryResultSetIterator.ReadNextAsync().Result;
                    foreach (UserSignupModel item in currentResultSet)
                    {
                        feedbacks.Add(item);
                    }
                }

                ViewData["feedbacks"] = feedbacks;
            
            
            return View(ViewData["feedbacks"] as List<UserSignupModel>);
        }


        [HttpPost]       
        public async Task<JsonResult> LogIn([FromForm] String login, [FromForm] String password)
        {
            var sqlQueryText = "SELECT * FROM c WHERE c.type = 'Feedback'";
            var queryResultSetIterator = GetFeedIterator(sqlQueryText);

            while (queryResultSetIterator.HasMoreResults)
            {
                var currentResultSet = await queryResultSetIterator.ReadNextAsync();
                foreach (UserSignupModel item in currentResultSet)
                {
                    if (item.Password == _hashService.HashString(password))
                    {
                        HttpContext.Session.SetString("AuthUserId", item.id.ToString());
                       
                        return Json(new { status = "OK" });
                    }

                }
            }
            return Json(new { status = "NO" });          
        }


        [HttpPost]
        public IActionResult SignOut()

        {
            HttpContext.Session.Remove("AuthUserId"); // видалення ключа з сесії
            HttpContext.SignOutAsync();

            return RedirectToAction("Index", "Home");
        }

       
        public IActionResult ChangeProfile([FromForm] ChangeProfileModel? realname)// зміна данних користувача(тільки імя)
        {
            var itemsContainer = _cosmosDb.MainContainer;
            var sqlQueryText = "SELECT * FROM c WHERE c.type = 'Feedback'";
            var queryResultSetIterator = GetFeedIterator(sqlQueryText);

            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<UserSignupModel> currentResultSet = queryResultSetIterator.ReadNextAsync().Result;

                foreach (UserSignupModel item in currentResultSet)
                {
                    if (realname?.NewPassword != null && item.Password == _hashService.HashString(realname.NewPassword))
                    {
                        item.RealName = realname.NewName;

                        var response = itemsContainer.ReplaceItemAsync<UserSignupModel>(
                            item, item.id.ToString(), new PartitionKey(item.partitionKey)).Result;
                        string successMessage = "Зміни було збережено успішно!";
                        ViewData["form"] = successMessage;
                    }
                }
            }

            return View();
        }
       

        public IActionResult Topic([FromForm] UserTopicModel? model)// додавання теми 
        {
            var itemsContainer = _cosmosDb.MainContainer;
            var sqlQueryText = "SELECT * FROM c WHERE c.type = 'Feedback'";
            var queryResultSetIterator = GetFeedIterator(sqlQueryText);

            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<UserSignupModel> currentResultSet = queryResultSetIterator.ReadNextAsync().Result;

                foreach (UserSignupModel item in currentResultSet)
                {
                    if (model?.PhotoPassword != null && item.Password == _hashService.HashString(model.PhotoPassword))
                    {
                        string newFileName = null!;
                        if (model != null)
                        {
                            // Перевіряємо, чи дозволено тип файлу
                            string[] allowedFileTypes = { ".jpg", ".jpeg", ".png" };


                            // Створюємо каталог, якщо він не існує
                            string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                            if (!Directory.Exists(uploadPath))
                            {
                                Directory.CreateDirectory(uploadPath);
                            }

                            // Створюємо нове унікальне ім'я файлу
                            newFileName = Guid.NewGuid().ToString() + ".jpg";

                            // Зберігаємо файл до каталогу завантажень
                            string filePath = Path.Combine(uploadPath, newFileName);
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                PhotoFileName1.CopyTo(stream);
                            }
                            // Зберігаємо шлях до аватарки у моделі
                            model.PhotoFileName = newFileName;
                        }
                        var newTopic = new UserTopicModel
                        {
                            Id = Guid.NewGuid(),
                            Title = model.Title,
                            Description = model.Description,
                            CreatedAtTopic = DateTime.Now,
                            PhotoFileName = model.PhotoFileName,
                            UserName = item.RealName
                        };

                    item.Topics.Add(newTopic);
                    var response = itemsContainer.ReplaceItemAsync<UserSignupModel>(
                        item, item.id.ToString(), new PartitionKey(item.partitionKey)).Result;
                    string successMessage = "додавання теми було  успішно!";
                    ViewData["form"] = successMessage;
                        break;
                    }

                }
                
            }
            
            return View();
        }
     
        public IActionResult OnlineUser()// який юзер онлайн  
        {
           
            return View();           
        }
      


    }
}

