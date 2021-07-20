using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Humanizer;
using Humanizer.Localisation;
using Silk.Core.Data.Models;
using Silk.Core.Services.Server;
using Silk.Core.Utilities.HelpFormatter;
using Silk.Extensions;

namespace Silk.Core.Commands.General
{
    [Category(Categories.General)]
    public class RemindersCommand : BaseCommandModule
    {
        [Command]
        public async Task Reminders(CommandContext ctx)
        {
            DiscordUser? user = ctx.User;
            DiscordChannel? channel = ctx.Channel;
            string? content = ctx.Message.Content;
            string? prefix = ctx.Prefix;
            Command? command = ctx.CommandsNext.FindCommand("remind list", out _);
            CommandContext? fctx = ctx.CommandsNext.CreateFakeContext(user, channel, content, prefix, command);

            await ctx.CommandsNext.ExecuteCommandAsync(fctx);
        }
    }

    [RequireGuild]
    [Group("remind")]
    [Aliases("reminder")]
    [Category(Categories.General)]
    public class RemindCommand : BaseCommandModule
    {
        private readonly ReminderService _reminders;
        public RemindCommand(ReminderService reminders)
        {
            _reminders = reminders;
        }

        [GroupCommand]
        [Description("Creates a reminder")]
        public async Task Remind(CommandContext ctx, TimeSpan time, [RemainingText] string reminder)
        {
            ulong? replyId = ctx.Message.ReferencedMessage?.Id;
            ulong? authorId = ctx.Message.ReferencedMessage?.Author?.Id;
            string? replyContent = ctx.Message.ReferencedMessage?.Content;

            await _reminders.CreateReminder(DateTime.UtcNow + time, ctx.User.Id, ctx.Channel.Id,
                ctx.Message.Id, ctx.Guild?.Id ?? 0, reminder, ctx.Message.ReferencedMessage is not null, ReminderType.Once, replyId, authorId, replyContent);
            await ctx.RespondAsync($"Alrighty, I'll remind you in {time.Humanize(2, minUnit: TimeUnit.Second)}: {reminder.Pull(..200)}");
        }

        // RECURRING REMINDERS //

        [Command]
        public Task Hourly(CommandContext ctx, TimeSpan offset, [RemainingText] string reminder)
        {
            return CreateRecurringReminder(ctx, reminder, ReminderType.Hourly, offset);
        }


        [Command]
        public Task Hourly(CommandContext ctx, [RemainingText] string reminder) => CreateRecurringReminder(ctx, reminder, ReminderType.Hourly, TimeSpan.Zero);

        [Command]
        public Task Daily(CommandContext ctx, TimeSpan offset, [RemainingText] string reminder) => CreateRecurringReminder(ctx, reminder, ReminderType.Daily, offset);

        [Command]
        public Task Daily(CommandContext ctx, [RemainingText] string reminder)
        {
            return CreateRecurringReminder(ctx, reminder, ReminderType.Daily, TimeSpan.Zero);
        }

        [Command]
        public Task Weekly(CommandContext ctx, TimeSpan offset, [RemainingText] string reminder) => CreateRecurringReminder(ctx, reminder, ReminderType.Weekly, offset);

        [Command]
        public Task Weekly(CommandContext ctx, [RemainingText] string reminder) => CreateRecurringReminder(ctx, reminder, ReminderType.Weekly, TimeSpan.Zero);

        [Command]
        public Task Monthly(CommandContext ctx, TimeSpan offset, [RemainingText] string reminder) => CreateRecurringReminder(ctx, reminder, ReminderType.Monthly, offset);

        [Command]
        public Task Monthly(CommandContext ctx, [RemainingText] string reminder) => CreateRecurringReminder(ctx, reminder, ReminderType.Monthly, TimeSpan.Zero);

        private async Task CreateRecurringReminder(CommandContext ctx, string reminder, ReminderType type, TimeSpan offset)

        {
            DateTime time = type switch
            {
                ReminderType.Hourly => DateTime.UtcNow + offset + TimeSpan.FromHours(1),
                ReminderType.Daily => DateTime.UtcNow + offset + TimeSpan.FromDays(1),
                ReminderType.Weekly => DateTime.UtcNow + offset + TimeSpan.FromDays(7),
                ReminderType.Monthly => DateTime.UtcNow + offset + TimeSpan.FromDays(30),
                ReminderType.Once => throw new ArgumentException($"{nameof(ReminderType.Once)} is not a supported type."),
                _ => throw new ArgumentException($"Unknown value for type of {nameof(ReminderType)}")
            };

            await _reminders.CreateReminder(time, ctx.User.Id, ctx.Channel.Id, ctx.Message.Id, ctx.Guild.Id, reminder, false, type);
            await ctx.RespondAsync($"Alrighty! I'll remind you {type.Humanize(LetterCasing.LowerCase)}: {reminder}");
        }


        // NON-RECURRING REMINDERS //
        [Command]
        [Description("Gives you a list of your reminders")]
        public async Task List(CommandContext ctx)
        {
            IEnumerable<Reminder> reminders = await _reminders.GetRemindersAsync(ctx.User.Id);

            if (!reminders.Any())
            {
                await ctx.RespondAsync("You don't have any active reminders!");
            }
            else
            {
                string[] allReminders = reminders
                    .Select(r =>
                    {
                        string s = r.Type is ReminderType.Once ?
                            $"`{r.Id}` → Expiring {r.Expiration.Humanize()}:\n" :
                            $"`{r.Id}` → Occurs **{r.Type.Humanize(LetterCasing.LowerCase)}**:\n";

                        if (r.ReplyId is not null)
                            s += $"[reply](https://discord.com/channels/{r.GuildId}/{r.ChannelId}/{r.ReplyId})\n";

                        s += $"`{r.MessageContent}`";
                        return s;
                    })
                    .ToArray();

                string remindersString = allReminders.Join("\n");

                if (remindersString.Length <= 2048)
                {
                    var builder = new DiscordEmbedBuilder();

                    builder.WithColor(DiscordColor.Blurple)
                        .WithTitle($"Reminders for {ctx.User.Username}:")
                        .WithFooter($"Silk! | Requested by {ctx.User.Id}")
                        .WithDescription(remindersString);

                    await ctx.RespondAsync(builder);
                }
                else
                {
                    InteractivityExtension? interactivity = ctx.Client.GetInteractivity();

                    List<Page>? pages = allReminders
                        .Select(reminder => new Page("You have too many reminders to fit in one embed, so I've paginated it for you!",
                            new DiscordEmbedBuilder()
                                .WithColor(DiscordColor.Blurple)
                                .WithTitle($"Reminders for {ctx.User.Username}:")
                                .WithDescription(reminder)
                                .WithFooter($"Silk! | Requested by {ctx.User.Id}")))
                        .ToList();
                    await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages, timeoutoverride: null);
                }
            }
        }

        [Command]
        [Aliases("cancel")]
        [Description("Removes one of your reminders based on the id given")]
        public async Task Remove(CommandContext ctx, int id)
        {
            if ((await _reminders.GetRemindersAsync(ctx.User.Id))?.Any(r => r.Id == id) ?? false)
            {
                await _reminders.RemoveReminderAsync(id);
                await ctx.RespondAsync("Successfully removed reminder.");
            }
            else
            {
                await ctx.RespondAsync("I couldn’t find that reminder!");
            }
        }
    }
}