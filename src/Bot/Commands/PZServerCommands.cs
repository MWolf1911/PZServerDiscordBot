using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class PZServerCommands : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("server_cmd", "Allows you to send inputs to the server console.")]
    public async Task ServerCommand([Summary("command", "The command to execute")] string command)
    {
        try
        {
            ServerUtility.ServerProcess.StandardInput.WriteLine(command);
            ServerUtility.ServerProcess.StandardInput.Flush();
        }
        catch(Exception ex)
        {
            await RespondAsync($"Error: {ex.Message}", ephemeral: true);
            return;
        }
        
        await RespondAsync("Command executed", ephemeral: true);
    }

    [SlashCommand("server_msg", "Broadcasts a message to all players in the server.")]
    public async Task ServerMessage([Summary("message", "The message to broadcast")] string message)
    {
        ServerUtility.Commands.ServerMsg(message);
        Logger.WriteLog($"[PZServerCommand - server_msg] Caller: {Context.User}, Params: {message}");

        await RespondAsync("Message sent", ephemeral: true);
    }

    [SlashCommand("start_server", "Starts the server.")]
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
            
            Logger.WriteLog($"[PZServerCommand - start_server] Caller: {Context.User}");
            await RespondAsync(Localization.Get("disc_cmd_start_server_ok"), ephemeral: true);
        }
    }

    [SlashCommand("stop_server", "Stops the server.")]
    public async Task StopServer()
    {
        if(!ServerUtility.IsServerRunning())
        {
            await RespondAsync(Localization.Get("disc_cmd_stop_server_warn"), ephemeral: true);
            return;
        }

        Logger.WriteLog($"[PZServerCommand - stop_server] Caller: {Context.User}");

        ServerUtility.Commands.StopServer();
        
        await RespondAsync(Localization.Get("disc_cmd_stop_server_ok"), ephemeral: true);
    }

    [SlashCommand("save_server", "Saves the server.")]
    public async Task SaveServer()
    {
        if(!ServerUtility.IsServerRunning())
        {
            await RespondAsync(Localization.Get("disc_cmd_save_server_warn"), ephemeral: true);
            return;
        }

        Logger.WriteLog($"[PZServerCommand - save_server] Caller: {Context.User}");

        ServerUtility.Commands.SaveServer();
        
        await RespondAsync(Localization.Get("disc_cmd_save_server_ok"), ephemeral: true);
    }

    [SlashCommand("kick_player", "Kicks a player from the server.")]
    public async Task KickPlayer([Summary("player_name", "The player's name")] string playerName)
    {
        ServerUtility.Commands.KickUser(playerName);
        Logger.WriteLog($"[PZServerCommand - kick_player] Caller: {Context.User}, Params: {playerName}");

        await RespondAsync($"Player {playerName} kicked", ephemeral: true);
    }

    [SlashCommand("ban_player", "Bans a player from the server.")]
    public async Task BanPlayer([Summary("player_name", "The player's name")] string playerName)
    {
        ServerUtility.Commands.BanUser(playerName);
        Logger.WriteLog($"[PZServerCommand - ban_player] Caller: {Context.User}, Params: {playerName}");

        await RespondAsync($"Player {playerName} banned", ephemeral: true);
    }

    [SlashCommand("make_admin", "Makes a player an admin.")]
    public async Task MakeAdmin([Summary("player_name", "The player's name")] string playerName)
    {
        ServerUtility.Commands.GrantAdmin(playerName);
        Logger.WriteLog($"[PZServerCommand - make_admin] Caller: {Context.User}, Params: {playerName}");

        await RespondAsync($"Player {playerName} is now an admin", ephemeral: true);
    }

    [SlashCommand("remove_admin", "Removes admin status from a player.")]
    public async Task RemoveAdmin([Summary("player_name", "The player's name")] string playerName)
    {
        ServerUtility.Commands.RemoveAdmin(playerName);
        Logger.WriteLog($"[PZServerCommand - remove_admin] Caller: {Context.User}, Params: {playerName}");

        await RespondAsync($"Player {playerName} is no longer an admin", ephemeral: true);
    }

    [SlashCommand("teleport_player", "Teleports a player to coordinates.")]
    public async Task TeleportPlayer([Summary("player1_name", "The player to teleport")] string player1Name, 
                                      [Summary("player2_name", "The target player to teleport to")] string player2Name)
    {
        ServerUtility.Commands.Teleport(player1Name, player2Name);
        Logger.WriteLog($"[PZServerCommand - teleport_player] Caller: {Context.User}, Params: {player1Name} teleported to {player2Name}");

        await RespondAsync($"Player {player1Name} teleported to {player2Name}", ephemeral: true);
    }

    [SlashCommand("start_rain", "Starts rain on the server.")]
    public async Task StartRain()
    {
        ServerUtility.Commands.StartRain();
        Logger.WriteLog($"[PZServerCommand - start_rain] Caller: {Context.User}");

        await RespondAsync("Rain started", ephemeral: true);
    }

    [SlashCommand("stop_rain", "Stops rain on the server.")]
    public async Task StopRain()
    {
        ServerUtility.Commands.StopRain();
        Logger.WriteLog($"[PZServerCommand - stop_rain] Caller: {Context.User}");

        await RespondAsync("Rain stopped", ephemeral: true);
    }
}
