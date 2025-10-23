#define EXPORT_DEFAULT_LOCALIZATION

using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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
        // Static constructor - no initialization needed
    }

    private static void Main(string[] _)
    {
        RunSynchronousSetup();
        
        // Create a separate thread with a large stack size to avoid Discord.NET stack overflow on .NET 4.7.2
        Thread discordThread = new Thread(() =>
        {
            try
            {
                StartDiscordSync();
            }
            catch (Exception ex)
            {
                Logger.WriteLog($"[FATAL] Discord thread exception: {ex.GetType().Name}: {ex.Message}");
                Logger.LogException(ex);
                Environment.Exit(1);
            }
        }, 64 * 1024 * 1024) // 64MB stack to avoid Discord.NET initialization stack overflow
        {
            Name = "DiscordClientThread",
            IsBackground = true
        };
        discordThread.Start();
        
        // Keep main thread alive for scheduler and commands
        System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
    }

    private static void RunSynchronousSetup()
    {
        const string SETTINGS_FILE = ".\\pzdiscordbot.conf";
        
        if(!File.Exists(SETTINGS_FILE))
        {
            BotSettings = new Settings.BotSettings();
            BotSettings.Save();
        }
        else
        {
            BotSettings = JsonConvert.DeserializeObject<Settings.BotSettings>(File.ReadAllText(SETTINGS_FILE), 
                new JsonSerializerSettings{ObjectCreationHandling = ObjectCreationHandling.Replace});
        }

        Localization.Load();
    #if EXPORT_DEFAULT_LOCALIZATION
        Localization.ExportDefault();
    #endif

    #if DEBUG
        Console.WriteLine(Localization.Get("warn_debug_mode"));
    #endif

        if(string.IsNullOrEmpty(DiscordUtility.GetToken()))
        {
            Console.WriteLine(Localization.Get("err_bot_token").KeyFormat(("repo_url", BotRepoURL)));
            Environment.Exit(1);
        }

    #if !DEBUG
        ServerPath.CheckCustomBasePath();
    #endif

        if(!Directory.Exists(Localization.LocalizationPath))
            Directory.CreateDirectory(Localization.LocalizationPath);

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
        
        Scheduler.Start(1000);
        
        // Server auto-start disabled - use /start_server command to start
    }

    private static void StartDiscordSync()
    {
        try
        {
            Logger.WriteLog("Discord: Initializing client");
            
            Client = new DiscordSocketClient(new DiscordSocketConfig() { GatewayIntents = GatewayIntents.All });
            Interactions = new InteractionService(Client);
            Services = new ServiceCollection()
                .AddSingleton(Client)
                .AddSingleton(Interactions)
                .BuildServiceProvider();
            InteractionHandler = new InteractionHandler(Client, Interactions, Services);
            
            // Attach event handlers BEFORE starting the client
            Client.Ready += async () =>
            {
                Logger.WriteLog("[EVENT] Bot Ready");
                if(!botInitialCheck)
                {
                    botInitialCheck = true;
                    Logger.WriteLog("[EVENT] Performing initial ready checks");

                    try 
                    {
                        Logger.WriteLog("[EVENT] Initializing interaction handler");
                        await InteractionHandler.InitializeAsync();
                        Logger.WriteLog("[EVENT] Interaction handler initialized");
                    }
                    catch(Exception ex)
                    {
                        Logger.WriteLog($"[EVENT] Interaction handler init error: {ex.GetType().Name}: {ex.Message}");
                        Logger.LogException(ex);
                    }

                    try 
                    {
                        Logger.WriteLog("[EVENT] Registering commands globally");
                        var commands = await Interactions.RegisterCommandsGloballyAsync();
                        Logger.WriteLog($"[EVENT] Registered {commands.Count} global commands");
                        foreach(var cmd in commands)
                        {
                            Logger.WriteLog($"[EVENT]   - /{cmd.Name}: {cmd.Description}");
                        }
                    }
                    catch(Exception ex)
                    {
                        Logger.WriteLog($"[EVENT] Command registration error: {ex.GetType().Name}: {ex.Message}");
                        Logger.LogException(ex);
                    }

                    try
                    {
                        Logger.WriteLog("[EVENT] Doing channel check");
                        await DiscordUtility.DoChannelCheck();
                        Logger.WriteLog("[EVENT] Channel check complete");
                    }
                    catch(Exception ex)
                    {
                        Logger.WriteLog($"[EVENT] Channel check error: {ex.Message}");
                    }

                    try
                    {
                        Logger.WriteLog("[EVENT] Notifying latest bot version");
                        await BotUtility.NotifyLatestBotVersion();
                        Logger.WriteLog("[EVENT] Version notify complete");
                    }
                    catch(Exception ex)
                    {
                        Logger.WriteLog($"[EVENT] Bot version notify error: {ex.Message}");
                    }

                    try
                    {
                        Logger.WriteLog("[EVENT] Checking localization update");
                        await Localization.CheckUpdate();
                        Logger.WriteLog("[EVENT] Localization check complete");
                    }
                    catch(Exception ex)
                    {
                        Logger.WriteLog($"[EVENT] Localization check error: {ex.Message}");
                    }
                }
            };

            Client.Disconnected += async (ex) =>
            {
                Logger.WriteLog($"[EVENT] Bot Disconnected: {ex?.GetType().Name}");
                Logger.LogException(ex);
                if(ex?.InnerException != null)
                {
                    Logger.WriteLog($"[EVENT] Inner Exception: {ex.InnerException.GetType().Name}");
                    Logger.LogException(ex.InnerException);
                }

                if(ex?.InnerException?.Message.Contains("Authentication failed") == true)
                {
                    Logger.WriteLog("[EVENT] Authentication failed - exiting");
                    Console.WriteLine(Localization.Get("err_disc_auth_fail"));
                    Environment.Exit(1);
                }
            };
            
            Logger.WriteLog("Discord: Event handlers attached");

            // Fire-and-forget async Discord login and start - runs independently
            _ = Task.Run(async () =>
            {
                try
                {
                    Logger.WriteLog("Discord: Logging in");
                    await Client.LoginAsync(TokenType.Bot, DiscordUtility.GetToken());
                    Logger.WriteLog("Discord: Login complete");
                    
                    Logger.WriteLog("Discord: Starting client");
                    await Client.StartAsync();
                    Logger.WriteLog("Discord: Client started successfully");
                    
                    await Client.SetGameAsync(Localization.Get("info_disc_act_bot_ver").KeyFormat(("version", BotVersion)));
                    Logger.WriteLog("Discord: Bot is now running");
                    
                    await Task.Delay(-1); // Keep Discord client running
                }
                catch(Exception ex)
                {
                    Logger.WriteLog($"[FATAL] Discord async error: {ex.GetType().Name}: {ex.Message}");
                    Logger.LogException(ex);
                    Environment.Exit(1);
                }
            });
        }
        catch(Exception ex)
        {
            Logger.WriteLog($"[FATAL] Discord initialization error: {ex.GetType().Name}: {ex.Message}");
            Logger.LogException(ex);
            throw;
        }
    }
}


