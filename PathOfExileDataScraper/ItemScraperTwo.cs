using AngleSharp.Dom;
using AngleSharp.Parser.Html;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;
using PathOfExileDataScraper.Items;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PathOfExileDataScraper
{
    internal class ItemScraperTwo
    {

        internal readonly string HtmlTagRegex;
        internal readonly string BaseUrl;

        internal HttpClient _web;
        internal HtmlParser _parser;
        internal Stopwatch _stopwatch;
        internal SqliteConnection _connection;

        internal ConcurrentBag<Map> _maps;
        internal ConcurrentBag<UniqueMap> _uniqueMaps;
        internal ConcurrentBag<DivinationCard> _divinationCards;
        internal ConcurrentBag<Currency> _currencies;
        internal ConcurrentBag<Essence> _essences;

        internal const string MapsUrl = "/Map";
        internal const string UniqueMapsUrl = "/List_of_unique_maps";
        internal const string DivinationCardsUrl = "/List_of_divination_cards";
        internal const string CurrencyUrl = "/Currency";
        internal const string EssencesUrl = "/List_of_essences";

        internal ItemScraperTwo(HttpClient webParams, HtmlParser parserParams, Stopwatch stopwatchParams, SqliteConnection connectionParams,
            string regexParams, string urlParams)
        {

            _web = webParams;
            _parser = parserParams;
            _stopwatch = stopwatchParams;
            _connection = connectionParams;
            HtmlTagRegex = regexParams;
            BaseUrl = urlParams;

        }

        internal async Task Run()
        {

            _stopwatch.Start();
            //await GetMaps();
            Log($"Getting maps took {_stopwatch.Elapsed.TotalSeconds} seconds.");
            _stopwatch.Restart();

            //await GetUniqueMaps();
            Log($"Getting unique maps took {_stopwatch.Elapsed.TotalSeconds} seconds.");
            _stopwatch.Restart();

            //await GetDivinationCards();
            Log($"Getting divination cards took {_stopwatch.Elapsed.TotalSeconds} seconds.");
            _stopwatch.Reset();

            //await GetCurrencies();
            Log($"Getting currency took {_stopwatch.Elapsed.TotalSeconds} seconds.");
            _stopwatch.Reset();

        }

        internal async Task GetMaps()
        {

            Log($"Getting maps...");

            await _connection.ExecuteAsync("CREATE TABLE 'Maps' ( " +
                "'Name' TEXT NOT NULL UNIQUE, " +
                "'MapLevel' INTEGER, " +
                "'Tier' TEXT, " +
                "'Unique' INTEGER, " +
                "'LayoutType' TEXT," +
                "'BossDifficulty' TEXT, " +
                "'LayoutSet' TEXT, " +
                "'UniqueBoss' TEXT," +
                "'NumberOfUniqueBosses' INTEGER," +
                "'SextantCoverage' INTEGER, " +
                "'ImageUrl' TEXT, " +
                "'Url' TEXT, " +
                "PRIMARY KEY('Name') ) ");

            _maps = new ConcurrentBag<Map>();

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(MapsUrl));
            var table = dom.GetElementById("mw-content-text").GetElementsByClassName("wikitable sortable")[1].GetElementsByTagName("tbody").First();
            var maps = table.GetElementsByTagName("tr").Where(element => !element.TextContent.Contains("Itself"));

            var layoutTypes = new Dictionary<char, string>(3)
            {
                { 'A', "The map has a consistent layout that can be reliably fully cleared with no backtracking." },
                { 'B', "The map has an open layout with few obstacles, or has only short and well-connected side paths." },
                { 'C', "The map has an open layout with many obstacles, or has long side paths that require backtracking." }
            };

            var bossDifficulties = new Dictionary<int, string>(5)
            {
                { 0, "Any build can be used." },
                { 1, "Trivial for most builds." },
                { 2, "Moderate damage output that can be easily kited and/or reasonably mitigated by most builds." },
                { 3, "Occasionally high damage output that can be avoided reasonably well." },
                { 4, "High and consistent damage output that can be avoided reasonably well but still very dangerous." },
                { 5, "High and consistent damage output that is difficult to reliably avoid; skipped by many players." }
            };

            Parallel.ForEach(maps, item =>
            {

                var statLines = item.GetElementsByTagName("td");

                var firstStatLine = statLines.First().GetElementsByTagName("a");
                var info = firstStatLine.ElementAtOrDefault(1) ?? firstStatLine.First();

                var formattedBoss = statLines[7].InnerHtml.Replace("<br>", "\\n");
                formattedBoss = Regex.Replace(formattedBoss, HtmlTagRegex, string.Empty);

                var map = new Map
                {

                    Name = info.TextContent,
                    MapLevel = statLines[1].TextContent.Trim(),
                    Tier = statLines[2].TextContent.Trim(),
                    Unique = statLines[3].GetElementsByTagName("img").First().GetAttribute("alt") == "yes",
                    LayoutType = char.TryParse(statLines.ElementAtOrDefault(4)?.TextContent.Trim() ?? "blah", out char c) ? layoutTypes[c] : "N/A", //some are not present
                    BossDifficulty = int.TryParse(statLines.ElementAtOrDefault(5)?.TextContent.Trim() ?? "blah", out int i) ? bossDifficulties[i] : "N/A", //some are not present
                    LayoutSet = statLines[6].TextContent.Trim(),
                    UniqueBoss = formattedBoss,
                    NumberOfUniqueBosses = statLines[8].TextContent.Trim(),
                    SextantCoverage = statLines.ElementAtOrDefault(9)?.TextContent.Trim(),
                    ImageUrl = statLines.First().GetElementsByTagName("img").FirstOrDefault()?.GetAttribute("src") ?? "N/A",
                    Url = BaseUrl + info.GetAttribute("href"),                    

                };

                _maps.Add(map);

            });

            Log("Finished getting maps.");

            Log("Inserting maps into database...");
            await _connection.InsertAsync(_maps);
            Log($"Finished maps into database.");

        }

        internal async Task GetUniqueMaps()
        {

            await _connection.ExecuteAsync("CREATE TABLE 'UniqueMaps' ( " +
                "'Name' TEXT, " +
                "'MapLevel' INTEGER, " +
                "'Stats' TEXT, " +
                "'ImageUrl' TEXT, " +
                "'Url' TEXT, " +
                "PRIMARY KEY('Name') )");

            _uniqueMaps = new ConcurrentBag<UniqueMap>();

            Log("Getting unique maps...");

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(UniqueMapsUrl));
            var table = dom.GetElementById("mw-content-text").GetElementsByTagName("tbody").First();
            var maps = table.GetElementsByTagName("tr").Where(element => !element.TextContent.Contains("Stats"));

            Parallel.ForEach(maps, item =>
            {

                var statLines = item.GetElementsByTagName("td");
                var info = statLines.First().GetElementsByTagName("a").First();
                var formattedStats = statLines[2].InnerHtml.Replace("<br>", "\\n");

                var map = new UniqueMap
                {

                    Name = info.TextContent,
                    MapLevel = statLines[1].TextContent,
                    Stats = Regex.Replace(formattedStats, HtmlTagRegex, string.Empty),
                    ImageUrl = statLines.First().GetElementsByTagName("img").First().GetAttribute("src"),
                    Url = BaseUrl + info.GetAttribute("href"),

                };

                _uniqueMaps.Add(map);

            });

            Log("Finished getting unique maps.");

            Log("Inserting unique maps...");
            await _connection.InsertAsync(_uniqueMaps);
            Log("Finished inserting unique maps.");

        }

        internal async Task GetDivinationCards()
        {

            await _connection.ExecuteAsync("CREATE TABLE 'DivinationCards' ( " +
                "'Name' TEXT NOT NULL UNIQUE, " +
                "'Set' TEXT, " +
                "'Reward' TEXT, " +
                "'IsRewardCorrupted' INTEGER, " +
                "'DropRestrictions' TEXT, " +
                "'ImageUrl' TEXT, " +
                "'Url' TEXT, " +
                "PRIMARY KEY('Name') )");

            _divinationCards = new ConcurrentBag<DivinationCard>();

            Log("Getting divination cards...");
            Log("May take longer due to the need to fetch each individual art.");

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(DivinationCardsUrl));
            var cards = dom.GetElementById("mw-content-text").GetElementsByTagName("tbody").First().GetElementsByTagName("tr").Where(element => !element.TextContent.Contains("Drop Restrictions"));

            Parallel.ForEach(cards, item =>
            {

                var statLines = item.GetElementsByTagName("td");
                var info = statLines.First().FirstElementChild;
                var divinationCardUrl = info.GetAttribute("href");
                var rewardElement = statLines[2].InnerHtml;
                var formattedReward = rewardElement.Replace("Corrupted", string.Empty).Trim().Replace(" <br> ", "\\n").Replace("<br>", "\\n");
                var formattedRestrictions = statLines[3].InnerHtml.Replace(" <br> ", "\\n").Replace("<br>", "\\n");

                var card = new DivinationCard
                {

                    Name = info.TextContent,
                    Set = int.Parse(statLines[1].TextContent),
                    Reward = Regex.Replace(formattedReward, HtmlTagRegex, string.Empty),
                    IsRewardCorrupted = rewardElement.Contains("Corrupted"),
                    DropRestrictions = Regex.Replace(formattedRestrictions, HtmlTagRegex, string.Empty),
                    Url = BaseUrl + divinationCardUrl,

                };

                var cardDom = _parser.Parse(_web.GetStringAsync(divinationCardUrl).Result);
                var imageUrl = cardDom.GetElementsByClassName("divicard-art").First().FirstElementChild.GetAttribute("src");
                card.ImageUrl = imageUrl;

                _divinationCards.Add(card);

            });

            Log("Finished getting divination cards.");

            Log("Inserting divination cards...");
            await _connection.InsertAsync(_divinationCards);
            Log("Finished inserting divination cards.");

        }

        internal async Task GetCurrencies()
        {

            await _connection.ExecuteAsync("CREATE TABLE 'Currencies' ( " +
                "'Name' TEXT, " +
                "'DropLevel' TEXT, " +
                "'StackSize' TEXT, " +
                "'TabStackSize' TEXT, " +
                "'HelpText' TEXT, " +
                "'IsDiscontinued' INTEGER, " +
                "'ImageUrl' TEXT, " +
                "'Url' TEXT, " +
                "PRIMARY KEY('Name') )");

            _currencies = new ConcurrentBag<Currency>();

            Log("Getting orbs and scrolls...");

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(CurrencyUrl));
            var tables = dom.GetElementById("mw-content-text").GetElementsByTagName("tbody");

            await GetOrbScrolls(tables[0], false);
            await GetOrbScrolls(tables[1], false);
            await GetOrbScrolls(tables[3], true); //table[2] is perandus coins

            Log("Finished getting orbs and scrolls");
            Log("Inserting currencies...");
            await _connection.InsertAsync(_currencies);
            Log("Finished inserting currencies.");

            await _connection.ExecuteAsync("CREATE TABLE 'Essences' ( " +
                "'Name' TEXT, " +
                "'Tier' TEXT, " +
                "'DropLevel' TEXT, " +
                "'Effects' TEXT, " +
                "'ImageUrl' TEXT, " +
                "'Url' TEXT, " +
                "PRIMARY KEY('Name') )");

            _essences = new ConcurrentBag<Essence>();

            dom = await _parser.ParseAsync(await _web.GetStringAsync(EssencesUrl));
            var table = dom.GetElementById("mw-content-text").GetElementsByTagName("tbody").First();
            var essences = table.GetElementsByTagName("tr").Where(element => !element.TextContent.Contains("Effect(s)"));

            Log("Getting essences...");

            Parallel.ForEach(essences, item =>
            {

                var statLines = item.GetElementsByTagName("td");
                var info = statLines.First().FirstElementChild;
                var formattedEffects = statLines[3].InnerHtml.Replace(" <br> ", "\\n").Replace("<br>", "\\n");

                var essence = new Essence
                {

                    Name = info.FirstElementChild.TextContent,
                    Tier = statLines[1].TextContent,
                    DropLevel = statLines[2].TextContent,
                    Effects = Regex.Replace(formattedEffects, HtmlTagRegex, string.Empty),
                    ImageUrl = info.GetElementsByTagName("img").First().GetAttribute("src"),
                    Url = BaseUrl + WebUtility.UrlDecode(info.FirstElementChild.GetAttribute("href")),

                };

                _essences.Add(essence);

            });

            Log("Finished getting essences.");
            Log("Inserting essences...");
            await _connection.InsertAsync(_essences);
            Log("Finished inserting essences.");

        }

        #region Currency
        internal Task GetOrbScrolls(IElement table, bool isDiscontinued)
        {
            
            var currencies = table.GetElementsByTagName("tr").Where(element => !element.TextContent.Contains("Help Text"));

            Parallel.ForEach(currencies, item =>
            {

                var statLines = item.GetElementsByTagName("td");
                var info = statLines.First().FirstElementChild;
                var formattedHelp = statLines[4].InnerHtml.Replace("<br>", "\\n");

                var currency = new Currency
                {

                    Name = info.FirstElementChild.TextContent,
                    DropLevel = statLines[1].TextContent,
                    StackSize = statLines[2].TextContent,
                    TabStackSize = statLines[3].TextContent,
                    HelpText = Regex.Replace(formattedHelp, HtmlTagRegex, string.Empty),
                    IsDiscontinued = isDiscontinued,
                    ImageUrl = info.GetElementsByTagName("img").First().GetAttribute("src"),
                    Url = BaseUrl + info.FirstElementChild.GetAttribute("href"),

                };

                _currencies.Add(currency);

            });
            
            return Task.CompletedTask;

        }
        #endregion Currency

        internal void Log(string message)
            => Console.WriteLine($"{DateTime.Now} {message}");

    }
}
