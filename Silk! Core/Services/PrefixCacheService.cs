﻿using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SilkBot.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SilkBot.Services
{
    public class PrefixCacheService
    {
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<ulong, string> _cache;
        private readonly IDbContextFactory<SilkDbContext> _dbFactory;
        private readonly Stopwatch _sw = new Stopwatch();
        public PrefixResolverDelegate PrefixDelegate { get; private set; }
        
        public PrefixCacheService(ILogger<PrefixCacheService> logger, IDbContextFactory<SilkDbContext> dbFactory) 
        {
            _logger = logger;
            _cache = new ConcurrentDictionary<ulong, string>();
            _dbFactory = dbFactory;

            PrefixDelegate = ResolvePrefixAsync;
        }

        public async Task<int> ResolvePrefixAsync(DiscordMessage m)
        {
            var prefix = await RetrievePrefixAsync(m.Channel.GuildId) ?? string.Empty;
            var prefixPos = m.GetStringPrefixLength(prefix);
            return prefixPos;
        }

        public async Task<string> RetrievePrefixAsync(ulong? guildId)
        {
            if (guildId == default || guildId == 0) return null;
            else if (_cache.TryGetValue(guildId.Value, out string prefix)) return prefix;
            else return await GetPrefixFromDatabaseAsync(guildId.Value);
        }

        private async Task<string> GetPrefixFromDatabaseAsync(ulong guildId)
        {
            _logger.LogDebug("Prefix not present in cache; queuing from database.");
            _sw.Restart();
            using var db = _dbFactory.CreateDbContext();
            GuildModel guild = await db.Guilds.AsNoTracking().FirstAsync(g => g.DiscordGuildId == guildId);
            _sw.Stop();
            _logger.LogDebug($"Cached {guild.Prefix} - {guildId} in {_sw.ElapsedMilliseconds} ms.");
            _cache.TryAdd(guildId, guild.Prefix);
            return guild.Prefix;
        }

        public void UpdatePrefix(ulong id, string prefix) 
        {
            _cache.TryGetValue(id, out string currentPrefix);
            _cache.AddOrUpdate(id, prefix, (i, p) => p = prefix);
            _logger.LogDebug($"Updated prefix for {id} - {currentPrefix} -> {prefix}");
        }
    }
}