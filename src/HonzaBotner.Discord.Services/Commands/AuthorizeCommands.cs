﻿using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using HonzaBotner.Services.Contract;
using HonzaBotner.Services.Contract.Dto;

namespace HonzaBotner.Discord.Services.Commands
{
    [Description("Příkazy sloužící k ověření totožnosti")]
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class AuthorizeCommands : BaseCommandModule
    {
        private readonly IUrlProvider _urlProvider;
        private readonly IAuthorizationService _authorizationService;

        public AuthorizeCommands(IUrlProvider urlProvider, IAuthorizationService authorizationService)
        {
            _urlProvider = urlProvider;
            _authorizationService = authorizationService;
        }

        [Command("authorize"), Aliases("auth")]
        public async Task AuthorizeCommand(CommandContext ctx)
        {
            DiscordUser user = ctx.User;
            DiscordDmChannel channel;

            if (ctx.Channel.Type is ChannelType.Group or ChannelType.Private)
            {
                channel = (DiscordDmChannel) ctx.Channel;
            }
            else
            {
                channel = await ctx.Guild.Members[user.Id].CreateDmChannelAsync();
            }

            string link = _urlProvider.GetAuthLink(user.Id, RolesPool.Auth);

            if (await _authorizationService.IsUserVerified(user.Id))
            {
                await channel.SendMessageAsync(
                    $"You are already authorized.\nTo update roles follow this link: {link}");
            }
            else
            {
                await channel.SendMessageAsync($"Hi, authorize by following this link: {link}");
            }
        }
    }
}
