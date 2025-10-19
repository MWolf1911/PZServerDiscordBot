using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

public class BotCommands : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("set_command_channel", "Sets the channel for bot to work in.")]
    public async Task SetCommandChannel([Summary("channel", "The channel to set")] IChannel channel)
    {
        if(Application.BotSettings.LogChannelId == channel.Id)
        {
            await RespondAsync(Localization.Get("disc_cmd_set_command_channel_warn").KeyFormat(("channel_id", channel.Id)), ephemeral: true);
            return;
        }

        Application.BotSettings.CommandChannelId = channel.Id;
        Application.BotSettings.Save();

        Logger.WriteLog($"[BotCommands - set_command_channel] Caller: {Context.User}, Params: <#{channel.Id}>");
        await RespondAsync(Localization.Get("bot_disc_chan_set_ok").KeyFormat(("channel_id", channel.Id)), ephemeral: true);
    }

    [SlashCommand("set_log_channel", "Sets the channel for bot logging.")]
    public async Task SetLogChannel([Summary("channel", "The channel to set")] IChannel channel)
    {
        if(Application.BotSettings.CommandChannelId == channel.Id)
        {
            await RespondAsync(Localization.Get("disc_cmd_set_log_channel_warn").KeyFormat(("channel_id", channel.Id)), ephemeral: true);
            return;
        }

        Application.BotSettings.LogChannelId = channel.Id;
        Application.BotSettings.Save();

        Logger.WriteLog($"[BotCommands - set_log_channel] Caller: {Context.User}, Params: <#{channel.Id}>");
        await RespondAsync(Localization.Get("bot_disc_chan_set_ok").KeyFormat(("channel_id", channel.Id)), ephemeral: true);
    }

    [SlashCommand("set_public_channel", "Sets the public channel.")]
    public async Task SetPublicChannel([Summary("channel", "The channel to set")] IChannel channel)
    {
        Application.BotSettings.PublicChannelId = channel.Id;
        Application.BotSettings.Save();

        Logger.WriteLog($"[BotCommands - set_public_channel] Caller: {Context.User}, Params: <#{channel.Id}>");
        await RespondAsync(Localization.Get("bot_disc_chan_set_ok").KeyFormat(("channel_id", channel.Id)), ephemeral: true);
    }

    [SlashCommand("get_settings", "Gets the bot settings.")]
    public async Task GetSettings()
    {
        Logger.WriteLog($"[BotCommands - get_settings] Caller: {Context.User}");
        
        string botSettings = "";
        botSettings += Localization.Get("disc_cmd_get_settings_serv_id").KeyFormat(("server_id", Application.BotSettings.GuildId)) + "\n";
        botSettings += Localization.Get("disc_cmd_get_settings_cmd_chan_id").KeyFormat(("channel_id", Application.BotSettings.CommandChannelId)) + "\n";
        botSettings += Localization.Get("disc_cmd_get_settings_log_chan_id").KeyFormat(("channel_id", Application.BotSettings.LogChannelId)) + "\n";
        botSettings += Localization.Get("disc_cmd_get_settings_pub_chan_id").KeyFormat(("channel_id", Application.BotSettings.PublicChannelId)) + "\n";
        botSettings += Localization.Get("disc_cmd_get_settings_perk_cac_dur").KeyFormat(("minutes", Application.BotSettings.ServerLogParserSettings.PerkParserCacheDuration)) + "\n";
        botSettings += Localization.Get("disc_cmd_get_settings_res_serv_sch_type").KeyFormat(("type", Application.BotSettings.ServerScheduleSettings.ServerRestartScheduleType)) + "\n";
        botSettings += Localization.Get("disc_cmd_get_settings_serv_res_times").KeyFormat(("timeList", String.Join(", ", Application.BotSettings.ServerScheduleSettings.ServerRestartTimes))) + "\n";
        botSettings += Localization.Get("disc_cmd_get_settings_res_sch_int").KeyFormat(("minutes", Application.BotSettings.ServerScheduleSettings.ServerRestartSchedule / (60 * 1000))) + "\n";
        botSettings += Localization.Get("disc_cmd_get_settings_mod_sch_int").KeyFormat(("minutes", Application.BotSettings.ServerScheduleSettings.WorkshopItemUpdateSchedule / (60 * 1000))) + "\n";
        botSettings += Localization.Get("disc_cmd_get_settings_mod_rst_timer").KeyFormat(("minutes", Application.BotSettings.ServerScheduleSettings.WorkshopItemUpdateRestartTimer / (60 * 1000))) + "\n";
        botSettings += Localization.Get("disc_cmd_get_settings_serv_aut_strt").KeyFormat(("state", Application.BotSettings.BotFeatureSettings.AutoServerStart ? Localization.Get("gen_enab_up") : Localization.Get("gen_disa_up"))) + "\n";
        botSettings += Localization.Get("disc_cmd_get_settings_mod_logging").KeyFormat(("state", Application.BotSettings.BotFeatureSettings.NonPublicModLogging ? Localization.Get("gen_enab_up") : Localization.Get("gen_disa_up")));

        await RespondAsync(botSettings, ephemeral: true);
    }

    [SlashCommand("get_schedules", "Gets the remaining times until schedules to be executed.")]
    public async Task GetSchedules()
    {
        Logger.WriteLog($"[BotCommands - get_schedules] Caller: {Context.User}");
        
        IReadOnlyCollection<ScheduleItem> scheduleItems = Scheduler.GetItems();

        if(scheduleItems.Count > 0)
        {
            string schedules = "";
            foreach((int i, ScheduleItem item) in scheduleItems.Select((val, i) => (i, val)))
            {
                schedules += Localization.Get("disc_cmd_get_schedules_run").KeyFormat(("name", item.DisplayName), ("timestamp", new DateTimeOffset(item.NextExecuteTime).ToUnixTimeSeconds()));
                if(i != scheduleItems.Count - 1) schedules += "\n";
            }
            await RespondAsync(schedules, ephemeral: true);
        }
        else await RespondAsync(Localization.Get("disc_cmd_get_schedules_not_fnd"), ephemeral: true);
    }

    [SlashCommand("get_ram_cpu", "Gets the total RAM and CPU usage of the machine.")]
    public async Task GetRAMCPU()
    {
        Logger.WriteLog($"[BotCommands - get_ram_cpu] Caller: {Context.User}");
        
        string progressBarStr = "```" + Statistics.GetPercentageValueProgressBar("RAM", Statistics.GetTotalRAMUsagePercentage()) + "\n" + Statistics.GetPercentageValueProgressBar("CPU", Statistics.GetTotalCPUUsagePercentage()) + "```";
        await RespondAsync(progressBarStr, ephemeral: true);
    }

    [SlashCommand("set_restart_schedule_type", "Set the server'\''s restart schedule type.")]
    public async Task SetRestartScheduleType([Summary("type", "interval or time")] string scheduleType)
    {
        if(scheduleType.ToLower() != "interval" && scheduleType.ToLower() != "time")
        {
            await RespondAsync(Localization.Get("disc_cmd_set_restart_schedule_type_warn"), ephemeral: true);
            return;
        }

        Logger.WriteLog($"[BotCommands - set_restart_schedule_type] Caller: {Context.User}, Params: {scheduleType}");
        Application.BotSettings.ServerScheduleSettings.ServerRestartScheduleType = scheduleType.ToLower();
        Application.BotSettings.Save();
        ServerUtility.ResetServerRestartInterval();
        await RespondAsync(Localization.Get("disc_cmd_set_restart_schedule_type_ok").KeyFormat(("type", scheduleType)), ephemeral: true);
    }

    [SlashCommand("set_restart_interval", "Set the server'\''s restart schedule interval in minutes.")]
    public async Task SetRestartInterval([Summary("minutes", "Interval in minutes (minimum 60)")] uint intervalMinute)
    {
        if(intervalMinute < 60)
        {
            await RespondAsync(Localization.Get("disc_cmd_set_restart_interval_int_warn"), ephemeral: true);
            return;
        }

        Logger.WriteLog($"[BotCommands - set_restart_interval] Caller: {Context.User}, Params: {intervalMinute}");
        Scheduler.GetItem("ServerRestart")?.UpdateInterval(intervalMinute * 60 * 1000);
        Application.BotSettings.ServerScheduleSettings.ServerRestartSchedule = intervalMinute * 60 * 1000;
        Application.BotSettings.Save();
        await RespondAsync(Localization.Get("disc_cmd_set_restart_interval_int_ok"), ephemeral: true);
    }

    [SlashCommand("reset_perk_cache", "Reset the perk cache.")]
    public async Task ResetPerkCache()
    {
        Logger.WriteLog($"[BotCommands - reset_perk_cache] Caller: {Context.User}");
        ServerLogParsers.PerkLog.PerkCache = null;
        await RespondAsync(Localization.Get("disc_cmd_reset_perk_cache_ok"), ephemeral: true);
    }

    [SlashCommand("toggle_server_auto_start", "Toggle the server auto start feature.")]
    public async Task ToggleServerAutoStart()
    {
        Logger.WriteLog($"[BotCommands - toggle_server_auto_start] Caller: {Context.User}");
        Application.BotSettings.BotFeatureSettings.AutoServerStart = !Application.BotSettings.BotFeatureSettings.AutoServerStart;
        Application.BotSettings.Save();
        ScheduleItem autoServerStartSchedule = Scheduler.GetItem("AutoServerStart");
        autoServerStartSchedule?.UpdateInterval();
        await RespondAsync(Localization.Get("disc_cmd_toggle_server_auto_start_ok").KeyFormat(("state", (Application.BotSettings.BotFeatureSettings.AutoServerStart ? Localization.Get("gen_enab_up") : Localization.Get("gen_disa_up")).ToLower())), ephemeral: true);
    }

    [SlashCommand("backup_server", "Creates a backup of the server.")]
    public async Task BackupServer()
    {
        if(ServerUtility.IsServerRunning())
        {
            await RespondAsync(Localization.Get("disc_cmd_backup_server_warn"), ephemeral: true);
            return;
        }

        Logger.WriteLog($"[BotCommands - backup_server] Caller: {Context.User}");
        _ = Task.Run(async () => await ServerBackupCreator.Start());
        await RespondAsync(Localization.Get("disc_cmd_backup_server_ok").KeyFormat(("channel_id", Application.BotSettings.LogChannelId)), ephemeral: true);
    }

    [SlashCommand("localization", "Get or change the current localization.")]
    public async Task Localization_([Summary("language", "Optional language name to switch to")] string localizationName = null)
    {
        if(!string.IsNullOrEmpty(localizationName))
        {
            (bool, string) result = await Localization.Download(localizationName);
            await RespondAsync(result.Item2, ephemeral: true);
            return;
        }

        Localization.LocalizationInfo localizationInfo = Localization.GetCurrentLocalizationInfo();

        var embed = new EmbedBuilder()
        {
            Title = Localization.Get("disc_cmd_localization_embed_title"),
            Color = Color.Green
        };

        embed.AddField(Localization.Get("disc_cmd_localization_embed_language"), localizationInfo.Name);
        embed.AddField(Localization.Get("disc_cmd_localization_embed_version"), localizationInfo.Version);
        embed.AddField(Localization.Get("disc_cmd_localization_embed_desc"), localizationInfo.Description);

        await RespondAsync(embed: embed.Build(), ephemeral: true);

        List<Localization.LocalizationInfo> localizationList = await Localization.GetAvailableLocalizationList();
            
        if(localizationList != null)
        {
            List<KeyValuePair<string, string>> availableLocalizations = localizationList.Select(x => new KeyValuePair<string, string>(x.Name, $"{x.Description} ({x.Version})")).ToList();
            await FollowupAsync(Localization.Get("disc_cmd_localization_avaib_list"), ephemeral: true);
            await DiscordUtility.SendEmbeddedMessage(Context.Channel, availableLocalizations);
            await FollowupAsync(Localization.Get("disc_cmd_localization_usage"), ephemeral: true);
        }
        else await FollowupAsync(Localization.Get("disc_cmd_localization_no_localization"), ephemeral: true);

        var lastCacheTime = Localization.LastCacheTime;
        if(lastCacheTime != null)
        {
            await FollowupAsync(Localization.Get("gen_last_cache_text").KeyFormat(("relative_time", BotUtility.GetPastRelativeTimeStr(DateTime.Now, (DateTime)lastCacheTime))), ephemeral: true);
        }
    }
}
