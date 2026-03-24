using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using HtmlAgilityPack;

class Program
{
    static async Task Main(string[] args)
    {
        const string stateFile = "sent.txt";

        if (args.Length < 1 || string.IsNullOrWhiteSpace(args[0]))
        {
            Console.WriteLine("ERROR: Discord webhook URL missing (argument 0).");
            Environment.Exit(1);
        }

        string discordWebhookUrl = args[0];

        string[] watchedGames =
        {
            "Escape from Tarkov",
            "Tarkov Arena",
            "ARC Raiders",
            "SCUM",
            "Rust",
            "Enshrouded"
        };

        var sent = File.Exists(stateFile)
            ? new HashSet<string>(File.ReadAllLines(stateFile))
            : new HashSet<string>();

        using var http = new HttpClient();

        try
        {
            Console.WriteLine("Fetching drophunter page...");
            var html = await http.GetStringAsync("https://drophunter.app/drops");

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var pageText = doc.DocumentNode.InnerText;

            foreach (var game in watchedGames)
            {
                if (!pageText.Contains(game, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (sent.Contains(game))
                {
                    Console.WriteLine($"Already sent: {game}");
                    continue;
                }

                Console.WriteLine($"New drop detected: {game}");

                var payload = new
                {
                    content =
                                $@"🎁 **ÚJ TWITCH DROP**
                                🎮 **{game}**
                                🔗 https://www.twitch.tv/drops"
                };

                var json = JsonSerializer.Serialize(payload);

                try
                {
                    var response = await http.PostAsync(
                        discordWebhookUrl,
                        new StringContent(json, Encoding.UTF8, "application/json")
                    );

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"✓ Successfully sent to Discord: {game}");
                        sent.Add(game);
                    }
                    else
                    {
                        Console.WriteLine($"✗ Discord webhook failed with status {response.StatusCode}: {game}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error sending to Discord: {ex.Message}");
                }
            }

            File.WriteAllLines(stateFile, sent);

            Console.WriteLine("Done.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            Environment.Exit(1);
        }
    }
}