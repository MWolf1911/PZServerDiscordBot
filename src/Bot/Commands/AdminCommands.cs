using Discord.Interactions;
using System.Threading.Tasks;

public class AdminCommands : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("debug", "Debug command")]
    public async Task Debug()
    {
        await RespondAsync("Debug mode", ephemeral: true);
    }

    [SlashCommand("clear_global_commands", "Clear all globally registered commands (removes duplicates)")]
    public async Task ClearGlobalCommands()
    {
        try
        {
            await Application.Client.BulkOverwriteGlobalApplicationCommandsAsync(new Discord.ApplicationCommandProperties[] { });
            Logger.WriteLog("[ADMIN] Cleared all global commands");
            await RespondAsync("Global commands cleared. Duplicates should disappear within a few minutes.", ephemeral: true);
        }
        catch (System.Exception ex)
        {
            Logger.WriteLog($"[ADMIN] Error clearing global commands: {ex.Message}");
            Logger.LogException(ex);
            await RespondAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }
}
