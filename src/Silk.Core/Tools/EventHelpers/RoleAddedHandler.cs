﻿using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;
using Silk.Core.Database;
using Silk.Core.Database.Models;
using Silk.Core.Services.Interfaces;
using Silk.Extensions;

namespace Silk.Core.Tools.EventHelpers
{
    public class RoleAddedHandler
    {
        private readonly IDatabaseService _dbService;
        private readonly ILogger<RoleAddedHandler> _logger;
        public RoleAddedHandler(IDatabaseService dbService, ILogger<RoleAddedHandler> logger)
        {
            _dbService = dbService;
            _logger = logger;
        }

        public async Task CheckStaffRole(DiscordClient c, GuildMemberUpdateEventArgs e)
        {
            if (e.RolesBefore.Count >= e.RolesAfter.Count) return;
            _ = Task.Run(async () =>
            {

                Guild guild = (await _dbService.GetGuildAsync(e.Guild.Id))!;
                if (e.RolesAfter.Except(e.RolesBefore).Any(r => r.HasPermission(Permissions.KickMembers | Permissions.ManageMessages)))
                {
                    // I was really stupid to make the oversight of picking the first user in the Database instead of the first user in the guild. ~Velvet. //
                    User? user = guild.Users.FirstOrDefault(u => u.Id == e.Member.Id);
                    
                    if (user is not null && !user.Flags.Has(UserFlag.Staff))
                    {
                        user.Flags.Add(UserFlag.Staff);
                    }
                    else
                    {
                        user = new() {Id = e.Member.Id, Flags = UserFlag.Staff, Guild = guild};
                        guild.Users.Add(user);
                        await _dbService.UpdateGuildAsync(guild);
                    }
                    _logger.LogDebug("Logged user as staff from role added event");
                    await _dbService.UpdateGuildUserAsync(user);
                }
            });
        }
    }
}