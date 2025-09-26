using Cartagena;

class Program
{
    private const string CLIENT_ID = "cnfaczx3u66tev6siqdv7fm33224a0";
    private static string? clientSecret = null;
    private static TwitchBot? twitchBot = null;

    static async Task Main(string[] args)
    {
        await RunConsoleCommandFlow();
    }

    private static async Task RunConsoleCommandFlow()
    {
        var commands = new Dictionary<string, Func<string[], Task>>(StringComparer.OrdinalIgnoreCase)
        {
            ["secret"] = async args => await SetClientSecret(),
            ["twitchbot"] = async args => await TwitchBot(args)

        };
        
        while (true)
        {
            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input)) continue;

            string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string cmd = parts[0];
            string[] args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

            if (commands.TryGetValue(cmd, out var action))
            {
                await action(args);
            }
            else
            {
                Console.WriteLine($"Unknown command: {cmd}");
            }
        }
    }

    //TODO this flow sucks fucking balls, make a better one.
    private static async Task TwitchBot(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: bot <subcommand>");
            Console.WriteLine("Subcommands: start, stop");
            return;
        }
        if (twitchBot == null || !twitchBot.ClientIsConnected())
        {
            Console.WriteLine("Bot is not connected or not authenticated!");
        }
        if (clientSecret == null)
        {
            Console.WriteLine("Client Secret not set! Do 'secret'");
            return;
        }

        if (args[0].ToLower() == "start")
        {
            if (twitchBot == null)
            {
                twitchBot = new TwitchBot(CLIENT_ID, clientSecret);
            }
            await twitchBot.InitializeAsync();
        }

        if (args[0].ToLower() == "stop")
        {
            //TODO
        }
    }

    private static async Task SetClientSecret()
    {
        Console.Write("Enter your Twitch App's CLIENT SECRET:\n");
        clientSecret = ReadSecretFromConsole();
        Console.WriteLine("Client secret set.");
        await Task.CompletedTask;
    } 
    

    private static string ReadSecretFromConsole()
    {
        string input = "";
        ConsoleKeyInfo key;

        do
        {
            key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter)
                break;
            if (key.Key == ConsoleKey.Backspace && input.Length > 0)
            {
                input = input.Substring(0, input.Length - 1);
                Console.Write("\b \b");
            }
            else if (!char.IsControl(key.KeyChar))
            {
                input += key.KeyChar;
                Console.Write("*"); // show asterisk instead of the actual character
            }
        } while (true);

        Console.WriteLine();
        return input;
    }
}

