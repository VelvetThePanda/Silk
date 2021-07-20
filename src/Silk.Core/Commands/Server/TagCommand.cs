using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using MediatR;
using Silk.Core.Data.MediatR.Tags;
using Silk.Core.Data.MediatR.Users;
using Silk.Core.Data.Models;
using Silk.Core.Services.Server;
using Silk.Core.Types;
using Silk.Core.Utilities.HelpFormatter;
using Silk.Extensions;

namespace Silk.Core.Commands.Server
{
    [RequireGuild]
    [Group("tag")]
    [Category(Categories.Server)]
    public class TagCommand : BaseCommandModule
    {
        private readonly IMediator _mediator;
        private readonly string[] _reservedWords =
        {
            "create", "update", "delete",
            "alias", "info", "claim",
            "raw", "list"
        };

        private readonly TagService _tagService;
        public TagCommand(IMediator mediator, TagService tagService)
        {
            _mediator = mediator;
            _tagService = tagService;
        }

        [GroupCommand]
        [Description("Shows the Content of a Tag")]
        public async Task Tag(CommandContext ctx, [RemainingText] string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                await ctx.RespondAsync("You must specify a tag!");
                return;
            }
            
            Tag? dbTag = await _tagService.GetTagAsync(tag, ctx.Guild.Id);

            if (dbTag is null)
            {
                await ctx.RespondAsync("Tag not found!");
            }
            else
            {
                await ctx.RespondAsync(dbTag.Content);
                await _mediator.Send(new UpdateTagRequest(tag, ctx.Guild.Id) {Uses = dbTag.Uses + 1});
            }
        }

        [Command]
        [Description("Get some Info about a Tag")]
        public async Task Info(CommandContext ctx, [RemainingText] string tag)
        {
            Tag? dbTag = await _tagService.GetTagAsync(tag, ctx.Guild.Id);

            if (dbTag is null)
            {
                await ctx.RespondAsync("Tag not found! :(");
                return;
            }

            DiscordUser tagOwner = await ctx.Client.GetUserAsync(dbTag.OwnerId);
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Blurple)
                .WithAuthor(tagOwner.Username, iconUrl: tagOwner.AvatarUrl)
                .WithTitle(dbTag.Name)
                .AddField("Uses:", dbTag.Uses.ToString())
                .AddField("Created At:", dbTag.CreatedAt.ToUniversalTime().ToString("MM/dd/yyyy - h:mm UTC"));

            if (dbTag.OriginalTag is not null)
                builder.AddField("Original:", dbTag.OriginalTag.Name);

