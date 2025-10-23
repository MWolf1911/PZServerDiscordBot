using Discord.Interactions;
using Discord;
using System;
using System.Threading.Tasks;

[Group("server", "Server control commands")]
public class ServerControlCommands : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("start", "Starts the server")]
    public async Task StartServer()
    {
        if(ServerUtility.IsServerRunning())
        {
            await RespondAsync(Localization.Get("disc_cmd_start_server_warn_running"), ephemeral: true);
        }
        else if(ServerBackupCreator.IsRunning)
        {
            await RespondAsync(Localization.Get("disc_cmd_start_server_warn_backup"), ephemeral: true);
        }
        else
        {
            ServerUtility.ServerProcess = ServerUtility.Commands.StartServer();
            
            Logger.WriteLog($"[ServerControl - start] Caller: {Context.User}");
            await RespondAsync(Localization.Get("disc_cmd_start_server_ok"), ephemeral: true);
        }
    }

    [SlashCommand("stop", "Stops the server")]
    public async Task StopServer()
    {
        if(!ServerUtility.IsServerRunning())
        {
            await RespondAsync(Localization.Get("disc_cmd_stop_server_warn"), ephemeral: true);
            return;
        }

        Logger.WriteLog($"[ServerControl - stop] Caller: {Context.User}");
        ServerUtility.Commands.StopServer();
        
        await RespondAsync(Localization.Get("disc_cmd_stop_server_ok"), ephemeral: true);
    }

    [SlashCommand("save", "Saves the server")]
    public async Task SaveServer()
    {
        if(!ServerUtility.IsServerRunning())
        {
            await RespondAsync(Localization.Get("disc_cmd_save_server_warn"), ephemeral: true);
            return;
        }

        Logger.WriteLog($"[ServerControl - save] Caller: {Context.User}");
        ServerUtility.Commands.SaveServer();
        
        await RespondAsync(Localization.Get("disc_cmd_save_server_ok"), ephemeral: true);
    }

    [SlashCommand("cmd", "Sends a command to the server console")]
    public async Task ServerCommand([Summary("command", "The command to execute")] string command)
    {
        try
        {
            ServerUtility.ServerProcess.StandardInput.WriteLine(command);
            ServerUtility.ServerProcess.StandardInput.Flush();
            Logger.WriteLog($"[ServerControl - cmd] Caller: {Context.User}, Command: {command}");
            await RespondAsync("Command executed", ephemeral: true);
        }
        catch(Exception ex)
        {
            await RespondAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }

    [SlashCommand("msg", "Broadcasts a message to all players")]
    public async Task ServerMessage([Summary("message", "The message to broadcast")] string message)
    {
        ServerUtility.Commands.ServerMsg(message);
        Logger.WriteLog($"[ServerControl - msg] Caller: {Context.User}, Message: {message}");
        await RespondAsync("Message sent", ephemeral: true);
    }

    [SlashCommand("status", "Gets the server status")]
    public async Task ServerStatus()
    {
        await RespondAsync(ServerUtility.IsServerRunning() 
            ? Localization.Get("disc_cmd_server_status_ok") 
            : Localization.Get("disc_cmd_server_status_fail"), ephemeral: true);
    }
}

[Group("player", "Player management commands")]
public class PlayerCommands : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("kick", "Kicks a player from the server")]
    public async Task KickPlayer([Summary("player_name", "The player's name")] string playerName)
    {
        ServerUtility.Commands.KickUser(playerName);
        Logger.WriteLog($"[Player - kick] Caller: {Context.User}, Target: {playerName}");
        await RespondAsync($"Player {playerName} kicked", ephemeral: true);
    }

    [SlashCommand("ban", "Bans a player from the server")]
    public async Task BanPlayer([Summary("player_name", "The player's name")] string playerName)
    {
        ServerUtility.Commands.BanUser(playerName);
        Logger.WriteLog($"[Player - ban] Caller: {Context.User}, Target: {playerName}");
        await RespondAsync($"Player {playerName} banned", ephemeral: true);
    }

    [SlashCommand("teleport", "Teleports a player to another player")]
    public async Task TeleportPlayer(
        [Summary("player1_name", "The player to teleport")] string player1Name, 
        [Summary("player2_name", "The target player")] string player2Name)
    {
        ServerUtility.Commands.Teleport(player1Name, player2Name);
        Logger.WriteLog($"[Player - teleport] Caller: {Context.User}, {player1Name} -> {player2Name}");
        await RespondAsync($"Player {player1Name} teleported to {player2Name}", ephemeral: true);
    }

    [SlashCommand("perks", "Gets a player's perks from the last log")]
    public async Task PlayerPerks([Summary("player_name", "The player's name")] string playerName)
    {
        var allPerks = ServerLogParsers.PerkLog.Get();
        
        if(allPerks == null || !allPerks.TryGetValue(playerName, out var perkData))
        {
            await RespondAsync(Localization.Get("disc_cmd_player_perks_not_fnd").KeyFormat(("name", playerName)), ephemeral: true);
            return;
        }

        var embed = new EmbedBuilder()
        {
            Title = $"{perkData.Username}",
            Description = $"Steam ID: {perkData.SteamId}\nLog Date: {perkData.LogDate}",
            Color = Color.Blue
        };

        foreach(var perk in perkData.Perks)
        {
            embed.AddField(perk.Key, perk.Value.ToString());
        }

        await RespondAsync(embed: embed.Build(), ephemeral: true);
    }
}

[Group("admin", "Admin management commands")]
public class AdminManagementCommands : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("grant", "Makes a player an admin")]
    public async Task MakeAdmin([Summary("player_name", "The player's name")] string playerName)
    {
        ServerUtility.Commands.GrantAdmin(playerName);
        Logger.WriteLog($"[Admin - grant] Caller: {Context.User}, Target: {playerName}");
        await RespondAsync($"Player {playerName} is now an admin", ephemeral: true);
    }

    [SlashCommand("remove", "Removes admin status from a player")]
    public async Task RemoveAdmin([Summary("player_name", "The player's name")] string playerName)
    {
        ServerUtility.Commands.RemoveAdmin(playerName);
        Logger.WriteLog($"[Admin - remove] Caller: {Context.User}, Target: {playerName}");
        await RespondAsync($"Player {playerName} is no longer an admin", ephemeral: true);
    }
}

[Group("weather", "Weather control commands")]
public class WeatherCommands : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("start_rain", "Starts rain on the server")]
    public async Task StartRain()
    {
        ServerUtility.Commands.StartRain();
        Logger.WriteLog($"[Weather - start_rain] Caller: {Context.User}");
        await RespondAsync("Rain started", ephemeral: true);
    }

    [SlashCommand("stop_rain", "Stops rain on the server")]
    public async Task StopRain()
    {
        ServerUtility.Commands.StopRain();
        Logger.WriteLog($"[Weather - stop_rain] Caller: {Context.User}");
        await RespondAsync("Rain stopped", ephemeral: true);
    }
}
