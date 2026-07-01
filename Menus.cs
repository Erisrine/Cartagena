using System.Text.Json;
using Cartagena.Models;
using Spectre.Console;
using Spectre.Console.Json;

namespace Cartagena.Menu
{
    public static class Menus
    {
        public static async Task<string> MainM()
        {
            AnsiConsole.Clear();

            AnsiConsole.Write(
            new FigletText("MAIN MENU")
                .LeftJustified()
                .Color(Color.Red));

            return await AnsiConsole.PromptAsync(
            new SelectionPrompt<string>()
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
                .AddChoices(new[]
                {
                    "Config Menu", "Close App"
                }));
        }

        public static async Task<string> Configuration()
        {
            AnsiConsole.Clear();

            return await AnsiConsole.PromptAsync(
            new SelectionPrompt<string>()
                .Title("Configuration Menu")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
                .AddChoices(new[]
                {
                    "New Config", "Load Config", "View Loaded Config", "Go Back"
                }));
        }

        public static async Task<string> NewConfigOptions()
        {
            AnsiConsole.Clear();

            return await AnsiConsole.PromptAsync(
            new SelectionPrompt<string>()
                .Title("What would you like to do with this Config?")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
                .AddChoices(new[]
                {
                    "Save & Load Config", "Load and use without saving", "Discard"
                }));
        }

        public static async Task WaitForAnyKeyAsync(string message = "[grey]Press any key to continue...[/]") //Jank?
        {
            AnsiConsole.MarkupLine(message);
            await Task.Run(() => Console.ReadKey(true));
        }

        public static async Task<bool> MakeYNPrompt(string question, bool bdefault = true)
        {
            return await AnsiConsole.PromptAsync(
                new TextPrompt<bool>(question)
                    .AddChoice(true)
                    .AddChoice(false)
                    .DefaultValue(bdefault)
                    .WithConverter(choice => choice ? "y" : "n"));
        }


        //This could easily be a synchronous function and it would be fine
        //but I don't want to risk the bot getting blocked at all ever.
        //That is if I understand async tasks and how they work correctly. But anyway.
        public static async Task ViewJSON(Config? cfg, string header = "JSON", Color? frameColor = null)
        {
            string jsonString = JsonSerializer.Serialize(cfg, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            var color = frameColor ?? Color.Yellow;
            var json = new JsonText(jsonString);

            AnsiConsole.Write(
                new Panel(json)
                    .Header(header)
                    .Collapse()
                    .RoundedBorder()
                    .BorderColor(color)
            );
            await Task.Delay(1);
            //await WaitForAnyKeyAsync();
        }
    }
}