using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.EventArgs;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.Extensions.Logging;
using Serilog;
using Silk.Extensions;

namespace Silk.Core.Utilities.Bot
{
    public class BotExceptionHandler
    {
        private readonly DiscordShardedClient _client;
        private readonly ILogger<BotExceptionHandler> _logger;

        public BotExceptionHandler(ILogger<BotExceptionHandler> logger, DiscordShardedClient client)
        {
            _logger = logger;
            _client = client;
            _client.ClientErrored += OnClientErrored;
            _client.SocketClosed += OnSocketErrored;
        }

        private async Task OnCommandErrored(CommandsNextExtension c, CommandErrorEventArgs e)
        {
            Task task = e.Exception switch
            {
                ChecksFailedException f => ShowChecksFailedMessage(e, c.Client, f),

                //Command parsing exceptions //
                ArgumentException {Message: "Could not find a suitable overload for the command."}
                    => SendHelpAsync(c.Client, e.Command.QualifiedName, e.Context),
                InvalidOperationException {Message: "No matching subcommands were found, and this group is not executable."}
                    => SendHelpAsync(c.Client, e.Command.QualifiedName, e.Context),

                _ => Task.CompletedTask
            };
            await task;

            Log.Logger.ForContext(e.Command.Module.ModuleType).Warning(e.Exception, "Something went wrong!");
            //_logger.LogWarning(e.Exception.InnerException ?? e.Exception , "A command threw an exception! Command: {CommandName}", e.Command.Name);
        }

        [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter", Justification = "I don't care.")]
        private static async Task ShowChecksFailedMessage(CommandEventArgs e, DiscordClient c, ChecksFailedException cf)
        {
            string owner = c.CurrentApplication.Owners.Select(o => $"{o.Username}#{o.Discriminator}").Join(", ");
            foreach (CheckBaseAttribute check in cf.FailedChecks)
            {
                string? message = check switch
                {
                    RequireOwnerAttribute => $"My owners consist of: {owner}. {cf.Context.User.Username}#{cf.Context.User.Discriminator} doesn't look like any of those names!",
                    RequireNsfwAttribute => "As much as I'd love to, I've gotta keep the hot stuff to the right channels.",
                    RequireFlagAttribute f => $"Heh. You need to be {f.RequisiteUserFlag.Humanize(LetterCasing.Title)} for that.",
                    CooldownAttribute cd => $"Sorry, but this command has a cooldown! You can use it {cd.MaxUses} time(s) every {cd.Reset.Humanize(2, minUnit: TimeUnit.Second)}!",
                    RequireUserPermissionsAttribute p => $"You need to have permission to {p.Permissions.Humanize(LetterCasing.Title)} to run this!",
                    RequireDirectMessageAttribute => "Psst. You need to be in DMs to run this!",
                    RequireGuildAttribute => "As it would turn out, you can't run this in DMs!",
                    _ => null
                };

                if (message is null) continue;

                await e.Context.RespondAsync(message);
                return;
            }
        }

        private async Task OnClientErrored(DiscordClient c, ClientErrorEventArgs e)
        {
            if (e.Exception.Message.Contains("event"))
                _logger.LogWarning("[{Event}] Timed out!", e.EventName);
            else
                _logger.LogWarning(e.Exception, "Client threw an exception!");
        }
        private async Task OnSocketErrored(DiscordClient c, SocketCloseEventArgs e)
        {
            if (e.CloseCode is 4014)
                _logger.LogCritical("Missing intents! Enable them on the developer dashboard (discord.com/developers/applications/{AppId})", _client.CurrentApplication.Id);
        }

        private async Task SendHelpAsync(DiscordClient c, string commandName, CommandContext originalContext)
        {
            CommandsNextExtension? cnext = c.GetCommandsNext();
            Command? cmd = cnext.RegisteredCommands["help"];
            CommandContext? ctx = cnext.CreateContext(originalContext.Message, null, cmd, commandName);
            await cnext.ExecuteCommandAsync(ctx);
        }
        public async Task SubscribeToEventsAsync()
        {
            _logger.LogInformation("Hooking task and command exception events");
            TaskScheduler.UnobservedTaskException += async (_, e) => _logger.LogError("Task Scheduler caught an unobserved exception: {Exception}", e.Exception);

            IEnumerable<CommandsNextExtension?> commandsNext = (await _client.GetCommandsNextAsync()).Values;
            
            foreach (CommandsNextExtension? c in commandsNext)
                c!.CommandErrored += OnCommandErrored;
            _logger.LogDebug("Registered command exception-handler for {Shards} shard(s)", commandsNext.Count());
        }
    }
}