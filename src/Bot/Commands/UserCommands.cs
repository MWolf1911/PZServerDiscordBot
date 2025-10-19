using Discord;
using Discord.Interactions;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class UserCommands : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("bot_info", "Displays information about this bot.")]
    public async Task BotInfo()
    {
        await RespondAsync(Localization.Get("disc_cmd_bot_info_text").KeyFormat(("repo_url", Application.BotRepoURL)), ephemeral: true);
    }

    [SlashCommand("server_status", "Gets the server status.")]
    public async Task ServerStatus()
    {
        string status = ServerUtility.IsServerRunning() 
                       ? Localization.Get("disc_cmd_server_status_running")
                       : ServerBackupCreator.IsRunning
                       ? Localization.Get("disc_cmd_server_status_backup")
                       : Localization.Get("disc_cmd_server_status_dead");
        
        await RespondAsync(status, ephemeral: true);
    }

    [SlashCommand("restart_time", "Gets the next automated restart time.")]
    public async Task RebootTime()
    {
        var timestamp = new DateTimeOffset(Scheduler.GetItem("ServerRestart").NextExecuteTime).ToUnixTimeSeconds();
        await RespondAsync(Localization.Get("disc_cmd_restart_time_text").KeyFormat(("timestamp", timestamp)), ephemeral: true);
    }

    [SlashCommand("game_date", "Gets the current in-game date.")]
    public async Task GameDate()
    {
        string mapTimeFile = ServerPath.MapTimeFilePath();

        if(!File.Exists(mapTimeFile))
        {
            Logger.WriteLog($"[UserCommand - game_date] Couldn't find path: {mapTimeFile}");
            await RespondAsync(Localization.Get("disc_cmd_game_date_warn_file"), ephemeral: true);
            return;
        }

        byte[] fileBytes  = File.ReadAllBytes(mapTimeFile);
        byte[] dayBytes   = fileBytes.Skip(0x1C).Take(4).ToArray(); 
        byte[] monthBytes = fileBytes.Skip(0x20).Take(4).ToArray();
        byte[] yearBytes  = fileBytes.Skip(0x24).Take(4).ToArray();

        int day   = (dayBytes[0] << 24) | (dayBytes[1] << 16) | (dayBytes[2] << 8) | dayBytes[3];
        int month = (monthBytes[0] << 24) | (monthBytes[1] << 16) | (monthBytes[2] << 8) | monthBytes[3];
        int year  = (yearBytes[0] << 24) | (yearBytes[1] << 16) | (yearBytes[2] << 8) | yearBytes[3];

        await RespondAsync($"{day}/{month}/{year}", ephemeral: true);
    }

    [SlashCommand("player_perks", "Gets a player's perks from the last log.")]
    public async Task GetPlayerPerks([Summary("player_name", "The player's name")] string playerName)
    {
        var perkData = ServerLogParsers.PerkLog.GetPlayerPerks(playerName);

        if(perkData == null)
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
