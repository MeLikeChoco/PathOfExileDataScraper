using System;
using System.Threading.Tasks;
using System.Linq;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;
using System.Net.Http;
using Dapper;
using AngleSharp.Parser.Html;
using AngleSharp.Dom;
using System.Threading;
using System.Collections.Generic;

namespace PathOfExileDataScraper
{

    class Program
    {
        static void Main(string[] args)
            => new Program().Start().GetAwaiter().GetResult();

        internal const string InMemoryDb = "Data Source = :memory:";
        internal const string FlatFileDb = "Data Source = PathOfExile.db";
        SqliteConnection  _connection;

        internal HttpClient _web;
        internal HtmlParser _parser;

        internal const string GenericAxesUrl = "/List_of_axes";
        internal const string GenericBowsUrl = "/List_of_bows";
        internal const string GenericClawsUrl = "/List_of_claws";
        internal const string GenericDaggersUrl = "/List_of_daggers";
        internal const string GenericFishingRodsUrl = "/List_of_fishing_rods";
        internal const string GenericMacesUrl = "/List_of_maces";
        internal const string GenericStavesUrl = "/List_of_staves"; //aka staff
        internal const string GenericSwordsUrl = "/List_of_swords";
        internal const string GenericWandsUrl = "/List_of_wands";

        internal int _counter = 0;
        internal int _size;

        internal async Task Start()
        {

            _web = new HttpClient { BaseAddress = new Uri("http://pathofexile.gamepedia.com") };
            _parser = new HtmlParser();
            _connection = new SqliteConnection(FlatFileDb);
            await _connection.OpenAsync();
            await GetGenericWeapons();
            _connection.Close();

            Console.ReadKey();

        }

        internal async Task GetGenericWeapons()
        {

            Log("Getting axes...");

            await _connection.ExecuteAsync("CREATE TABLE 'GenericWeapons' (" +
                "'Name' TEXT NOT NULL UNIQUE, " +
                "'LevelReq' INTEGER, " +
                "'Strength' INTEGER, " +
                "'Dexterity' INTEGER, " +
                "'Intelligence' INTEGER, " +
                "'Damage' TEXT, " +
                "'APS' REAL, " +
                "'CritChance' REAL, " +
                "'DPS' REAL, " +
                "'Stats' TEXT," +
                "'ImageUrl' TEXT," +
                "PRIMARY KEY('Name') )");

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(GenericAxesUrl));
            var mainDom = dom.GetElementById("mw-content-text");
            var weapons = mainDom.GetElementsByTagName("tbody").SelectMany(element => element.GetElementsByTagName("tr").Where(e => e.TextContent != "ItemDamageAPSCritDPSStats"));
            _size = weapons.Count();
            var store = new List<GenericWeapon>();

            Parallel.ForEach(weapons, new ParallelOptions { MaxDegreeOfParallelism = 1 }, async axe =>
            {
                store.Add(await GetGenericAxes(axe));
            });

            await _connection.InsertAsync(store);

            _counter = 0;

        }

        internal Task<GenericWeapon> GetGenericAxes(IElement weapon)
        {

            var statLines = weapon.GetElementsByTagName("td");

            var axe = new GenericWeapon
            {

                Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                Strength = int.Parse(statLines[2].TextContent),
                Dexterity = int.Parse(statLines[3].TextContent),
                Damage = statLines[4].TextContent,
                APS = double.Parse(statLines[5].TextContent),
                CritChance = double.Parse(statLines[6].TextContent.Substring(0, statLines[6].TextContent.Length - 2)), //i have no idea it there will always be 2 trailing digits after the .
                DPS = double.Parse(statLines[7].TextContent),
                ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                Stats = statLines.ElementAtOrDefault(8)?.TextContent ?? "N/A",

            };

            InLineLog(Interlocked.Increment(ref _counter).ToString() + $"/{_size}");
            return Task.FromResult(axe);

            //return Task.CompletedTask;

        }

        internal void Log(string message)
            => Console.WriteLine(message);

        internal void InLineLog(string message)
            => Console.Write($"\r{message}");

    }

}