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
            Console.WriteLine("ERROR: Discord webhook URL missing.");
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
        http.Timeout = TimeSpan.FromSeconds(20);

        try
        {
            Console.WriteLine("Fetching drophunter page...");
            var html = await http.GetStringAsync("https://drophunter.app/drops");

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var cards = doc.DocumentNode.SelectNodes("//a[contains(@href,'campaign')]");

            if (cards == null)
            {
                Console.WriteLine("No drop cards found.");
                return;
            }

            foreach (var card in cards)
            {
                var cardText = card.InnerText.Trim();

                foreach (var game in watchedGames)
                {
                    if (!cardText.Contains(game, StringComparison.OrdinalIgnoreCase))
                        continue;

                    string todayKey = $"{game}_{DateTime.UtcNow:yyyyMMdd}";

                    if (sent.Contains(todayKey))
                    {
                        Console.WriteLine($"Already sent today: {game}");
                        continue;
                    }

                    Console.WriteLine($"New drop detected: {game}");

                    var payload = new
                    {
                        content = $"🎁 **ÚJ TWITCH DROP**\n🎮 **{game}**\n🔗 https://www.twitch.tv/drops"
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
                            Console.WriteLine($"✓ Sent: {game}");
                            sent.Add(todayKey);
                        }
                        else
                        {
                            Console.WriteLine($"✗ Discord error: {response.StatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✗ Discord send failed: {ex.Message}");
                    }
                }
            }

            File.WriteAllLines(stateFile, sent);
            Console.WriteLine("Done.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }
}
