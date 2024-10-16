using EF;
using EF.Repositories;
using Infrastructure.IRepositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using TechnicalSupportBot.Controllers;
using TechnicalSupportBot;

var serviceProvider = new ServiceCollection()
    .AddScoped<IMappingRepository, MappingRepository>()
    .AddScoped<IQuestionRepository, QuestionRepository>()
    .AddScoped<IAnswerRepository, AnswerRepository>()
    .AddScoped<IUserRepository, UserRepository>()
    .AddScoped<QuestionController>()
    .AddScoped<AnswerController>()
    .AddScoped<UserController>()
    .AddDbContext<ProjectContext>(options =>
        options.UseNpgsql("")) 
    .BuildServiceProvider();


var questionController = serviceProvider.GetService<QuestionController>();
var answerController = serviceProvider.GetService<AnswerController>();
var userController = serviceProvider.GetService<UserController>();

var telegramBot = new TelegramBot("", questionController, answerController, userController);

telegramBot.ListenForMessagesAsync().Wait();


