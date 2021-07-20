using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.VoiceNext;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silk.Core.EventHandlers.Messages;
using Silk.Core.SlashCommands;
using Silk.Core.SlashCommands.Commands;
using Silk.Core.Utilities;
using Silk.Core.Utilities.Bot;
using Silk.Core.Utilities.HelpFormatter;

namespace Silk.Core
{
    public sealed class Main : IHostedService
    {
        private readonly ILogger<Main> _logger;
        private readonly DiscordShardedClient _shardClient;
        private readonly BotExceptionHandler _handler;
        private readonly CommandHandler _commandHandler;
        private readonly SlashCommandExceptionHandler _slashExceptionHandler;

        public Main(
            DiscordShardedClient shardClient, 
            ILogger<Main> logger, 
            EventHelper e, 
            BotExceptionHandler handler, 
            CommandHandler commandHandler, 
            SlashCommandExceptionHandler slashExceptionHandler) // About the EventHelper: Consuming it in the ctor causes it to be constructed,
        {
            // And that's all it needs, since it subs to events in it's ctor.
            _logger = logger; // Not ideal, but I'll figure out a better way. Eventually. //
            _handler = handler;
            _commandHandler = commandHandler;
            _slashExceptionHandler = slashExceptionHandler;
            _shardClient = shardClient;
            _ = e;
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting Service");
            
            await InitializeClientExtensions();
            _logger.LogInformation("Initialized Client");
            
            await InitializeCommandsNextAsync();
            await InitializeSlashCommandsAsync();
            
            await _handler.SubscribeToEventsAsync();
            
            _logger.LogDebug("Connecting to Discord Gateway");
            await _shardClient.StartAsync();
            _logger.LogInformation("Connected to Discord Gateway as {Username}#{Discriminator}", _shardClient.CurrentUser.Username, _shardClient.CurrentUser.Discriminator);

        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Service");
            _logger.LogDebug("Disconnecting from Discord Gateway");
            await _shardClient.StopAsync();
            _logger.LogInformation("Disconnected from Discord Gateway");
        }

        private async Task InitializeClientExtensions()
        {
            _logger.LogDebug("Initializing Client");

            await _shardClient.UseCommandsNextAsync(DiscordConfigurations.CommandsNext);
            await _shardClient.UseInteractivityAsync(DiscordConfigurations.Interactivity);
            await _shardClient.UseVoiceNextAsync(DiscordConfigurations.VoiceNext);
        }

        private Task InitializeSlashCommandsAsync()
        {
            _logger.LogInformation("Initializing Slash-Commands");
            var sc = _shardClient.ShardClients[0].UseSlashCommands(DiscordConfigurations.SlashCommands);
            sc.SlashCommandErrored += _slashExceptionHandler.Handle;
            
            sc.RegisterCommands<RemindCommands>();
            sc.RegisterCommands<TagCommands>();
            sc.RegisterCommands<AvatarCommands>();

            return Task.CompletedTask;
        }

        private async Task InitializeCommandsNextAsync()
        {
            _logger.LogInformation("Initializing Command Framework");

            var t = Stopwatch.StartNew();
            var asm = Assembly.GetEntryAssembly();
            IReadOnlyDictionary<int, CommandsNextExtension>? cnext = await _shardClient.GetCommandsNextAsync();

            foreach (var cnextExt in cnext.Values)
            {
                cnextExt.RegisterCommands(asm);
                cnextExt.SetHelpFormatter<HelpFormatter>();
                cnextExt.RegisterConverter(new MemberConverter());
                cnextExt.CommandExecuted += _commandHandler.AddCommandInvocation;
            }

            t.Stop();
            int registeredCommands = cnext.Values.Sum(r => r.RegisteredCommands.Count);

            _logger.LogDebug("Registered {Commands} Commands for {Shards} Shards in {Time} ms", registeredCommands, _shardClient.ShardClients.Count, t.ElapsedMilliseconds);
            _logger.LogInformation("Initialized Command Framework");
        }
    }
}