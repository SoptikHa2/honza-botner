using System;
using System.Globalization;
using System.Threading.Tasks;
using Chronic.Core;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using HonzaBotner.Discord.Extensions;
using HonzaBotner.Discord.Services.Options;
using HonzaBotner.Services.Contract;
using HonzaBotner.Services.Contract.Dto;
using Microsoft.Extensions.Options;

namespace HonzaBotner.Discord.Services.Commands
{
    [Group("reminder")]
    [Aliases("remind")]
    [Description("Commands to manager reminders.")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    [Cooldown(2, 60 * 60, CooldownBucketType.User)]
    public class ReminderCommands : BaseCommandModule
    {
        private readonly IRemindersService _service;

        private readonly ReminderOptions _options;

        public ReminderCommands(IRemindersService service, IOptions<ReminderOptions> options)
        {
            _service = service;
            _options = options.Value;
        }

        [GroupCommand]
        [Command("create")]
        [Aliases("me")] // Allows a more "fluent" usage ::remind me <>
        [Description("Create a new reminder.")]
        public async Task Create(
            CommandContext context,
            [Description("Date or time of the reminder")]
            string rawDatetime,
            [Description("Content of the reminder."), RemainingText]
            string? content
        )
        {
            DateTime now = DateTime.Now;
            DateTime? datetime = ParseDateTime(rawDatetime);

            if (content == null)
            {
                await context.RespondErrorAsync(
                    $"Cannot parse content string",
                    "You didn't provide any content for the reminder."
                );
                await context.Message.DeleteAsync();
                return;
            }

            if (datetime == null)
            {
                await context.RespondErrorAsync(
                    $"Cannot parse datetime string `{rawDatetime}`",
                    "Try using an explicit datetime or expressions like `in 30 minutes`, `tomorrow at 8:00`, ..."
                );
                return;
            }

            if (datetime <= now)
            {
                await context.RespondErrorAsync(
                    "Cannot schedule reminders in the past.",
                    "You can only create reminders that are in the future."
                );
                return;
            }

            await context.TriggerTypingAsync();
            DiscordMessage message = await context.RespondAsync("Creating reminder...");

            Reminder reminder = await _service.CreateReminderAsync(
                context.User.Id,
                message.Id,
                message.ChannelId,
                datetime.Value, // This is safe, as the nullability is validated above
                content
            );

            await message.ModifyAsync("", CreateReminderEmbed(context, reminder));
            await message.CreateReactionAsync(DiscordEmoji.FromUnicode(_options.CancelEmojiName));
            await message.CreateReactionAsync(DiscordEmoji.FromUnicode(_options.JoinEmojiName));
            await context.Message.DeleteAsync();
        }

        private static DateTime? ParseDateTime(string datetime)
        {
            // First try to parse the explicit datetime formats
            // Cases with time only are handled by the parser
            string[] formats = { "dd. MM. yyyy HH:mm", "dd.MM.yyyy HH:mm", "dd. MM. yyyy", "dd.MM.yyyy" };

            if (DateTime.TryParseExact(datetime, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal,
                out DateTime parsed))
            {
                return parsed;
            }

            return new Parser().Parse(datetime)?.Start;
        }

        private DiscordEmbed CreateReminderEmbed(CommandContext context, Reminder reminder)
        {
            return new DiscordEmbedBuilder()
                .WithAuthor(context.Member.RatherNicknameThanUsername(), iconUrl: context.Member.AvatarUrl)
                .WithColor(DiscordColor.Yellow)
                .WithTitle("🔔 Reminder")
                .WithDescription(reminder.Content.RemoveDiscordMentions(context.Guild))
                .AddField("I will remind you at", reminder.DateTime.ToString(CultureInfo.InvariantCulture))
                .Build();
        }
    }
}
