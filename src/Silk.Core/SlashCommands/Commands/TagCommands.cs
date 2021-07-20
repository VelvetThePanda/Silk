using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using MediatR;
using Silk.Core.Data.MediatR.Tags;
using Silk.Core.Data.MediatR.Users;
using Silk.Core.Data.Models;
using Silk.Core.Services.Server;
using Silk.Core.SlashCommands.Attributes;
using Silk.Core.Types;
using Silk.Extensions.DSharpPlus;

namespace Silk.Core.SlashCommands.Commands
{
    public class TagCommands : SlashCommandModule
    {
        [SlashCommandGroup("tag", "Tag related commands!")]
        public class TagCommandGroup : SlashCommandModule
        {
            private readonly string[] _reservedWords =
            {
                "create", "update", "delete",
                "alias", "info", "claim",
                "raw", "list"
            };

            
            private readonly IMediator _mediator;
            private readonly TagService _tags;
            public TagCommandGroup(TagService tags, IMediator mediator)
            {
                _tags = tags;
                _mediator = mediator;
            }

            
            [RequireBot]
            [RequireGuild]
            [SlashCommand("delete", "Delete a tag. This can't be undone!")]
            public async Task Delete(InteractionContext ctx, [Option("tag", "The tag to delete")] string tagName)
            {
                await ctx.CreateThinkingResponseAsync();
                Tag? tag = await _tags.GetTagAsync(tagName, ctx.Interaction.GuildId.Value);
                
                if (tag is null)
                {
                    await ctx.EditResponseAsync(new() {Content = "Sorry, but I can't delete something that doesn't exist!"});
                    return;
                }

                if (tag.OwnerId == ctx.User.Id)
                {
                    await _tags.RemoveTagAsync(tagName, ctx.Interaction.GuildId.Value);
                    await ctx.EditResponseAsync(new() {Content = $"{(tag.OriginalTag is null ? "Tag" : "Alias")} {Formatter.Bold(tagName)} {(tag.Aliases?.Any() ?? false ? "" : "and all associated aliases")} successfully deleted!"});
                    return;
                }
                
                User? user = await _mediator.Send(new GetUserRequest(ctx.Interaction.GuildId.Value, ctx.User.Id));
                var staff = user?.Flags.HasFlag(UserFlag.Staff) ?? false;

                if (!staff)
                {
                    await ctx.EditResponseAsync(new() {Content = "You don't own this tag!"});
                    return;
                }
                
                await _tags.RemoveTagAsync(tagName, ctx.Interaction.GuildId.Value);
                await ctx.EditResponseAsync(new() {Content = $"{(tag.OriginalTag is null ? "Tag" : "Alias")} {Formatter.Bold(tagName)} {(tag.Aliases?.Any() ?? false ? "" : "and all associated aliases")} successfully deleted!"});
            }
            
            [RequireGuild]
            [SlashCommand("info", "Get info about a tag!")]
            public async Task Info(InteractionContext ctx, [Option("tag", "The tag you want to get information about.")] string tagName)
            {
                await ctx.CreateThinkingResponseAsync();
                Tag? tag = await _tags.GetTagAsync(tagName, ctx.Interaction.GuildId.Value);

                if (tag is null)
                {
                    await ctx.EditResponseAsync(new() {Content = "Sorry, but I couldn't find that tag!"});
                    return;
                }
                
                DiscordUser tagOwner = await ctx.Client.GetUserAsync(tag.OwnerId);
                
                DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Blurple)
                    .WithAuthor(tagOwner.Username, iconUrl: tagOwner.AvatarUrl)
                    .WithTitle(tag.Name)
                    .AddField("Uses:", tag.Uses.ToString())
                    .WithFooter("Created:")
                    .WithTimestamp(tag.CreatedAt);

                if (tag.OriginalTag is not null)
                    builder.AddField("Original:", tag.OriginalTag.Name);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(builder));
            }
            
            [RequireGuild]
            [SlashCommand("create", "Create a tag!")]
            public async Task Create(InteractionContext ctx,
                [Option("name", "The name of the tag")] string tagname,
                [Option("content", "The content of the tag")] string content)
            {
                await ctx.CreateThinkingResponseAsync();

                if (_reservedWords.Contains(tagname.ToLower()))
                {
                    await ctx.EditResponseAsync(new() {Content = $"Sorry, but you can't create a tag that contains a reserved word! Reserved words are: {string.Join(", ", _reservedWords)}"});
                    return;
                }
                
                TagCreationResult result = await _tags.CreateTagAsync(tagname, content, ctx.Interaction.GuildId.Value, ctx.User.Id);

                await ctx.EditResponseAsync(new() {Content = result.Success ? $"Successfully created tag {Formatter.Bold(tagname)}." : result.Reason});
            }

