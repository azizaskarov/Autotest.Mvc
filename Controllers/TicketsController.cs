using Autotest.Mvc.Models;
using Autotest.Mvc.Services;
using Microsoft.AspNetCore.Mvc;

namespace Autotest.Mvc.Controllers;

public class TicketsController : Controller
{
    private readonly UsersService _usersService;
    private readonly QuestionService _questionService;
    
    public TicketsController(UsersService usersService, QuestionService questionService)
    {
        _usersService = usersService;
        _questionService = questionService;
    }

    public IActionResult Index()
    {
        var user = _usersService.GetCurrentUser(HttpContext);

        if (user == null)
            return RedirectToAction("SignIn", "Users");
        
        return View(user);
    }

    public IActionResult StartTicket(int ticketIndex)
    {
        var user = _usersService.GetCurrentUser(HttpContext);

        if (user == null)
            return RedirectToAction("SignIn", "Users");

        var tickets = _usersService.TicketRepository.GetTicketList(user.Id);

        if ( tickets[0].Id > ticketIndex || ticketIndex > tickets[tickets.Count-1].Id)
            return View("NotFound");

        user.CurrentTicketIndex = ticketIndex;
        user.CurrentTicket = user.CurrentTicketIndex == null ? null
            : _usersService.TicketRepository.GetTicket(user.CurrentTicketIndex.Value);

        _usersService.UserRepository.UpdateUser(user);

        return RedirectToAction("Questions", new { id = user.CurrentTicket?.StartIndex });
    }

    public IActionResult Questions(int id, int? choiceIndex = null)
    {
        User? user = _usersService.GetCurrentUser(HttpContext);
        if (user == null)
            return RedirectToAction("SignIn", "Users");

        if (user.CurrentTicketIndex == null)
            return RedirectToAction("Index");

        if (id > user.CurrentTicketIndex * 10 + 10)
        {
            return RedirectToAction(nameof(Result));
        }

        var question = _questionService.Questions?.FirstOrDefault(x => x.Id == id);

        if (question == null)
            return View("NotFound");

        ViewBag.Question = question;
        ViewBag.IsAnswer = choiceIndex != null;

        if (choiceIndex != null)
        {
            var answer = new TicketQuestionAnswer()
            {
                TicketId = user.CurrentTicketIndex.Value,
                ChoiceIndex = choiceIndex.Value,
                QuestionIndex = id,
                CorrectIndex = question.Choices.IndexOf(question.Choices.First(c => c.Answer))
            };
            
            _usersService.TicketRepository.AddTicketAnswer(answer);

            ViewBag.Answer = answer;
        }

        user = _usersService.GetCurrentUser(HttpContext);
        return View(user);
    }

    public IActionResult Result()
    {
        var user = _usersService.GetCurrentUser(HttpContext);

        if (user == null)
            return RedirectToAction("SignIn", "Users");

        return View(user);
    }
}