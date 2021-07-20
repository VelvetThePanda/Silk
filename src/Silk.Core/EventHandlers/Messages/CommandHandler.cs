using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using MediatR;
using Microsoft.Extensions.Logging;
using Silk.Core.Data.MediatR.CommandInvocations;
using Silk.Core.Services.Interfaces;

namespace Silk.Core.EventHandlers.Messages
{
    public sealed class CommandHandler
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CommandHandler> _logger;
        private readonly IPrefixCacheService _prefixService;
        public CommandHandler(IPrefixCacheService prefixService, IMediator mediator, ILogger<CommandHandler> logger)
        {
            _prefixService = prefixService;
            _mediator = mediator;
            _logger = logger;
        }

        public Task AddCommandInvocation(CommandsNextExtension ext, CommandEventArgs args) =>
            _mediator.Send(new AddCommandInvocationRequest(args.Context.User.Id, args.Context.Guild?.Id, args.Command.QualifiedName));

        public async Task Handle(DiscordClient client, MessageCreateEventArgs args)
        {
            bool isBot = args.Author.IsBot;
            bool isEmpty = string.IsNullOrEmpty(args.Message.Content);

            if (isBot || isEmpty)
                return;

            DiscordUser bot = client.CurrentUser;
            CommandsNextExtension? commandsNext = client.GetCommandsNext();
            string prefix = _prefixService.RetrievePrefix(args.Guild?.Id);

            int prefixLength = GetPrefixLength(prefix, args.Message, bot);

            if (prefixLength is -1)
                return;

            string commandString = args.Message.Content[prefixLength..];

            Command? command = commandsNext.FindCommand(commandString, out string arguments);

            if (command is null)
            {
                _logger.LogWarning("Could not find command. Message: {Message}", args.Message.Content);
                return;
            }

            CommandContext context = commandsNext.CreateContext(args.Message, prefix, command, arguments);

            _ = Task.Run(async () => await commandsNext.ExecuteCommandAsync(context));
        }

        private static int GetPrefixLength(string prefix, DiscordMessage message, DiscordUser currentUser)
        {
            if (message.Channel is DiscordDmChannel)
                return 0;

            return message.Content.StartsWith(currentUser.Mention) ?
                message.GetMentionPrefixLength(currentUser) :
                message.GetStringPrefixLength(prefix);
        }
    }
}