            [RequireGuild]
            [SlashCommand("alias", "Point a tag to another tag!")]
            public async Task Alias(
                InteractionContext ctx,
                [Option("tag", "The tag to alias")] string tagname,
                [Option("alias", "The name of th alias")] string aliasName)
            {
                await ctx.CreateThinkingResponseAsync();

                TagCreationResult result = await _tags.AliasTagAsync(tagname, aliasName, ctx.Interaction.GuildId.Value, ctx.User.Id);

                await ctx.EditResponseAsync(new() {Content = result.Reason ?? $"Successfuly aliased tag {Formatter.Bold(aliasName)} to {Formatter.Bold(tagname)}"});
            }
            
            [RequireGuild]
            [SlashCommand("raw", "View the raw content of a tag!")]
            public async Task Raw(InteractionContext ctx, [Option("tag", "The tag to view")] string tag)
            {
                await ctx.CreateThinkingResponseAsync();

                Tag? dbTag = await _tags.GetTagAsync(tag, ctx.Interaction.GuildId.Value);

                if (dbTag is null)
                {
                    await ctx.EditResponseAsync(new() {Content = "Sorry, but I couldn't find a tag by that name!"});
                    return;
                }

                await ctx.EditResponseAsync(new() {Content = Formatter.Sanitize(dbTag.Content)});
            }
            
            [RequireGuild]
            [SlashCommand("list", "List server tags!")]
            public async Task List(InteractionContext ctx)
            {
                await ctx.CreateThinkingResponseAsync();

                IEnumerable<Tag> tags = await _tags.GetGuildTagsAsync(ctx.Interaction.GuildId.Value);
                if (!tags.Any())
                {
                    await ctx.EditResponseAsync(new() {Content = "This server doesn't have any tags!"});
                    return;
                }

                string allTags = string.Join('\n', tags
                    .Select(t =>
                    {
                        var s = $"`{t.Name}`";
                        if (t.OriginalTagId is not null) s += $" → `{t.OriginalTag!.Name}`";
                        return s;
                    }));
                DiscordEmbedBuilder? builder = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Blurple)
                    .WithTitle($"Tags in {ctx.Guild.Name}:")
                    .WithFooter($"Silk! | Requested by {ctx.User.Id}");

