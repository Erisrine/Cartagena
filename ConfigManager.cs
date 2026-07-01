using System.Text.Json;
using Cartagena.Models;
using Spectre.Console;
using Cartagena.Menu;

namespace Cartagena
{
    public static class ConfigManager
    {
        private const string CONFIG_FILE = "config.json";

        public static async Task<Config?> LoadConfig()
        {
            if (File.Exists(CONFIG_FILE))
            {
                try
                {
                    await using var stream = File.OpenRead(CONFIG_FILE);
                    var cfg = await JsonSerializer.DeserializeAsync<Config>(stream);

                    if (!string.IsNullOrWhiteSpace(cfg?.ClientId) &&        //Maybe this check is dumb
                        !string.IsNullOrWhiteSpace(cfg?.ClientSecret))
                    {
                        await Menus.WaitForAnyKeyAsync("Config loaded. Press any key to continue...");
                        return cfg;
                    }
                }
                catch (Exception e)
                {
                    await Menus.WaitForAnyKeyAsync($"Could not load Config:\n{e.Message}\nPress any key to continue...");
                    return null;
                }
            }
            await Menus.WaitForAnyKeyAsync($"Could not find {CONFIG_FILE}, Press any key to continue...");
            return null;
        }
        public static async Task NewConfig()
        {
            var cfg = await PromptForConfig();
            bool savecfg = await Menus.MakeYNPrompt("Would you like to save this new Config?");

            if (savecfg)
            {
                await SaveConfig(cfg);
            }
        }

        public static async Task<Config> PromptForConfig()
        {
            var cfg = new Config
            {
                ClientId = await AnsiConsole.PromptAsync(
                        new TextPrompt<string>("Enter your Twitch App [green]Client ID[/]:")
                        .PromptStyle("cyan")),

                ClientSecret = await AnsiConsole.PromptAsync(
                        new TextPrompt<string>("Enter your Twitch App [green]Client Secret[/]:")
                        .Secret().PromptStyle("red"))

            };
            return cfg;
        }

        public static async Task<bool> SaveConfig(Config cfg)
        {
            try
            {
                string json = JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(CONFIG_FILE, json);

                AnsiConsole.MarkupLine("[grey]Saved credentials to config.json[/]");
                return true;
            }
            catch (Exception e)
            {
                AnsiConsole.MarkupLine($"[red]Failed to save config:[/] {e.Message}");
                return false;
            }
        }
    }
}