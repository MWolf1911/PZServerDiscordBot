using Discord.Interactions;
using System;
using System.Linq;
using System.Threading.Tasks;

[Group("workshop", "Manage workshop mods on the server")]
public class WorkshopCommands : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("list", "Lists all workshop mods installed on the server")]
    public async Task ListWorkshopMods()
    {
        try
        {
            string configFilePath = ServerUtility.GetServerConfigIniFilePath();
            if (string.IsNullOrEmpty(configFilePath))
            {
                await RespondAsync("Error: Could not find server configuration file", ephemeral: true);
                return;
            }

            IniParser.IniData iniData = IniParser.Parse(configFilePath);
            if (iniData == null)
            {
                await RespondAsync("Error: Could not parse server configuration", ephemeral: true);
                return;
            }

            string workshopItems = iniData.GetValue("WorkshopItems");
            string mods = iniData.GetValue("Mods");

            if (string.IsNullOrEmpty(workshopItems) && string.IsNullOrEmpty(mods))
            {
                await RespondAsync("No workshop mods configured", ephemeral: true);
                return;
            }

            string response = "**Workshop Mods:**\n";
            
            if (!string.IsNullOrEmpty(workshopItems))
            {
                string[] workshopIdList = workshopItems.Split(';');
                response += $"**Workshop IDs:** {workshopIdList.Length} items\n";
                foreach (string id in workshopIdList)
                {
                    if (!string.IsNullOrWhiteSpace(id))
                        response += $"• {id}\n";
                }
            }

            if (!string.IsNullOrEmpty(mods))
            {
                string[] modList = mods.Split(';');
                response += $"\n**Mod IDs:** {modList.Length} items\n";
                foreach (string mod in modList)
                {
                    if (!string.IsNullOrWhiteSpace(mod))
                        response += $"• {mod}\n";
                }
            }

            await RespondAsync(response, ephemeral: true);
        }
        catch (Exception ex)
        {
            Logger.WriteLog($"[WORKSHOP] Error listing mods: {ex.Message}");
            Logger.LogException(ex);
            await RespondAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }

    [SlashCommand("add", "Adds a workshop mod to the server")]
    public async Task AddWorkshopMod(
        [Summary("workshop_id", "The Steam Workshop ID")] string workshopId,
        [Summary("mod_id", "The mod ID (e.g., ModName)")] string modId = null)
    {
        try
        {
            // Validate workshop ID is numeric
            if (!workshopId.All(char.IsDigit))
            {
                await RespondAsync("Error: Workshop ID must be numeric", ephemeral: true);
                return;
            }

            string configFilePath = ServerUtility.GetServerConfigIniFilePath();
            if (string.IsNullOrEmpty(configFilePath))
            {
                await RespondAsync("Error: Could not find server configuration file", ephemeral: true);
                return;
            }

            IniParser.IniData iniData = IniParser.Parse(configFilePath);
            if (iniData == null)
            {
                await RespondAsync("Error: Could not parse server configuration", ephemeral: true);
                return;
            }

            // Get current workshop items
            string currentWorkshopItems = iniData.GetValue("WorkshopItems") ?? "";
            string[] workshopList = currentWorkshopItems.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            // Check if already exists
            if (workshopList.Contains(workshopId))
            {
                await RespondAsync($"Workshop ID {workshopId} is already in the list", ephemeral: true);
                return;
            }

            // Add to workshop items
            string newWorkshopItems = string.IsNullOrEmpty(currentWorkshopItems) 
                ? workshopId 
                : currentWorkshopItems.TrimEnd(';') + ";" + workshopId;
            iniData.SetValue("WorkshopItems", newWorkshopItems);

            // If mod ID provided, add to Mods list
            if (!string.IsNullOrEmpty(modId))
            {
                string currentMods = iniData.GetValue("Mods") ?? "";
                string[] modList = currentMods.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                if (!modList.Contains(modId))
                {
                    string newMods = string.IsNullOrEmpty(currentMods)
                        ? modId
                        : currentMods.TrimEnd(';') + ";" + modId;
                    iniData.SetValue("Mods", newMods);
                }
            }

            // Save the file
            IniParser.Save(configFilePath, iniData);

            string message = $"Added workshop mod {workshopId}";
            if (!string.IsNullOrEmpty(modId))
                message += $" with mod ID {modId}";
            message += "\n**Note:** Server restart required for changes to take effect";

            Logger.WriteLog($"[WORKSHOP] Added workshop ID {workshopId}" + 
                          (!string.IsNullOrEmpty(modId) ? $" with mod ID {modId}" : ""));
            
            await RespondAsync(message, ephemeral: true);
        }
        catch (Exception ex)
        {
            Logger.WriteLog($"[WORKSHOP] Error adding mod: {ex.Message}");
            Logger.LogException(ex);
            await RespondAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }

    [SlashCommand("remove", "Removes a workshop mod from the server")]
    public async Task RemoveWorkshopMod(
        [Summary("workshop_id", "The Steam Workshop ID to remove")] string workshopId)
    {
        try
        {
            string configFilePath = ServerUtility.GetServerConfigIniFilePath();
            if (string.IsNullOrEmpty(configFilePath))
            {
                await RespondAsync("Error: Could not find server configuration file", ephemeral: true);
                return;
            }

            IniParser.IniData iniData = IniParser.Parse(configFilePath);
            if (iniData == null)
            {
                await RespondAsync("Error: Could not parse server configuration", ephemeral: true);
                return;
            }

            // Get current workshop items
            string currentWorkshopItems = iniData.GetValue("WorkshopItems") ?? "";
            string[] workshopList = currentWorkshopItems.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            // Check if exists
            if (!workshopList.Contains(workshopId))
            {
                await RespondAsync($"Workshop ID {workshopId} not found in the list", ephemeral: true);
                return;
            }

            // Remove from workshop items
            var newWorkshopList = workshopList.Where(id => id != workshopId).ToArray();
            string newWorkshopItems = string.Join(";", newWorkshopList);
            iniData.SetValue("WorkshopItems", newWorkshopItems);

            // Save the file
            IniParser.Save(configFilePath, iniData);

            Logger.WriteLog($"[WORKSHOP] Removed workshop ID {workshopId}");
            
            await RespondAsync($"Removed workshop mod {workshopId}\n**Note:** Server restart required for changes to take effect", 
                              ephemeral: true);
        }
        catch (Exception ex)
        {
            Logger.WriteLog($"[WORKSHOP] Error removing mod: {ex.Message}");
            Logger.LogException(ex);
            await RespondAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }

    [SlashCommand("clear", "Removes all workshop mods from the server")]
    public async Task ClearWorkshopMods()
    {
        try
        {
            string configFilePath = ServerUtility.GetServerConfigIniFilePath();
            if (string.IsNullOrEmpty(configFilePath))
            {
                await RespondAsync("Error: Could not find server configuration file", ephemeral: true);
                return;
            }

            IniParser.IniData iniData = IniParser.Parse(configFilePath);
            if (iniData == null)
            {
                await RespondAsync("Error: Could not parse server configuration", ephemeral: true);
                return;
            }

            // Clear both WorkshopItems and Mods
            iniData.SetValue("WorkshopItems", "");
            iniData.SetValue("Mods", "");

            // Save the file
            IniParser.Save(configFilePath, iniData);

            Logger.WriteLog("[WORKSHOP] Cleared all workshop mods");
            
            await RespondAsync("Cleared all workshop mods\n**Note:** Server restart required for changes to take effect", 
                              ephemeral: true);
        }
        catch (Exception ex)
        {
            Logger.WriteLog($"[WORKSHOP] Error clearing mods: {ex.Message}");
            Logger.LogException(ex);
            await RespondAsync($"Error: {ex.Message}", ephemeral: true);
        }
    }
}