                if (tags.Count() < 10)
                {
                    builder.WithDescription(allTags);
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(builder));
                }
                else
                {
                    InteractivityExtension? interactivity = ctx.Client.GetInteractivity();

                    IEnumerable<Page>? pages = interactivity.GeneratePagesInEmbed(allTags, SplitType.Line, builder);
                    await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages, timeoutoverride: null);
                }
            }
            
            [RequireGuild]
            [SlashCommand("use", "Display a tag!")]
            public async Task Use(InteractionContext ctx, [Option("tag-name", "What's the name of the tag you want to use?")] string tagname)
            {

                Tag? dbtag = await _tags.GetTagAsync(tagname, ctx.Interaction.GuildId.Value);
                if (dbtag is null)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new() {Content = "Sorry, but I don't see a tag by that name!", IsEphemeral = true});
                    return;
                }

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new() {Content = dbtag.Content});
                await _mediator.Send(new UpdateTagRequest(dbtag.Name, ctx.Interaction.GuildId.Value) {Uses = dbtag.Uses + 1});
            }

            [RequireGuild]
            [SlashCommand("search", "Search for a tag!")]
            public async Task Search(InteractionContext ctx, [Option("tag-name", "What tag do you want to look for?")] string tagName)
            {
                await ctx.CreateThinkingResponseAsync();

                IEnumerable<Tag> tags = await _tags.SearchTagsAsync(tagName, ctx.Interaction.GuildId.Value);

                if (!tags.Any())
                {
                    await ctx.EditResponseAsync(new() {Content = "Sorry, but I couldn't find any tags matching that search!"});
                }
                else
                {
                    string allTags = string.Join("\n\n", tags.Select(t => $"`{t.Name}`{(t.OriginalTag is null ? "" : $" → `{t.OriginalTag!.Name}`")} - <@{t.OwnerId}>"));
                    
                    DiscordEmbedBuilder? builder = new DiscordEmbedBuilder()
                        .WithColor(DiscordColor.Blurple)
                        .WithTitle($"Result for {tagName}:")
                        .WithDescription(allTags);

                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(builder));
                }
            }
            
            [RequireGuild]
            [SlashCommand("by", "See all a given user's tags!")]
            public async Task ListByUser(InteractionContext ctx, [Option("user", "Who's tags do you want to see?")] DiscordUser user)
            {
                await ctx.CreateThinkingResponseAsync();

                IEnumerable<Tag> tags = await _tags.GetUserTagsAsync(user.Id, ctx.Interaction.GuildId.Value);

                if (!tags.Any())
                {
                    await ctx.EditResponseAsync(new() {Content = "Looks like that user doesn't actually have any tags!"});
                    return;
                }

                string allTags = string.Join('\n', tags
                    .Select(t =>
                    {
                        var s = $"`{t.Name}`";

                        if (t.OriginalTagId is not null)
                            s += $" → `{t.OriginalTag!.Name}`";

                        return s;
                    }));

                DiscordEmbedBuilder? builder = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Blurple)
                    .WithTitle($"Tags for {user.Username}:")
                    .WithFooter($"Silk! | Requested by {ctx.User.Id}");

                if (tags.Count() < 10)
                {
                    builder.WithDescription(allTags);
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(builder));
                }
                else
                {
                    InteractivityExtension? interactivity = ctx.Client.GetInteractivity();

                    IEnumerable<Page>? pages = interactivity.GeneratePagesInEmbed(allTags, SplitType.Line, builder);
                    await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages, timeoutoverride: null);
                }

            }
            
            [RequireGuild]
            [SlashCommand("server", "Show the tags on this server!")]
            public async Task Server(InteractionContext ctx)
            {
                await ctx.CreateThinkingResponseAsync();
                
                IEnumerable<Tag> tags = await _tags.GetGuildTagsAsync(ctx.Interaction.GuildId.Value);
                if (!tags.Any())
                {
                    await ctx.EditResponseAsync(new() {Content = "This server doesn't have any tags! You could be the first."});
                    return;
                }
                
                string allTags = string.Join('\n', tags.Take(30)
                    .Select(t =>
                    {
                        var s = $"`{t.Name}`";
                        if (t.OriginalTagId is not null) s += $" → `{t.OriginalTag!.Name}`";
                        return s;
                    }));
                
                DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Blurple)
                    .WithTitle($"Tags in {ctx.Guild.Name}:")
                    .WithDescription(allTags + (tags.Count() > 30 ? $"\n+ {tags.Count() - 30} more..." : ""))
                    .WithFooter($"Silk! | Requested by {ctx.User.Id}");

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(builder));
            }
            
            [RequireBot]
            [RequireGuild]
            [SlashCommand("claim", "Claim a tag. Owner must not be in server.")]
            public async Task Claim(InteractionContext ctx, [Option("tag", "What tag do you want to claim? **Requires staff**")] string tag)
            {
                await ctx.CreateThinkingResponseAsync();

                Tag? dbTag = await _tags.GetTagAsync(tag, ctx.Interaction.GuildId.Value);
                
                if (dbTag is null)
                {
                    await ctx.EditResponseAsync(new() {Content = "Sorry, but that tag doesn't exist!"});
                    return;
                }

                var exists = ctx.Guild.Members.ContainsKey(dbTag.OwnerId);
                
                User? user = await _mediator.Send(new GetUserRequest(ctx.Interaction.GuildId.Value, ctx.User.Id));
                var staff = user?.Flags.HasFlag(UserFlag.Staff) ?? false;

                if (!staff)
                {
                    await ctx.EditResponseAsync(new() {Content = "Sorry, but you're not allowed to claim tags!"});
                    return;
                }
                
                if (exists)
                {
                    await ctx.EditResponseAsync(new() {Content = "The tag owner is still on the server."});
                    return;
                }

                await _tags.ClaimTagAsync(tag, ctx.Interaction.GuildId.Value, ctx.User.Id);
                await ctx.EditResponseAsync(new() {Content = "Successfully claimed tag!"});

            }
        }
    }
}