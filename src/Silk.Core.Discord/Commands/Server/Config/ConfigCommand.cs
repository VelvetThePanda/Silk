﻿using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Silk.Core.Data.Models;
using Silk.Core.Discord.Services.Interfaces;
using Silk.Core.Discord.Utilities;
using Silk.Shared.Abstractions.DSharpPlus.Interfaces;

namespace Silk.Core.Discord.Commands.Server.Config
{
    [RequireGuild]
    [Group("config")]
    [RequireFlag(UserFlag.Staff)]
    public partial class ConfigCommand : BaseCommandModule
    {
        private readonly IMessageSender _sender;
        private readonly IInputService _input;

        public ConfigCommand(IMessageSender sender, IInputService input)
        {
            _sender = sender;
            _input = input;
        }

        [GroupCommand]
        public async Task ConfigWrapper(CommandContext ctx) =>
            await ctx.RespondAsync($"See `{ctx.Prefix}help config` instead.");
    }
}