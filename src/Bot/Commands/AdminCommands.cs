using Discord.Interactions;
using System.Threading.Tasks;

public class AdminCommands : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("debug", "Debug command")]
    public async Task Debug()
    {
        await RespondAsync("Debug mode", ephemeral: true);
    }
}
