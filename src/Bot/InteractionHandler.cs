using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Reflection;
using System.Threading.Tasks;

public class InteractionHandler
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactions;
    private readonly IServiceProvider _services;

    public InteractionHandler(DiscordSocketClient client, InteractionService interactions, IServiceProvider services)
    {
        _client = client;
        _interactions = interactions;
        _services = services;
    }

    public async Task InitializeAsync()
    {
        _client.InteractionCreated += HandleInteractionAsync;
        _interactions.SlashCommandExecuted += SlashCommandExecutedAsync;

        await _interactions.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
    }

    private Task SlashCommandExecutedAsync(SlashCommandInfo commandInfo, IInteractionContext context, IResult result)
    {
        if (!result.IsSuccess)
        {
            Logger.WriteLog($"Slash command '{commandInfo.Name}' failed: {result.Error}");
        }
        return Task.CompletedTask;
    }

    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        try
        {
            var ctx = new SocketInteractionContext(_client, interaction);
            await _interactions.ExecuteCommandAsync(ctx, _services);
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);

            if (interaction.Type == InteractionType.ApplicationCommand)
            {
                if (interaction.HasResponded)
                {
                    await interaction.FollowupAsync("An error occurred while executing the command.");
                }
                else
                {
                    await interaction.RespondAsync("An error occurred while executing the command.", ephemeral: true);
                }
            }
        }
    }

    private async Task SlashCommandExecutedAsync(SlashCommandInfo command, SocketInteractionContext context, IResult result)
    {
        if (!result.IsSuccess)
        {
            if (result.Error == InteractionCommandError.UnknownCommand)
                return;

            Logger.WriteLog($"[InteractionHandler] Error: {result.ErrorReason}");
            await context.Interaction.RespondAsync($"Error: {result.ErrorReason}", ephemeral: true);
        }
    }
}
