using System.Threading.Tasks;
using Cartagena;
using Cartagena.Menu;
using Cartagena.Models;
using Spectre.Console;


class Program
{
    private static string? clientId = null;
    private static string? clientSecret = null;
    private static TwitchBot? twitchBot = null;
    private static Config? loadedConfig = null;


    static async Task Main(string[] args)
    {
        if (!args.Contains("-nosplash".ToLower()))
            await Splash();

        AnsiConsole.Clear();

        bool exit = false;

        while (!exit)
        {
            var mainMenuChoice = await Menus.MainM();
            switch (mainMenuChoice)
            {
                case "Config Menu":
                    await HandleConfigMenu();
                    break;

                case "Twitch Bot Menu":

                    break;

                case "Close App":
                    await ShutdownApplication();
                    exit = true; //this should never run, but still.
                    break;
                default:
                    AnsiConsole.WriteLine($"[red]Something seems to have gone wrong![/]");
                    break;
            }
            if (!exit)
            {
                await Menus.WaitForAnyKeyAsync();
                AnsiConsole.Clear(); // Optional: clear screen between menu cycles
            }
        }
    }

    private static async Task HandleConfigMenu()
    {
        bool exit = false;
        while (!exit)
        {
            var configMenuChoice = await Menus.Configuration();
            switch (configMenuChoice)
            {
                case "New Config":
                    await ConfigManager.NewConfig();
                    break;

                case "Load Config":
                    Config? cfg = await ConfigManager.LoadConfig();
                    if (cfg != null) { loadedConfig = cfg; }
                    break;

                case "View Loaded Config":
                    await ViewLoadedConfig();
                    break;
                
                case "View Saved Config":
                    await ViewSavedConfig();
                    break;

                case "Go Back":
                    exit = true;
                    break;
            }
        }
    }

    private static async Task HandleBotMenu()
    {
        
    }

    public static async Task ViewSavedConfig()
    {
        Config? cfg = await ConfigManager.LoadConfig();

        if (cfg != null)
        {
            await Menus.ViewJSON(cfg, "config.json");
            await Menus.WaitForAnyKeyAsync("Press any key to continue...");
        }
    }

    public static async Task ViewLoadedConfig()
    {
        Config? cfg = loadedConfig;

        if (cfg != null)
        {
            await Menus.ViewJSON(cfg, "Loaded Config");
            await Menus.WaitForAnyKeyAsync("Press any key to continue...");
        }
        else
        {
            await Menus.WaitForAnyKeyAsync("No Config is loaded! Please load or create a valid Config.\nPress any key to continue...");
        }
    }

    private static async Task ShutdownApplication()
    {
        //TODO Do more and more gracefully here
        AnsiConsole.Clear();
        Environment.Exit(0);
    }

    //Splash art because I'm fancy like that
    private static string asciiArti = @"                                                    
                        @@@@                        
                        @@@@                        
                        @@@@                        
                       @@@@@                        
                      @@@@@@@@                      
                  @@@@@@@@@@@@@@@@                  
                @@@@@@@      @@@@@@@                
              @@@@@  @@@@@@@@@@  @@@@@              
             @@@@  @@@@@@  @@@@@@  @@@@             
            @@@@  @@@@        @@@@@ @@@@            
            @@@@ @@@@   @@@@    @@@  @@@            
            @@@ @@@@   @@@@@@   @@@@ @@@            
            @@@ @@@@   @@@@@@   @@@@ @@@            
            @@@@ @@@@   @@@@    @@@  @@@            
            @@@@  @@@@        @@@@@ @@@@            
             @@@@  @@@@@@@@@@@@@@  @@@@             
              @@@@@  @@@@@@@@@@  @@@@@              
                @@@@@@@      @@@@@@@                
               @@@@@@@@@@@@@@@@@@@@@                
              @@@@@@@  @@@@@@ @@@@@@@               
              @@@@@@   @@@@@@   @@@@@@              
                 @@    @@@@@@    @@                 
                       @@@@@@                       
                       @@@@@@                       ";
    private static async Task Splash()
    {
        var version = System.Reflection.Assembly.GetEntryAssembly()!.GetName().Version;
        AnsiConsole.Clear();
        AnsiConsole.Write(new Markup($"[blue]{asciiArti}[/]").Centered());
        AnsiConsole.Write("\n");
        AnsiConsole.Write(new FigletText("CARTAGENA").Centered().Color(Color.Red));
        AnsiConsole.Write(new Markup($"[bold blue]A Speedrun Marathon bot by Eris\nv. {version}[/]\n").Centered());
        await Task.Delay(5000);
        AnsiConsole.Clear();
    }
}