            await ctx.RespondAsync(builder);

        }

        [Command]
        [Description("Create an Alias for a Tag")]
        public async Task Alias(CommandContext ctx, string aliasName, [RemainingText] string originalTag)
        {
            TagCreationResult? couldCreateAlias = await _tagService.AliasTagAsync(originalTag, aliasName, ctx.Guild.Id, ctx.User.Id);
            if (!couldCreateAlias.Success)
                await ctx.RespondAsync(couldCreateAlias.Reason);
            else
                await ctx.RespondAsync($"Alias `{aliasName}` that points to tag `{originalTag}` successfully created.");
        }

        [Command]
        [Description("Create a Tag")]
        public async Task Create(CommandContext ctx, string tagName, [RemainingText] string? content)
        {
            Tag? tag = await _tagService.GetTagAsync(tagName, ctx.Guild.Id);
            if (tag is not null)
            {
                await ctx.RespondAsync("Tag with that name already exists!");
                return;
            }
            if (_reservedWords.Any(tagName.StartsWith))
            {
                await ctx.RespondAsync("Tag name begins with reserved keyword!");
                return;
            }
            if (string.IsNullOrEmpty(content))
            {
                await ctx.RespondAsync("Missing tag content!");
                return;
            }

            TagCreationResult couldCreateTag = await _tagService.CreateTagAsync(tagName, content, ctx.Guild.Id, ctx.User.Id);
            if (!couldCreateTag.Success)
                await ctx.RespondAsync(couldCreateTag.Reason);
            else
                await ctx.RespondAsync($"Successfully created tag **{tagName}**.");
        }

        [Command]
        [Description("Delete a Tag")]
        public async Task Delete(CommandContext ctx, [RemainingText] string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
            {
                await ctx.RespondAsync("Must specify a tag to delete!");
                return;
            }

            Tag? tag = await _tagService.GetTagAsync(tagName, ctx.Guild.Id);
            if (tag is null)
            {
                await ctx.RespondAsync("Tag not found!");
                return;
            }
            User? user = await _mediator.Send(new GetUserRequest(ctx.Guild.Id, ctx.User.Id));

            if (tag.OwnerId != ctx.User.Id && (!user?.Flags.Has(UserFlag.Staff) ?? false))
            {
                await ctx.RespondAsync("You either do not own this tag, or are not staff!");
                return;
            }

            await _tagService.RemoveTagAsync(tagName, ctx.Guild.Id);

            string message =
                tag.OriginalTag is not null ?
                    "Alias successfully deleted." :
                    tag.Aliases?.Any() ?? false ?
                        "Tag and all corresponded aliases successfully deleted." :
                        "Tag successfully deleted.";

            await ctx.RespondAsync(message);

        }

        [Command]
        [Description("Edit a Tag's Content")]
        public async Task Edit(CommandContext ctx, string tagName, [RemainingText] string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                await ctx.RespondAsync("Missing tag content!");
                return;
            }

            TagCreationResult couldEditTag = await _tagService.UpdateTagContentAsync(tagName, content, ctx.Guild.Id, ctx.User.Id);
            if (!couldEditTag.Success)
                await ctx.RespondAsync(couldEditTag.Reason);
            else
                await ctx.RespondAsync("Successfully edited tag!");
        }

        [Command]
        [Description("Search for a Tag by name")]
        public async Task Search(CommandContext ctx, string tagName)
        {
            IEnumerable<Tag> tags = await _mediator.Send(new GetTagByNameRequest(tagName, ctx.Guild.Id));
            if (!tags.Any())
            {
                await ctx.RespondAsync("No tags found :c");
                return;
            }

            string allTags = string.Join("\n\n", tags!
                .Select(t =>
                {
                    var s = $"`{t.Name}`";
                    if (t.OriginalTagId is not null) s += $" → `{t.OriginalTag!.Name}`";
                    s += $" - <@{t.OwnerId}>";
                    return s;
                }));
            DiscordEmbedBuilder? builder = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Blurple)
                .WithTitle($"Result for {tagName}:")
                .WithFooter($"Silk! | Requested by {ctx.User.Id}");

            if (tags.Count() < 10)
            {
                builder.WithDescription(allTags);
                await ctx.RespondAsync(builder);
            }
            else
            {
                InteractivityExtension? interactivity = ctx.Client.GetInteractivity();

                IEnumerable<Page>? pages = interactivity.GeneratePagesInEmbed(allTags, SplitType.Line, builder);
                await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages, timeoutoverride: null);
            }

        }

        [Command]
        [Description("Shows the Raw Content of a Tag")]
        public async Task Raw(CommandContext ctx, string tag)
        {
            Tag? dbTag = await _tagService.GetTagAsync(tag, ctx.Guild.Id);

            if (dbTag is null)
            {
                await ctx.RespondAsync("Tag not found!");
            }
            else
            {
                await ctx.RespondAsync(Formatter.Sanitize(dbTag.Content));
                await _mediator.Send(new UpdateTagRequest(tag, ctx.Guild.Id) {Uses = dbTag.Uses + 1});
            }
        }

        [Command]
        [Description("Shows a List of All Tags in this Server")]
        public async Task List(CommandContext ctx)
        {
            IEnumerable<Tag> tags = await _tagService.GetGuildTagsAsync(ctx.Guild.Id);
            if (!tags.Any())
            {
                await ctx.RespondAsync("No tags in this server! :c");
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
                await ctx.RespondAsync(builder);
            }
            else
            {
                InteractivityExtension? interactivity = ctx.Client.GetInteractivity();

                IEnumerable<Page>? pages = interactivity.GeneratePagesInEmbed(allTags, SplitType.Line, builder);
                await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages, timeoutoverride: null);
            }
        }
    }

    [RequireGuild]
    [Category(Categories.Server)]
    public class TagsCommand : BaseCommandModule
    {
        private readonly TagService _tagService;
        public TagsCommand(TagService tagService)
        {
            _tagService = tagService;
        }

        [Command]
        [Description("Get Tags created by a User")]
        public async Task Tags(CommandContext ctx, DiscordMember user)
        {
            IEnumerable<Tag> tags = await _tagService.GetUserTagsAsync(user.Id, ctx.Guild.Id);
            if (!tags.Any())
            {
                await ctx.RespondAsync("User has no tags! :c");
                return;
            }

            string allTags = string.Join('\n', tags
                .Select(t =>
                {
                    var s = $"`{t.Name}`";
                    if (t.OriginalTagId is not null) s += $" → `{t.OriginalTag!.Name}`";
                    return s;
                }));

            DiscordEmbedBuilder builder = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Blurple)
                .WithTitle($"Tags for {user.Username}:")
                .WithFooter($"Silk! | Requested by {ctx.User.Id}");

            if (tags.Count() < 10)
            {
                builder.WithDescription(allTags);
                await ctx.RespondAsync(builder);
            }
            else
            {
                InteractivityExtension? interactivity = ctx.Client.GetInteractivity();

                IEnumerable<Page>? pages = interactivity.GeneratePagesInEmbed(allTags, SplitType.Line, builder);
                await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages, timeoutoverride: null);
            }
        }

        [Command]
        [Description("Get Tags created by the Current User")]
        public async Task Tags(CommandContext ctx)
            => await Tags(ctx, ctx.Member);
    }
}