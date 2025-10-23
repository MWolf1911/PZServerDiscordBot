using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class ServerPath
{
    public static string BasePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Zomboid\\";

    public static void CheckCustomBasePath()
    {
        string serverFile = "./server.bat";

        if(!File.Exists(serverFile))
        {
            Console.WriteLine(Localization.Get("err_serv_bat"));
            File.AppendAllText("startup.log", "ERROR: server.bat not found!\n");
            Environment.Exit(1);
        }

        try
        {
            File.AppendAllText("startup.log", "Setup: Parsing server.bat\n");
            string[] lines = File.ReadAllLines(serverFile);

        for(int i=0; i < lines.Length; i++)
        {
            string line = lines[i];

            // Look for strings that the command to start the Project Zomboid server may contain.
            if(line.Contains(@".\jre64\bin\java.exe")
            || line.Contains("zomboid.steam")
            || line.Contains("-Xms")
            || line.Contains("-Xmx"))
            {
                string[] args = line.Split(new string[] { " -" }, StringSplitOptions.None);

                foreach(string arg in args)
                {
                    if(arg.Contains("user.home"))
                    {
                        BasePath = arg.Split('=').Last() + "\\";

                        if(Directory.Exists(BasePath + "Zomboid\\"))
                            BasePath += "Zomboid\\";
                    }
                }
            }
            else if(line.Trim().ToLower() == "pause")
            {
                // server.bat shouldn't contain more than one "pause" and
                // it should be at the end of file so we can just break
                // out of for loop after we remove it.
                List<string> newLines = new List<string>(lines);
                newLines.RemoveAt(i);
                File.WriteAllLines(serverFile, newLines);
                break;
            }
        }
            File.AppendAllText("startup.log", "Setup: server.bat parsed successfully\n");
        }
        catch (Exception ex)
        {
            File.AppendAllText("startup.log", $"ERROR parsing server.bat: {ex.Message}\n");
            throw;
        }
    }

    public static string ServerLogsPath()
    {
    #if DEBUG
        return ".\\Logs\\";
    #else
        string path = BasePath + "Logs\\";
        return path;
    #endif
    }

    public static string ServerSettingsPath()
    {
        string path = BasePath + "Server\\";
        return path;
    }
    public static string ServerBaseSavesPath()
    {
        string path = BasePath + "Saves\\";
        return path;
    }

    public static string ServerSavesPath()
    {
        string path = BasePath + "Saves\\Multiplayer\\" + ServerUtility.GetServerConfigIniFileName(true) + "\\";
        return path;
    }

    public static string ServerSavesPlayerPath()
    {
        string path = BasePath + "Saves\\Multiplayer\\" + ServerUtility.GetServerConfigIniFileName(true) + "_player\\";
        return path;
    }

    public static string ServerDatabasePath()
    {
        string path = BasePath + "db\\";
        return path;
    }

    public static string ServerLuaPath()
    {
        string path = BasePath + "Lua\\";
        return path;
    }

    public static string MapTimeFilePath()
    {
        string path = ServerSavesPath() + "map_t.bin";
        return path;
    }
}
