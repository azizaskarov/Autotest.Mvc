using System.Net.NetworkInformation;
using Autotest.Mvc.Models;
using Autotest.Mvc.Repositories;

namespace Autotest.Mvc.Services;

public class UsersService
{
    private const string UserIdCookieKey = "user_id";
    public readonly TicketRepository TicketRepository;
    public readonly UserRepository UserRepository;
    private readonly QuestionService _questionService;

    public UsersService(TicketRepository ticketRepository, UserRepository userRepository, QuestionService questionService)
    {
        TicketRepository = ticketRepository;
        UserRepository = userRepository;
        _questionService = questionService;
    }

    public void Register(CreateUserModel createUser, HttpContext httpContext)
    {
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Name = createUser.Name!,
            Password = createUser.Password!,
            Username = createUser.Username!,
            PhotoPath = SavePhoto(createUser.Photo!)
        };

        user.CurrentTicket = user.CurrentTicketIndex == null ? null
            : TicketRepository.GetTicket(user.CurrentTicketIndex.Value);
        user.Tickets = TicketRepository.GetTicketList(user.Id);

        CreateUserTickets(user);

        UserRepository.AddUser(user);

        httpContext.Response.Cookies.Append(UserIdCookieKey, user.Id);
    }

    public bool LogIn(SignInUserModel signInUserModel, HttpContext httpContext)
    {
        var user = UserRepository.GetUserByUsername(signInUserModel.Username);

        if (user == null || user.Password != signInUserModel.Password)
            return false;

        //v2
        if (user?.Password != signInUserModel.Password)
            return false;

        httpContext.Response.Cookies.Append(UserIdCookieKey, user.Id);

        return true;
    }

    public User? GetCurrentUser(HttpContext context)
    {
        if (context.Request.Cookies.ContainsKey(UserIdCookieKey))
        {
            var userId = context.Request.Cookies[UserIdCookieKey];
            var user = UserRepository.GetUserById(userId);

            return user;
        }

        return null;
    }

    public bool IsLoggedIn(HttpContext context)
    {
        if (!context.Request.Cookies.ContainsKey(UserIdCookieKey)) return false;

        var userId = context.Request.Cookies[UserIdCookieKey];
        var user = UserRepository.GetUserById(userId);

        return user != null;
    }

    public void LogOut(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Delete(UserIdCookieKey);
    }

    private void CreateUserTickets(User user)
    {
        for (var i = 0; i < _questionService.TicketsCount; i++)
        {
            var startIndex = i * _questionService.TicketQuestionsCount + 1;
            
            TicketRepository.AddTicket(new Ticket()
            {
                Id = i,
                UserId = user.Id,
                CurrentQuestionIndex = startIndex,
                StartIndex = startIndex,
                QuestionsCount = _questionService.TicketQuestionsCount
            });
        }
    }

    private string SavePhoto(IFormFile file)
    {
        if (!Directory.Exists("wwwroot/UserImages"))
            Directory.CreateDirectory("wwwroot/UserImages");

        var fileName = Guid.NewGuid() + ".jpg";
        var ms = new MemoryStream();
        file.CopyTo(ms);
        System.IO.File.WriteAllBytes(Path.Combine("wwwroot", "UserImages", fileName), ms.ToArray());

        return "/UserImages/" + fileName;
    }
}