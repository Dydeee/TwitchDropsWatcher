using System.Net.Http;
using System.Text;
using System.Text.Json;
using HtmlAgilityPack;

// ================== BEÁLLÍTÁSOK ==================

var discordWebhookUrl = "https://discord.com/api/webhooks/1457397565111668797/wYOAi5SMIMVDEBeJh045IqJ0uAHj_eCQJTzQxa4s7UH1sNu7fFGxsmN0w7Q3cvX8Pm3q";

var watchedGames = new[]
{
   "Escape from Tarkov",
    "Tarkov Arena",
    "ARC Raiders",
    "Rust",
    "Enshrouded",
    "SCUM"
};

var stateFile = "sent.txt";

// =================================================

var sent = File.Exists(stateFile)
    ? new HashSet<string>(File.ReadAllLines(stateFile))
    : new HashSet<string>();

using var http = new HttpClient();
http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

// 1️⃣ Oldal letöltése
var html = await http.GetStringAsync("https://drophunter.app/drops");

// DEBUG – ha kell még valaha
File.WriteAllText("debug_drophunter.html", html);

// 2️⃣ HTML parse
var doc = new HtmlDocument();
doc.LoadHtml(html);

// 3️⃣ Kampány sorok (TABLE)
var rows = doc.DocumentNode.SelectNodes("//table//tbody//tr");

if (rows == null)
{
    Console.WriteLine("Nem található kampány táblázat.");
    return;
}

foreach (var row in rows)
{
    // JÁTÉK NÉV
    var gameNode = row.SelectSingleNode(".//h6");
    if (gameNode == null)
        continue;

    var gameName = HtmlEntity.DeEntitize(gameNode.InnerText).Trim();

    // csak a minket érdeklő játékok
    if (!watchedGames.Any(g =>
        gameName.Contains(g, StringComparison.OrdinalIgnoreCase)))
        continue;

    // VAN-E AKTÍV KAMPÁNY
    var campaigns = row.SelectNodes(".//div[contains(@class,'campaign-item')]");
    if (campaigns == null || campaigns.Count == 0)
        continue;

    // 1 játék = 1 értesítés
    if (sent.Contains(gameName))
        continue;

    var msg = new
    {
        content =
            "🎁 **AKTÍV TWITCH DROP**\n" +
            $"🎮 **{gameName}**\n" +
            "🔗 https://drophunter.app/drops"
    };

    await http.PostAsync(
        discordWebhookUrl,
        new StringContent(
            JsonSerializer.Serialize(msg),
            Encoding.UTF8,
            "application/json")
    );

    sent.Add(gameName);
}

// 4️⃣ Állapot mentése
File.WriteAllLines(stateFile, sent);

Console.WriteLine("Kész.");
