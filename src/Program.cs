#define EXPORT_DEFAULT_LOCALIZATION

using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

public static class Application
{
    public const string                    BotRepoURL = "https://github.com/egebilecen/PZServerDiscordBot";
    public static readonly SemanticVersion BotVersion = new SemanticVersion(1, 11, 5, DevelopmentStage.Release);
    public static Settings.BotSettings     BotSettings;

    public static DiscordSocketClient  Client;
    public static InteractionService   Interactions;
    public static IServiceProvider     Services;
    public static InteractionHandler   InteractionHandler;
    public static DateTime             StartTime = DateTime.UtcNow;

    private static bool botInitialCheck = false;

    static Application()
    {
        try
        {
            File.AppendAllText("startup.log", "Application static ctor\n");
        }
        catch
        {
        }
    }

    private static void Main(string[] _)
    {
        try
        {
            File.AppendAllText("startup.log", "Main start\n");
        }
        catch
        {
        }

        MainAsync().GetAwaiter().GetResult();
    }

    private static async Task MainAsync()
    {
        File.AppendAllText("startup.log", "MainAsync start\n");
        return;
        if(!File.Exists(Settings.BotSettings.SettingsFile))
        {
            File.AppendAllText("startup.log", "Settings file missing\n");
            BotSettings = new Settings.BotSettings();
            BotSettings.Save();
        }
        else
        {
            File.AppendAllText("startup.log", "Loading settings\n");
            BotSettings = JsonConvert.DeserializeObject<Settings.BotSettings>(File.ReadAllText(Settings.BotSettings.SettingsFile), 
                new JsonSerializerSettings{ObjectCreationHandling = ObjectCreationHandling.Replace});
        }

        File.AppendAllText("startup.log", "Localization.Load\n");
        Localization.Load();
    #if EXPORT_DEFAULT_LOCALIZATION
        Localization.ExportDefault();
    #endif

    #if DEBUG
        Console.WriteLine(Localization.Get("warn_debug_mode"));
    #endif

        try
        {
                File.AppendAllText("startup.log", "Checking token\n");
            if(string.IsNullOrEmpty(DiscordUtility.GetToken()))
            {
                Console.WriteLine(Localization.Get("err_bot_token").KeyFormat(("repo_url", BotRepoURL)));
                await Task.Delay(-1);
            }
        }
        catch(Exception ex)
        {
            Logger.LogException(ex);
            Console.WriteLine(Localization.Get("err_retv_bot_token").KeyFormat(("log_file", Logger.LogFile), ("repo_url", BotRepoURL)));
            await Task.Delay(-1);
        }

    #if !DEBUG
        ServerPath.CheckCustomBasePath();
    #endif

        File.AppendAllText("startup.log", "Ensuring localization directory\n");
        if(!Directory.Exists(Localization.LocalizationPath))
            Directory.CreateDirectory(Localization.LocalizationPath);

        File.AppendAllText("startup.log", "Adding schedules\n");
        Scheduler.AddItem(new ScheduleItem("ServerRestart",
                                           Localization.Get("sch_name_serverrestart"),
                                           BotSettings.ServerScheduleSettings.GetServerRestartSchedule(),
                                           Schedules.ServerRestart,
                                           null));
        Scheduler.AddItem(new ScheduleItem("ServerRestartAnnouncer",
                                           Localization.Get("sch_name_serverrestartannouncer"),
                                           Convert.ToUInt64(TimeSpan.FromSeconds(30).TotalMilliseconds),
                                           Schedules.ServerRestartAnnouncer,
                                           null));
        Scheduler.AddItem(new ScheduleItem("WorkshopItemUpdateChecker",
                                           Localization.Get("sch_name_workshopitemupdatechecker"),
                                           BotSettings.ServerScheduleSettings.WorkshopItemUpdateSchedule,
                                           Schedules.WorkshopItemUpdateChecker,
                                           null));
        Scheduler.AddItem(new ScheduleItem("AutoServerStart",
                                           Localization.Get("sch_name_autoserverstarter"),
                                           Convert.ToUInt64(TimeSpan.FromSeconds(30).TotalMilliseconds),
                                           Schedules.AutoServerStart,
                                           null));
        Scheduler.AddItem(new ScheduleItem("BotVersionChecker",
                                           Localization.Get("sch_name_botnewversioncchecker"),
                                           Convert.ToUInt64(TimeSpan.FromMinutes(5).TotalMilliseconds),
                                           Schedules.BotVersionChecker,
                                           null));
        Localization.AddSchedule();
    File.AppendAllText("startup.log", "Starting scheduler\n");
        Scheduler.Start(1000);
        
    #if !DEBUG
    File.AppendAllText("startup.log", "Attempting server start\n");
        ServerUtility.ServerProcess = ServerUtility.Commands.StartServer();
    #endif

    File.AppendAllText("startup.log", "Creating discord client\n");
        Client = new DiscordSocketClient(new DiscordSocketConfig() { GatewayIntents = GatewayIntents.All });
        Interactions = new InteractionService(Client);
        
        Services = new ServiceCollection()
            .AddSingleton(Client)
            .AddSingleton(Interactions)
            .BuildServiceProvider();
        
    File.AppendAllText("startup.log", "Initializing interaction handler\n");
        InteractionHandler = new InteractionHandler(Client, Interactions, Services);
        await InteractionHandler.InitializeAsync();
        
    File.AppendAllText("startup.log", "Logging into Discord\n");
        await Client.LoginAsync(TokenType.Bot, DiscordUtility.GetToken());
        await Client.StartAsync();
        await Client.SetGameAsync(Localization.Get("info_disc_act_bot_ver").KeyFormat(("version", BotVersion)));

        Client.Ready += async () =>
        {
            if(!botInitialCheck)
            {
                botInitialCheck = true;

                try 
                {
                    await Interactions.RegisterCommandsGloballyAsync();
                }
                catch(Exception ex)
                {
                    Logger.LogException(ex);
                }

                await DiscordUtility.DoChannelCheck();
                await BotUtility.NotifyLatestBotVersion();
                await Localization.CheckUpdate();
            }
        };

        Client.Disconnected += async (ex) =>
        {
            Logger.LogException(ex);
            if(ex.InnerException != null)
                Logger.LogException(ex.InnerException);

            if(ex.InnerException?.Message.Contains("Authentication failed") == true)
            {
                Console.WriteLine(Localization.Get("err_disc_auth_fail"));
                await Task.Delay(-1);
            }
        };

        await Task.Delay(-1);
    }
}

