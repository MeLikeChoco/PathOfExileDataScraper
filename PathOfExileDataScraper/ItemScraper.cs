using System;
using System.Threading.Tasks;
using System.Linq;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;
using System.Net.Http;
using Dapper;
using AngleSharp.Parser.Html;
using AngleSharp.Dom;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using AngleSharp.Dom.Html;
using System.Reflection;

//I tried so hard to reduce the lines of code needed that now I'm scared to the fact that I may not
//even remember what some parts even do after taking a break from this.

namespace PathOfExileDataScraper
{

    class ItemScraper
    {
        static void Main(string[] args)
            => new ItemScraper().Start().GetAwaiter().GetResult();

        internal const string InMemoryDb = "Data Source = :memory:";
        internal const string FlatFileDb = "Data Source = PathOfExile.db";
        SqliteConnection _connection;
        internal HttpClient _web;
        internal HtmlParser _parser;
        internal Stopwatch _stopwatch;

        #region Weapons
        internal const string GenericAxesUrl = "/List_of_axes";
        internal const string GenericBowsUrl = "/List_of_bows";
        internal const string GenericClawsUrl = "/List_of_claws";
        internal const string GenericDaggersUrl = "/List_of_daggers";
        internal const string GenericFishingRodsUrl = "/List_of_fishing_rods";
        internal const string GenericMacesUrl = "/List_of_maces";
        internal const string GenericStavesUrl = "/List_of_staves"; //aka staff
        internal const string GenericSwordsUrl = "/List_of_swords";
        internal const string GenericWandsUrl = "/List_of_wands";
        #endregion Weapons

        #region Armors
        internal const string GenericBodyArmoursUrl = "/List_of_body_armours";
        internal const string GenericBootsUrl = "/List_of_boots";
        internal const string GenericGlovesUrl = "/List_of_gloves";
        internal const string GenericHelmetsUrl = "/List_of_helmets";
        internal const string GenericShieldsUrl = "/List_of_shields";
        #endregion Armors

        #region Accessories
        internal const string GenericAmuletsUrl = "/List_of_amulets";
        internal const string GenericRingsUrl = "/List_of_rings";
        internal const string GenericQuiversUrl = "/List_of_quivers";
        internal const string GenericBeltsUrl = "/List_of_belts";
        #endregion Accessories

        #region Flasks
        internal const string GenericLifeFlasks = "/Life_Flasks";
        internal const string GenericManaFlasks = "/Mana_Flasks";
        internal const string GenericHybridFlasks = "/Hybrid_Flasks";
        internal const string GenericUtilityFlasks = "/Utility_Flasks";
        internal const string GenericCriticalUtiliyFlasks = "/Critical_Utility_Flasks";
        #endregion Flasks

        #region Uniques
        internal const string UniqueWeaponsUrl = "/List_of_unique_weapons";
        internal const string UniqueArmoursUrl = "/List_of_unique_armour";
        internal const string UniqueAccessoriesUrl = "/List_of_unique_accessories";
        internal const string UniqueFlasksUrl = "/List_of_unique_flasks";
        internal const string UniqueJewelsUrl = "/List_of_unique_jewels";
        internal const string UniqueMapsUrl = "/List_of_unique_maps";
        #endregion Uniques

        internal const string MapsUrl = "/Map";

        //yes, i could pass it through methods to stop persistance
        //but i did this for clarity
        internal ConcurrentBag<GenericWeapon> _genericWeapons;
        internal ConcurrentBag<GenericArmour> _genericArmours;
        internal ConcurrentBag<GenericAccessory> _genericAccessories;
        internal ConcurrentBag<GenericFlask> _genericFlasks;
        internal ConcurrentBag<UniqueWeapon> _uniqueWeapons;
        internal ConcurrentBag<UniqueArmour> _uniqueArmours;
        internal ConcurrentBag<UniqueAccessory> _uniqueAccessories;
        internal ConcurrentBag<UniqueFlask> _uniqueFlasks;
        internal ConcurrentBag<UniqueJewel> _uniqueJewels;
        internal ConcurrentBag<Map> _maps;
        internal ConcurrentBag<UniqueMap> _uniqueMaps;

        internal async Task Start()
        {

            Log("Path of Exile Data Scraper");
            _web = new HttpClient { BaseAddress = new Uri("http://pathofexile.gamepedia.com") };
            _parser = new HtmlParser();
            _stopwatch = new Stopwatch();

            _connection = new SqliteConnection(FlatFileDb);
            await _connection.OpenAsync();

            _stopwatch.Start();
            //await GetGenericWeapons();
            Log($"Getting generic weapons took {_stopwatch.Elapsed.TotalSeconds} seconds.");
            _stopwatch.Restart();

            //await GetGenericArmours();
            Log($"Getting generic armours took {_stopwatch.Elapsed.TotalSeconds} seconds.");
            _stopwatch.Restart();

            //await GetGenericAccessories();
            Log($"Getting generic accessories took {_stopwatch.Elapsed.TotalSeconds} seconds.");
            _stopwatch.Restart();

            //await GetGenericFlasks();
            Log($"Getting generic accessories took {_stopwatch.Elapsed.TotalSeconds} seconds.");
            _stopwatch.Restart();

            //await GetUniqueWeapons();
            Log($"Getting unique weapons took {_stopwatch.Elapsed.TotalSeconds} seconds.");
            _stopwatch.Restart();

            //await GetUniqueArmours();
            Log($"Getting unique armours took {_stopwatch.Elapsed.TotalSeconds} seconds.");
            _stopwatch.Restart();

            //await GetUniqueAccessories();
            Log($"Getting unique accessories took {_stopwatch.Elapsed.TotalSeconds} seconds.");
            _stopwatch.Restart();

            //await GetUniqueFlasks();
            Log($"Getting unique flasks took {_stopwatch.Elapsed.TotalSeconds} seconds.");
            _stopwatch.Restart();

            //await GetUniqueJewels();
            Log($"Getting unique jewels took {_stopwatch.Elapsed.TotalSeconds} seconds.");
            _stopwatch.Restart();

            //await GetMaps();
            Log($"Getting maps took {_stopwatch.Elapsed.TotalSeconds} seconds.");
            _stopwatch.Restart();

            await GetUniqueMaps();
            Log($"Getting unique maps took {_stopwatch.Elapsed.TotalSeconds} seconds.");
            _stopwatch.Restart();

            _connection.Close();

            Console.ReadKey();

        }

        internal async Task GetGenericWeapons()
        {

            await _connection.ExecuteAsync("CREATE TABLE 'GenericWeapons' ( " +
                "'Name' TEXT NOT NULL UNIQUE, " +
                "'LevelReq' INTEGER, " +
                "'Strength' INTEGER, " +
                "'Dexterity' INTEGER, " +
                "'Intelligence' INTEGER, " +
                "'Damage' TEXT, " +
                "'APS' TEXT, " +
                "'CritChance' TEXT, " +
                "'DPS' TEXT, " +
                "'Stats' TEXT, " +
                "'ImageUrl' TEXT, " +
                "'Type' TEXT, " +
                "PRIMARY KEY('Name') )");

            _genericWeapons = new ConcurrentBag<GenericWeapon>();


            var dom = await _parser.ParseAsync(await _web.GetStringAsync(GenericAxesUrl));
            await GetGenericAxesSwordsAsync(dom, "One Handed Axe", "one handed axes", 0);
            await GetGenericAxesSwordsAsync(dom, "Two Handed Axe", "two handed axes", 1);

            dom = await _parser.ParseAsync(await _web.GetStringAsync(GenericSwordsUrl));
            await GetGenericAxesSwordsAsync(dom, "One Handed Sword", "one handed swords", 0);
            await GetGenericBowsThrustingSwordsAsync(dom, "Thrusting One Handed Sword", "thrusting one handed swords", 1);
            await GetGenericAxesSwordsAsync(dom, "Two Handed Sword", "two handed swords", 2);

            dom = await _parser.ParseAsync(await _web.GetStringAsync(GenericBowsUrl));
            await GetGenericBowsThrustingSwordsAsync(dom, "Bow", "bows", 0);

            dom = await _parser.ParseAsync(await _web.GetStringAsync(GenericMacesUrl));
            await GetGenericMacesAsync(dom, "One Handed Mace", "one handed maces", 0);
            await GetGenericStavesSceptresAsync(dom, "Sceptre", "sceptres", 1);
            await GetGenericMacesAsync(dom, "Two Handed Mace", "two handed maces", 2);

            dom.Dispose();

            await GetGenericClawOrDaggerAsync(GenericClawsUrl, "Claw", "claws");
            await GetGenericClawOrDaggerAsync(GenericDaggersUrl, "Dagger", "daggers");
            await GetGenericFishingRodsAsync();
            await GetGenericWandsAsync();

            Log("Inserting generic weapons into database...");
            await _connection.InsertAsync(_genericWeapons);
            Log($"Finished inserting generic weapons into database.");

        }

        internal async Task GetGenericArmours()
        {

            //cant use unique because of fucking two toned boots
            await _connection.ExecuteAsync("CREATE TABLE 'GenericArmours' ( " +
                "'Name' TEXT NOT NULL, " +
                "'LevelReq' INTEGER, " +
                "'Strength' INTEGER, " +
                "'Dexterity' INTEGER, " +
                "'Intelligence' INTEGER, " +
                "'Armour' TEXT, " +
                "'Evasion' TEXT, " +
                "'EnergyShield' TEXT, " +
                "'BlockChance' TEXT, " +
                "'Stats' TEXT, " +
                "'ImageUrl' TEXT, " +
                "'Type' TEXT )");

            _genericArmours = new ConcurrentBag<GenericArmour>();

            await GetGenericArmoursAsync(GenericBodyArmoursUrl, "Body Armour", "body armours", false);
            await GetGenericArmoursAsync(GenericBodyArmoursUrl, "Boot", "boots", false);
            await GetGenericArmoursAsync(GenericBodyArmoursUrl, "Glove", "gloves", false);
            await GetGenericArmoursAsync(GenericBodyArmoursUrl, "Helmet", "helmets", false);
            await GetGenericArmoursAsync(GenericShieldsUrl, "Shield", "shields", true);

            Log("Inserting generic armours into database...");
            await _connection.InsertAsync(_genericArmours);
            Log($"Finished inserting generic armours into database.");

        }

        internal async Task GetGenericAccessories()
        {

            await _connection.ExecuteAsync("CREATE TABLE 'GenericAccessories' ( " +
                "'Name' TEXT NOT NULL, " +
                "'LevelReq' INTEGER, " +
                "'ImageUrl' TEXT, " +
                "'Stats' TEXT, " +
                "'IsCorrupted' INTEGER, " +
                "'Type' TEXT )");

            _genericAccessories = new ConcurrentBag<GenericAccessory>();

            await GetGenericAcessoriesAsync(GenericAmuletsUrl, "Amulet", "amulets");
            await GetGenericAcessoriesAsync(GenericRingsUrl, "Ring", "rings");
            await GetGenericAcessoriesAsync(GenericQuiversUrl, "Quiver", "quivers");
            await GetGenericAcessoriesAsync(GenericBeltsUrl, "Belt", "belts");

            Log("Inserting generic accessories into database...");
            await _connection.InsertAsync(_genericAccessories);
            Log($"Finished inserting generic accessories into database.");

        }

        internal async Task GetGenericFlasks()
        {

            await _connection.ExecuteAsync("CREATE TABLE 'GenericFlasks' ( " +
                "'Name' TEXT NOT NULL UNIQUE, " +
                "'LevelReq' INTEGER, " +
                "'Life' TEXT, " +
                "'Mana' TEXT, " +
                "'Duration' TEXT, " +
                "'Usage' TEXT, " +
                "'Capacity' TEXT, " +
                "'ImageUrl' TEXT, " +
                "'BuffEffects' TEXT, " +
                "'Stats' TEXT, " +
                "'Type' TEXT, " +
                "PRIMARY KEY('Name') )");

            _genericFlasks = new ConcurrentBag<GenericFlask>();

            await GetGenericLifeManaFlasks(GenericLifeFlasks, "Life Flask", "life flasks", false);
            await GetGenericLifeManaFlasks(GenericManaFlasks, "Mana Flask", "mana flasks", true);
            await GetGenericUtilityFlasks(GenericUtilityFlasks);
            await GetGenericUtilityFlasks(GenericCriticalUtiliyFlasks);

            Log("Inserting generic flasks into database...");
            await _connection.InsertAsync(_genericFlasks);
            Log($"Finished inserting generic flasks into database.");

        }

        internal async Task GetUniqueWeapons()
        {

            await _connection.ExecuteAsync("CREATE TABLE 'UniqueWeapons'(" +
                "'Name' TEXT NOT NULL UNIQUE, " +
                "'LevelReq' INTEGER, " +
                "'Strength' INTEGER, " +
                "'Dexterity' INTEGER, " +
                "'Intelligence' INTEGER, " +
                "'Damage' TEXT, " +
                "'APS' TEXT, " +
                "'CritChance' TEXT, " +
                "'PDPS' TEXT, " +
                "'EDPS' TEXT, " +
                "'DPS' TEXT, " +
                "'Stats' TEXT, " +
                "'ImageUrl' TEXT, " +
                "'Type' TEXT, " +
                "PRIMARY KEY('Name') )"); ;

            _uniqueWeapons = new ConcurrentBag<UniqueWeapon>();

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(UniqueWeaponsUrl));
            var tables = dom.GetElementById("mw-content-text").GetElementsByTagName("tbody");

            await GetUniqueStrDexWeaponsAsync(tables[0], "One Handed Axe", "one handed axes");
            await GetUniqueStrDexWeaponsAsync(tables[1], "Two Handed Axe", "two handed axes");
            await GetUniqueStrDexWeaponsAsync(tables[5], "Fishing Rod", "fishing rods");
            await GetUniqueStrDexWeaponsAsync(tables[9], "One Handed Sword", "one handed swords");
            await GetUniqueStrDexWeaponsAsync(tables[11], "Two Handed Sword", "two handed swords");

            await GetUniqueDexWeaponsAsync(tables[2], "Bow", "bows");
            await GetUniqueDexWeaponsAsync(tables[10], "Thrusting One Handed Sword", "thrusting one handed swords");

            await GetUniqueDexIntelWeaponsAsync(tables[3], "Claw", "claws");
            await GetUniqueDexIntelWeaponsAsync(tables[4], "Dagger", "daggers");

            await GetUniqueStrWeaponsAsync(tables[6], "One Handed Mace", "one handed maces");
            await GetUniqueStrWeaponsAsync(tables[8], "Two Handed Mace", "two handed maces");

            await GetUniqueStrIntelWeaponsAsync(tables[7], "Sceptre", "sceptres");
            await GetUniqueStrIntelWeaponsAsync(tables[12], "Staff", "staves");

            await GetUniqueIntelWeaponsAsync(tables[13], "Wand", "wands");

            Log("Inserting unique weapons into database...");
            await _connection.InsertAsync(_uniqueWeapons);
            Log($"Finished inserting unique weapons into database.");

        }

        internal async Task GetUniqueArmours()
        {

            await _connection.ExecuteAsync("CREATE TABLE 'UniqueArmours' ( " +
                "'Name' TEXT NOT NULL, " +
                "'LevelReq' INTEGER, " +
                "'Strength' INTEGER, " +
                "'Dexterity' INTEGER, " +
                "'Intelligence' INTEGER, " +
                "'Armour' TEXT, " +
                "'Evasion' TEXT, " +
                "'EnergyShield' TEXT, " +
                "'BlockChance' TEXT, " +
                "'Stats' TEXT, " +
                "'ImageUrl' TEXT, " +
                "'Type' TEXT )");

            _uniqueArmours = new ConcurrentBag<UniqueArmour>();

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(UniqueArmoursUrl));
            var tables = dom.GetElementById("mw-content-text").GetElementsByTagName("tbody").Where(element => element.Children.Length > 1).ToArray(); //make sure it doesnt just contain the label
            var typesOfArmourSingular = new string[] { "Body Armour", "Boot", "Glove", "Helmet" };
            var typesOfArmourPlural = new string[] { "body armours", "boots", "gloves", "helmets" };
            var singleAttributeTableGroups = new int[] { 0, 7, 13, 20 }; //represents where each of the singlar attribute armor tables start
            var doubleAttributeTableGroups = new int[] { 3, 10, 16, 23 };

            for (int index = 0; index < 4; index++)
            {

                var i = singleAttributeTableGroups[index];
                var s = typesOfArmourSingular[index];
                var p = typesOfArmourPlural[index];

                await GetSingleAttributeArmoursAsync(tables[i], "Strength", "Armour", s, p);
                await GetSingleAttributeArmoursAsync(tables[i + 1], "Dexterity", "Evasion", s, p);
                await GetSingleAttributeArmoursAsync(tables[i + 2], "Intelligence", "EnergyShield", s, p);

            }

            for (int index = 0; index < 4; index++)
            {

                var i = doubleAttributeTableGroups[index];
                var s = typesOfArmourSingular[index];
                var p = typesOfArmourPlural[index];

                await GetDoubleAttributeArmoursAsync(tables[i], "Strength", "Dexterity", "Armour", "Evasion", s, p);
                await GetDoubleAttributeArmoursAsync(tables[i], "Strength", "Intelligence", "Armour", "EnergyShield", s, p);
                await GetDoubleAttributeArmoursAsync(tables[i], "Dexterity", "Intelligence", "Evasion", "EnergyShield", s, p);

            }

            await GetTripleAttributeArmoursAsync(tables[6], typesOfArmourSingular[0], typesOfArmourPlural[0]);

            Log("Inserting unique armours into database...");
            await _connection.InsertAsync(_uniqueArmours);
            Log($"Finished inserting unique armours into database.");

        }

        internal async Task GetUniqueAccessories()
        {

            await _connection.ExecuteAsync("CREATE TABLE 'UniqueAccessories' ( " +
                "'Name' TEXT NOT NULL, " +
                "'LevelReq' INTEGER, " +
                "'Stats' TEXT, " +
                "'IsCorrupted' INTEGER, " +
                "'ImageUrl' TEXT, " +
                "'Type' TEXT )");

            _uniqueAccessories = new ConcurrentBag<UniqueAccessory>();

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(UniqueAccessoriesUrl));
            var tables = dom.GetElementById("mw-content-text").GetElementsByTagName("tbody");

            await GetUniqueAccessoriesAsync(tables[0], "Amulet", "amulets");
            await GetUniqueAccessoriesAsync(tables[1], "Belt", "belts");
            await GetUniqueAccessoriesAsync(tables[2], "Ring", "rings");
            await GetUniqueAccessoriesAsync(tables[3], "Quiver", "quivers");
            await GetUniqueAccessoriesAsync(tables[4], "Quiver", "quivers"); //i have no clue why there's a random quiver by itself here

            Log("Inserting unique accessories into database...");
            await _connection.InsertAsync(_uniqueAccessories);
            Log($"Finished inserting unique accessories into database.");

        }

        internal async Task GetUniqueFlasks()
        {

            await _connection.ExecuteAsync("CREATE TABLE 'UniqueFlasks' ( " +
                "'Name' TEXT NOT NULL, " +
                "'LevelReq' TEXT, " +
                "'Life' TEXT, " +
                "'Mana' TEXT, " +
                "'Duration' TEXT, " +
                "'Usage' TEXT, " +
                "'Capacity' TEXT, " +
                "'ImageUrl' TEXT, " +
                "'BuffEffects' TEXT, " +
                "'Stats' TEXT, " +
                "'Type' TEXT )");

            _uniqueFlasks = new ConcurrentBag<UniqueFlask>();

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(UniqueFlasksUrl));
            var tables = dom.GetElementById("mw-content-text").GetElementsByTagName("tbody");

            //possible way to shorten: use counters
            await GetUniqueLifeManaFlasksAsync(tables[0], "Life Flask", "life flasks", false);
            await GetUniqueLifeManaFlasksAsync(tables[1], "Mana Flask", "mana flasks", true);
            await GetUniqueHybridFlasksAsync(tables[2]);
            await GetUniqueUtilityFlasksAsync(tables[3]);

            Log("Inserting unique flasks into database...");
            await _connection.InsertAsync(_uniqueFlasks);
            Log($"Finished inserting unique flasks into database.");

        }

        internal async Task GetUniqueJewels()
        {

            await _connection.ExecuteAsync("CREATE TABLE 'UniqueJewels' ( " +
                "'Name' TEXT NOT NULL, " +
                "'Limit' TEXT, " +
                "'Radius' TEXT, " +
                "'Stats' TEXT, " +
                "'ImageUrl' TEXT, " +
                "'IsCorrupted' INTEGER, " +
                "'ObtainMethod' TEXT ) ");

            _uniqueJewels = new ConcurrentBag<UniqueJewel>();

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(UniqueJewelsUrl));
            var tables = dom.GetElementById("mw-content-text").GetElementsByTagName("tbody");

            await GetUniqueJewelsAsync(tables[0], "Drop");
            await GetUniqueJewelsAsync(tables[1], "Corruption");
            await GetUniqueJewelsAsync(tables[2], "Labryninth");
            await GetUniqueJewelsAsync(tables[3], "Beta");

            Log("Inserting unique jewels into database...");
            await _connection.InsertAsync(_uniqueJewels);
            Log($"Finished unique jewels into database.");

        }

        internal async Task GetMaps()
        {

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
                "PRIMARY KEY('Name') ) ");

            _maps = new ConcurrentBag<Map>();

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(MapsUrl));
            var table = dom.GetElementById("mw-content-text").GetElementsByClassName("wikitable sortable")[1].GetElementsByTagName("tbody").FirstOrDefault();
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

            Log($"Getting maps...");

            Parallel.ForEach(maps, item =>
            {

                var statLines = item.GetElementsByTagName("td");
                var formattedBoss = statLines[7].InnerHtml.Replace("<br>", "\\n");
                formattedBoss = Regex.Replace(formattedBoss, "<.*?>", string.Empty);
                var test = statLines.FirstOrDefault();

                var map = new Map
                {

                    Name = statLines.FirstOrDefault().GetElementsByTagName("a").ElementAtOrDefault(1)?.TextContent ?? statLines.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    MapLevel = int.Parse(statLines[1].TextContent.Trim()),
                    Tier = statLines[2].TextContent.Trim(),
                    Unique = statLines[3].GetElementsByTagName("img").FirstOrDefault().GetAttribute("alt") == "yes",
                    LayoutType = char.TryParse(statLines.ElementAtOrDefault(4)?.TextContent.Trim() ?? "blah", out char c) ? layoutTypes[c] : "N/A", //some are not present
                    BossDifficulty = int.TryParse(statLines.ElementAtOrDefault(5)?.TextContent.Trim() ?? "blah", out int i) ? bossDifficulties[i] : "N/A", //some are not present
                    LayoutSet = statLines[6].TextContent.Trim(),
                    UniqueBoss = formattedBoss,
                    NumberOfUniqueBosses = int.TryParse(statLines[8].TextContent.Trim(), out int number) ? number : 0,
                    SextantCoverage = int.TryParse(statLines.ElementAtOrDefault(9)?.TextContent.Trim() ?? "blah", out number) ? number : 0,
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault()?.GetAttribute("src") ?? "N/A",

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
                "PRIMARY KEY('Name') )");

            _uniqueMaps = new ConcurrentBag<UniqueMap>();

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(UniqueMapsUrl));
            var table = dom.GetElementById("mw-content-text").GetElementsByTagName("tbody").FirstOrDefault();
            var maps = table.GetElementsByTagName("tr").Where(element => !element.TextContent.Contains("Stats"));

            Parallel.ForEach(maps, item =>
            {

                var statLines = item.GetElementsByTagName("td");
                var formattedStats = statLines[2].InnerHtml.Replace("<br>", "\\n");

                var map = new UniqueMap
                {

                    Name = statLines.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    MapLevel = statLines[1].TextContent,
                    Stats = Regex.Replace(formattedStats, "<.*?>", string.Empty),
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),

                };

                _uniqueMaps.Add(map);

            });

            Log("Inserting unique maps...");
            await _connection.InsertAsync(_uniqueMaps);
            Log("Finished inserting unique maps.");

        }

        #region Generics
        internal Task GetGenericAxesSwordsAsync(IHtmlDocument dom, string upperCaseSingular, string lowerCasePlural, int tableToUse)
        {

            var mainDom = dom.GetElementById("mw-content-text");
            var tables = mainDom.GetElementsByTagName("table");

            var oneHandedAxes = tables.ElementAtOrDefault(tableToUse).GetElementsByTagName("tbody").FirstOrDefault().Children.Where(element => !element.TextContent.Contains("DPSStats")); //.Where(e => e.TextContent != "ItemDamageAPSCritDPSStats");

            Log($"Getting {lowerCasePlural}...");

            Parallel.ForEach(oneHandedAxes, axe =>
            {

                var statLines = axe.GetElementsByTagName("td");

                var weapon = new GenericWeapon
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Strength = int.Parse(statLines[2].TextContent),
                    Dexterity = int.Parse(statLines[3].TextContent),
                    Damage = statLines[4].TextContent,
                    APS = statLines[5].TextContent,
                    CritChance = statLines[6].TextContent, //i have no idea if there will always be 2 trailing digits after the .
                    DPS = statLines[7].TextContent,
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = statLines.ElementAtOrDefault(8)?.TextContent ?? "N/A",
                    Type = upperCaseSingular,

                };

                _genericWeapons.Add(weapon);

            });

            Log($"Finished getting {lowerCasePlural}.");

            return Task.CompletedTask;

        }

        internal Task GetGenericBowsThrustingSwordsAsync(IHtmlDocument dom, string upperCaseSingular, string lowerCasePlural, int tableToUse)
        {

            Log($"Getting {lowerCasePlural}...");

            var mainDom = dom.GetElementById("mw-content-text");
            var weapons = mainDom.GetElementsByTagName("tbody").ElementAtOrDefault(tableToUse).Children.Where(element => !element.TextContent.Contains("Stats"));

            Parallel.ForEach(weapons, item =>
            {

                var statLines = item.GetElementsByTagName("td");

                var weapon = new GenericWeapon
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Dexterity = int.Parse(statLines[2].TextContent),
                    Damage = statLines[3].TextContent,
                    APS = statLines[4].TextContent,
                    CritChance = statLines[5].TextContent, //i have no idea if there will always be 2 trailing digits after the .
                    DPS = statLines.ElementAtOrDefault(8)?.TextContent ?? statLines.ElementAtOrDefault(6).TextContent,
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = statLines.ElementAtOrDefault(9)?.TextContent ?? statLines.ElementAtOrDefault(7)?.TextContent,
                    Type = upperCaseSingular,

                };

                _genericWeapons.Add(weapon);

            });

            Log($"Finished getting {lowerCasePlural}.");

            return Task.CompletedTask;

        }

        internal async Task GetGenericFishingRodsAsync()
        {

            Log("Getting fishing rods...");

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(GenericFishingRodsUrl));
            var mainDom = dom.GetElementById("mw-content-text");
            var weapons = mainDom.GetElementsByTagName("tbody").FirstOrDefault().Children.Where(element => !element.TextContent.Contains("CritDPS"));

            Parallel.ForEach(weapons, claw =>
            {

                var statLines = claw.GetElementsByTagName("td");

                var weapon = new GenericWeapon
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Damage = statLines[2].TextContent,
                    APS = statLines[3].TextContent,
                    CritChance = statLines[4].TextContent, //i have no idea if there will always be 2 trailing digits after the .
                    DPS = statLines[5].TextContent,
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Type = "Fishing Rod",

                };

                _genericWeapons.Add(weapon);

            });

            Log("Finished getting fishing rods.");

        }

        internal Task GetGenericStavesSceptresAsync(IHtmlDocument dom, string upperCaseSingular, string lowerCasePlural, int tableToUse)
        {

            Log($"Getting {lowerCasePlural}...");

            var mainDom = dom.GetElementById("mw-content-text");
            var weapons = mainDom.GetElementsByTagName("tbody").ElementAtOrDefault(tableToUse).Children.Where(element => !element.TextContent.Contains("Stats"));

            Parallel.ForEach(weapons, item =>
            {

                var statLines = item.GetElementsByTagName("td");

                var weapon = new GenericWeapon
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Strength = int.Parse(statLines[2].TextContent),
                    Intelligence = int.Parse(statLines[3].TextContent),
                    Damage = statLines[4].TextContent,
                    APS = statLines[5].TextContent,
                    CritChance = statLines[6].TextContent, //i have no idea if there will always be 2 trailing digits after the .
                    DPS = statLines[7].TextContent,
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = statLines[8].TextContent,
                    Type = upperCaseSingular,

                };

                _genericWeapons.Add(weapon);

            });

            Log($"Finished getting {lowerCasePlural}.");

            return Task.CompletedTask;

        }

        internal Task GetGenericMacesAsync(IHtmlDocument dom, string upperCaseSingular, string lowerCasePlural, int tableToUse)
        {

            Log($"Getting {lowerCasePlural}...");

            var mainDom = dom.GetElementById("mw-content-text");
            var weapons = mainDom.GetElementsByTagName("tbody").ElementAtOrDefault(tableToUse).Children.Where(element => !element.TextContent.Contains("Stats"));

            Parallel.ForEach(weapons, item =>
            {

                var statLines = item.GetElementsByTagName("td");

                var weapon = new GenericWeapon
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Strength = int.Parse(statLines[2].TextContent),
                    Damage = statLines[3].TextContent,
                    APS = statLines[4].TextContent,
                    CritChance = statLines[5].TextContent, //i have no idea if there will always be 2 trailing digits after the .
                    DPS = statLines[6].TextContent,
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = statLines[7].TextContent,
                    Type = upperCaseSingular,

                };

                _genericWeapons.Add(weapon);

            });

            Log($"Finished getting {lowerCasePlural}.");

            return Task.CompletedTask;

        }

        internal async Task GetGenericClawOrDaggerAsync(string url, string upperCaseSingular, string lowerCasePlural)
        {

            Log($"Getting {lowerCasePlural}...");

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(url));
            var mainDom = dom.GetElementById("mw-content-text");
            var weapons = mainDom.GetElementsByTagName("tbody").FirstOrDefault().Children.Where(element => !element.TextContent.Contains("DPSStats"));

            Parallel.ForEach(weapons, claw =>
            {

                var statLines = claw.GetElementsByTagName("td");

                var weapon = new GenericWeapon
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Dexterity = int.Parse(statLines[2].TextContent),
                    Intelligence = int.Parse(statLines[3].TextContent),
                    Damage = statLines[4].TextContent,
                    APS = statLines[5].TextContent,
                    CritChance = statLines[6].TextContent, //i have no idea if there will always be 2 trailing digits after the .
                    DPS = statLines[7].TextContent,
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = Regex.Replace(statLines[8].InnerHtml.Replace("<br>", "\\n"), "<.*?>", string.Empty),
                    Type = $"{upperCaseSingular}",

                };

                _genericWeapons.Add(weapon);

            });

            Log($"Finished getting {lowerCasePlural}.");

        }

        internal async Task GetGenericWandsAsync()
        {

            Log("Getting wands...");

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(GenericWandsUrl));
            var mainDom = dom.GetElementById("mw-content-text");
            var weapons = mainDom.GetElementsByTagName("tbody");

            var wands = weapons.FirstOrDefault().Children.Where(element => !element.TextContent.Contains("DPSStats"));

            Parallel.ForEach(wands, wand =>
            {

                var statLines = wand.GetElementsByTagName("td");

                var weapon = new GenericWeapon
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Intelligence = int.Parse(statLines[2].TextContent),
                    Damage = statLines[3].TextContent,
                    APS = statLines[4].TextContent,
                    CritChance = statLines[5].TextContent, //i have no idea if there will always be 2 trailing digits after the .
                    DPS = statLines[6].TextContent,
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = statLines.ElementAtOrDefault(7)?.TextContent ?? "N/A",
                    Type = "Wand",

                };

                _genericWeapons.Add(weapon);

            });

            Log("Finished getting wands.");

        }

        internal async Task GetGenericArmoursAsync(string url, string upperCaseSingular, string lowerCasePlural, bool isShield)
        {

            Log($"Getting armor {lowerCasePlural}...");

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(url));
            var mainDom = dom.GetElementById("mw-content-text");
            var armours = mainDom.GetElementsByTagName("tbody");

            var strengthArmours = armours.FirstOrDefault().Children.Where(element => !element.TextContent.Contains("Stats"));

            Parallel.ForEach(strengthArmours, armor =>
            {

                var statLines = armor.GetElementsByTagName("td");

                var armour = new GenericArmour
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Strength = int.Parse(statLines[2].TextContent),
                    Armour = statLines[3].TextContent,
                    BlockChance = isShield ? statLines[4].TextContent : "N/A",
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = isShield ? statLines.ElementAtOrDefault(5).TextContent : statLines.ElementAtOrDefault(4).TextContent,
                    Type = $"{upperCaseSingular}",

                };

                _genericArmours.Add(armour);

            });

            Log($"Finished getting armor {lowerCasePlural}.");
            Log($"Getting evasion {lowerCasePlural}...");

            var evasionArmours = armours.ElementAtOrDefault(1).Children.Where(element => !element.TextContent.Contains("Stats"));

            Parallel.ForEach(evasionArmours, armor =>
            {

                var statLines = armor.GetElementsByTagName("td");

                var armour = new GenericArmour
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Dexterity = int.Parse(statLines[2].TextContent),
                    Evasion = statLines[3].TextContent,
                    BlockChance = isShield ? statLines[4].TextContent : "N/A",
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = isShield ? statLines.ElementAtOrDefault(5).TextContent : statLines.ElementAtOrDefault(4).TextContent,
                    Type = $"{upperCaseSingular}",

                };

                _genericArmours.Add(armour);

            });

            Log($"Finished getting evasion {lowerCasePlural}.");
            Log($"Getting energy shield {lowerCasePlural}...");

            var energyShieldArmours = armours.ElementAtOrDefault(2).Children.Where(element => !element.TextContent.Contains("Stats"));

            Parallel.ForEach(energyShieldArmours, armor =>
            {

                var statLines = armor.GetElementsByTagName("td");

                var armour = new GenericArmour
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Intelligence = int.Parse(statLines[2].TextContent),
                    EnergyShield = statLines[3].TextContent,
                    BlockChance = isShield ? statLines[4].TextContent : "N/A",
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = isShield ? statLines.ElementAtOrDefault(5).TextContent : statLines.ElementAtOrDefault(4).TextContent,
                    Type = $"{upperCaseSingular}",

                };

                _genericArmours.Add(armour);

            });

            Log($"Finished getting energy shield {lowerCasePlural}.");
            Log($"Getting armour/evasion {lowerCasePlural}...");

            var armourEvasionArmours = armours.ElementAtOrDefault(3).Children.Where(element => !element.TextContent.Contains("Stats"));

            Parallel.ForEach(armourEvasionArmours, armor =>
            {

                var statLines = armor.GetElementsByTagName("td");

                var armour = new GenericArmour
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Strength = int.Parse(statLines[2].TextContent),
                    Dexterity = int.Parse(statLines[3].TextContent),
                    Armour = statLines[4].TextContent,
                    Evasion = statLines[5].TextContent,
                    BlockChance = isShield ? statLines[6].TextContent : "N/A",
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = isShield ? statLines.ElementAtOrDefault(7).TextContent : statLines.ElementAtOrDefault(6).TextContent,
                    Type = $"{upperCaseSingular}",

                };

                _genericArmours.Add(armour);

            });

            Log($"Finished getting armour/evasion {lowerCasePlural}.");
            Log($"Getting armour/energy shield {lowerCasePlural}...");

            var armourEnergyArmours = armours.ElementAtOrDefault(4).Children.Where(element => !element.TextContent.Contains("Stats"));

            Parallel.ForEach(armourEnergyArmours, armor =>
            {

                var statLines = armor.GetElementsByTagName("td");

                var armour = new GenericArmour
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Strength = int.Parse(statLines[2].TextContent),
                    Intelligence = int.Parse(statLines[3].TextContent),
                    Armour = statLines[4].TextContent,
                    EnergyShield = statLines[5].TextContent,
                    BlockChance = isShield ? statLines[6].TextContent : "N/A",
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = isShield ? statLines.ElementAtOrDefault(7).TextContent : statLines.ElementAtOrDefault(6).TextContent,
                    Type = $"{upperCaseSingular}",

                };

                _genericArmours.Add(armour);

            });

            Log($"Finished getting armour/energy shield {lowerCasePlural}.");
            Log($"Getting evasion/energy shield {lowerCasePlural}...");

            var evasionEnergyArmours = armours.ElementAtOrDefault(5).Children.Where(element => !element.TextContent.Contains("Stats"));

            Parallel.ForEach(evasionEnergyArmours, armor =>
            {

                var statLines = armor.GetElementsByTagName("td");

                var armour = new GenericArmour
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Dexterity = int.Parse(statLines[2].TextContent),
                    Intelligence = int.Parse(statLines[3].TextContent),
                    Evasion = statLines[4].TextContent,
                    EnergyShield = statLines[5].TextContent,
                    BlockChance = isShield ? statLines[6].TextContent : "N/A",
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = isShield ? statLines.ElementAtOrDefault(7).TextContent : statLines.ElementAtOrDefault(6).TextContent,
                    Type = $"{upperCaseSingular}",

                };

                _genericArmours.Add(armour);

            });

            Log($"Finished getting evasion/energy shield {lowerCasePlural}.");

        }

        internal async Task GetGenericAcessoriesAsync(string url, string upperCaseSingular, string lowerCasePlural)
        {

            Log($"Getting {lowerCasePlural}...");

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(url));
            var mainDom = dom.GetElementById("mw-content-text");
            var accessoriesTable = mainDom.GetElementsByTagName("tbody");

            var accessories = accessoriesTable.SelectMany(element => element.GetElementsByTagName("tr")).Where(element => !element.TextContent.Contains("Stats"));

            Parallel.ForEach(accessories, accessory =>
            {

                var statLines = accessory.GetElementsByTagName("td");
                var stats = statLines[2].TextContent;

                var item = new GenericAccessory
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = stats.Replace("Corrupted", string.Empty),
                    IsCorrupted = stats.Contains("Corrupted"),
                    Type = $"{upperCaseSingular}",

                };

                _genericAccessories.Add(item);

            });

            Log($"Finished getting {lowerCasePlural}.");

        }

        internal async Task GetGenericLifeManaFlasks(string url, string upperCaseSingular, string lowerCasePlural, bool IsManaFlasks)
        {

            Log($"Getting {lowerCasePlural}...");

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(url));
            var mainDom = dom.GetElementById("mw-content-text");
            var flasksTable = mainDom.GetElementsByTagName("tbody").FirstOrDefault();

            var flasks = flasksTable.GetElementsByTagName("tr").Where(element => !element.TextContent.Contains("Capacity"));

            Parallel.ForEach(flasks, flask =>
            {

                var statLines = flask.GetElementsByTagName("td");

                var item = new GenericFlask
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Life = IsManaFlasks ? "0" : statLines[2].TextContent,
                    Mana = IsManaFlasks ? statLines[2].TextContent : "0",
                    Duration = statLines[3].TextContent,
                    Usage = statLines[4].TextContent,
                    Capacity = statLines[5].TextContent,
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Type = $"{upperCaseSingular}",

                };

                _genericFlasks.Add(item);

            });

            Log($"Finished getting {lowerCasePlural}.");

        }

        internal async Task GetGenericHybridFlasks()
        {

            Log($"Getting hybrid flasks...");

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(GenericUtilityFlasks));
            var mainDom = dom.GetElementById("mw-content-text");
            var flasksTable = mainDom.GetElementsByTagName("tbody").FirstOrDefault();

            var flasks = flasksTable.GetElementsByTagName("tr").Where(element => !element.TextContent.Contains("Capacity"));

            Parallel.ForEach(flasks, flask =>
            {

                var statLines = flask.GetElementsByTagName("td");

                var item = new GenericFlask
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Life = statLines[2].TextContent,
                    Mana = statLines[3].TextContent,
                    Duration = statLines[4].TextContent,
                    Usage = statLines[5].TextContent,
                    Capacity = statLines[6].TextContent,
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Type = $"Hybrid Flask",

                };

                _genericFlasks.Add(item);

            });

            Log($"Finished getting hybrid flasks.");

        }

        internal async Task GetGenericUtilityFlasks(string url)
        {

            Log($"Getting utility flasks...");

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(url));
            var mainDom = dom.GetElementById("mw-content-text");
            var flasksTable = mainDom.GetElementsByTagName("tbody").FirstOrDefault();

            var flasks = flasksTable.GetElementsByTagName("tr").Where(element => !element.TextContent.Contains("Capacity"));

            Parallel.ForEach(flasks, flask =>
            {

                var statLines = flask.GetElementsByTagName("td");

                var item = new GenericFlask
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Duration = statLines[2].TextContent,
                    Usage = statLines[3].TextContent,
                    Capacity = statLines[4].TextContent,
                    BuffEffects = Regex.Replace(statLines[5].InnerHtml.Replace("<br>", "\\n"), "<.*?>", string.Empty),
                    Stats = statLines[6].TextContent,
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Type = $"Utility Flask",

                };

                _genericFlasks.Add(item);

            });

            Log($"Finished getting utility flasks.");

        }
        #endregion Generics

        #region Uniques
        internal Task GetUniqueStrDexWeaponsAsync(IElement table, string upperCaseSingular, string lowerCasePlural)
        {

            Log($"Getting {lowerCasePlural}...");

            var weapons = table.Children.Where(element => !element.TextContent.Contains("DPSStats"));

            Parallel.ForEach(weapons, item =>
            {

                var statLines = item.GetElementsByTagName("td");
                var statElement = statLines.ElementAtOrDefault(10) ?? statLines.ElementAtOrDefault(9); //regular weapon ?? fishing rod
                var formattedStats = statElement.InnerHtml.Replace("<br>", "\\n").Replace("<span class=\"item-stat-separator -unique\">", "\\n");

                var weapon = new UniqueWeapon
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Strength = int.Parse(statLines[2].TextContent),
                    Dexterity = int.Parse(statLines[3].TextContent),
                    Damage = Regex.Replace(statLines[4].InnerHtml.Replace("<br>", "\\n"), "<.*?>", string.Empty),
                    APS = statLines[5].TextContent,
                    CritChance = statLines[6].TextContent, //i have no idea if there will always be 2 trailing digits after the .
                    PDPS = statLines[7].TextContent,
                    EDPS = statLines[8].TextContent,
                    DPS = statLines.ElementAtOrDefault(9)?.TextContent ?? "N/A",
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = Regex.Replace(formattedStats, "<.*?>", string.Empty),
                    Type = upperCaseSingular,

                };

                _uniqueWeapons.Add(weapon);

            });

            Log($"Finished getting {lowerCasePlural}.");

            return Task.CompletedTask;

        }

        internal Task GetUniqueDexWeaponsAsync(IElement table, string upperCaseSingular, string lowerCasePlural)
        {

            Log($"Getting {lowerCasePlural}...");

            var weapons = table.Children.Where(element => !element.TextContent.Contains("DPSStats"));

            Parallel.ForEach(weapons, item =>
            {

                var statLines = item.GetElementsByTagName("td");
                var statElement = statLines[9];
                var formattedStats = statElement.InnerHtml.Replace("<br>", "\\n").Replace("<span class=\"item-stat-separator -unique\">", "\\n");

                var weapon = new UniqueWeapon
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Dexterity = int.Parse(statLines[2].TextContent),
                    Damage = Regex.Replace(statLines[3].InnerHtml.Replace("<br>", "\\n"), "<.*?>", string.Empty),
                    APS = statLines[4].TextContent,
                    CritChance = statLines[5].TextContent, //i have no idea if there will always be 2 trailing digits after the .
                    PDPS = statLines[6].TextContent,
                    EDPS = statLines[7].TextContent,
                    DPS = statLines.ElementAtOrDefault(8)?.TextContent ?? "N/A",
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = Regex.Replace(formattedStats, "<.*?>", string.Empty), //regular weapon ?? fishing rod
                    Type = upperCaseSingular,

                };

                _uniqueWeapons.Add(weapon);

            });

            Log($"Finished getting {lowerCasePlural}.");

            return Task.CompletedTask;

        }

        internal Task GetUniqueDexIntelWeaponsAsync(IElement table, string upperCaseSingular, string lowerCasePlural)
        {

            Log($"Getting {lowerCasePlural}...");

            var weapons = table.Children.Where(element => !element.TextContent.Contains("DPSStats"));

            Parallel.ForEach(weapons, item =>
            {

                var statLines = item.GetElementsByTagName("td");
                var statElement = statLines[10];
                var formattedStats = statElement.InnerHtml.Replace("<br>", "\\n").Replace("<span class=\"item-stat-separator -unique\">", "\\n");

                var weapon = new UniqueWeapon
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Dexterity = int.Parse(statLines[2].TextContent),
                    Intelligence = int.Parse(statLines[3].TextContent),
                    Damage = Regex.Replace(statLines[4].InnerHtml.Replace("<br>", "\\n"), "<.*?>", string.Empty),
                    APS = statLines[5].TextContent,
                    CritChance = statLines[6].TextContent, //i have no idea if there will always be 2 trailing digits after the .
                    PDPS = statLines[7].TextContent,
                    EDPS = statLines[8].TextContent,
                    DPS = statLines.ElementAtOrDefault(9)?.TextContent ?? "N/A",
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = Regex.Replace(formattedStats, "<.*?>", string.Empty),
                    Type = upperCaseSingular,

                };

                _uniqueWeapons.Add(weapon);

            });

            Log($"Finished getting {lowerCasePlural}.");

            return Task.CompletedTask;

        }

        internal Task GetUniqueStrWeaponsAsync(IElement table, string upperCaseSingular, string lowerCasePlural)
        {

            Log($"Getting {lowerCasePlural}...");

            var weapons = table.Children.Where(element => !element.TextContent.Contains("DPSStats"));

            Parallel.ForEach(weapons, item =>
            {

                var statLines = item.GetElementsByTagName("td");
                var statElement = statLines[9];
                var formattedStats = statElement.InnerHtml.Replace("<br>", "\\n").Replace("<span class=\"item-stat-separator -unique\">", "\\n");

                var weapon = new UniqueWeapon
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Strength = int.Parse(statLines[2].TextContent),
                    Damage = Regex.Replace(statLines[3].InnerHtml.Replace("<br>", "\\n"), "<.*?>", string.Empty),
                    APS = statLines[4].TextContent,
                    CritChance = statLines[5].TextContent, //i have no idea if there will always be 2 trailing digits after the .
                    PDPS = statLines[6].TextContent,
                    EDPS = statLines[7].TextContent,
                    DPS = statLines.ElementAtOrDefault(8)?.TextContent ?? "N/A",
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = Regex.Replace(formattedStats, "<.*?>", string.Empty), //regular weapon ?? fishing rod
                    Type = upperCaseSingular,

                };

                _uniqueWeapons.Add(weapon);

            });

            Log($"Finished getting {lowerCasePlural}.");

            return Task.CompletedTask;

        }

        internal Task GetUniqueStrIntelWeaponsAsync(IElement table, string upperCaseSingular, string lowerCasePlural)
        {

            Log($"Getting {lowerCasePlural}...");

            var weapons = table.Children.Where(element => !element.TextContent.Contains("DPSStats"));

            Parallel.ForEach(weapons, item =>
            {

                var statLines = item.GetElementsByTagName("td");
                var statElement = statLines[10];
                var formattedStats = statElement.InnerHtml.Replace("<br>", "\\n").Replace("<span class=\"item-stat-separator -unique\">", "\\n");

                var weapon = new UniqueWeapon
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Strength = int.Parse(statLines[2].TextContent),
                    Intelligence = int.Parse(statLines[3].TextContent),
                    Damage = Regex.Replace(statLines[4].InnerHtml.Replace("<br>", "\\n"), "<.*?>", string.Empty),
                    APS = statLines[5].TextContent,
                    CritChance = statLines[6].TextContent, //i have no idea if there will always be 2 trailing digits after the .
                    PDPS = statLines[7].TextContent,
                    EDPS = statLines[8].TextContent,
                    DPS = statLines.ElementAtOrDefault(9)?.TextContent ?? "N/A",
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = Regex.Replace(formattedStats, "<.*?>", string.Empty),
                    Type = upperCaseSingular,

                };

                _uniqueWeapons.Add(weapon);

            });

            Log($"Finished getting {lowerCasePlural}.");

            return Task.CompletedTask;

        }

        internal Task GetUniqueIntelWeaponsAsync(IElement table, string upperCaseSingular, string lowerCasePlural)
        {

            Log($"Getting {lowerCasePlural}...");

            var weapons = table.Children.Where(element => !element.TextContent.Contains("DPSStats"));

            Parallel.ForEach(weapons, item =>
            {

                var statLines = item.GetElementsByTagName("td");
                var statElement = statLines[9];
                var formattedStats = statElement.InnerHtml.Replace("<br>", "\\n").Replace("<span class=\"item-stat-separator -unique\">", "\\n");

                var weapon = new UniqueWeapon
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Intelligence = int.Parse(statLines[2].TextContent),
                    Damage = Regex.Replace(statLines[3].InnerHtml.Replace("<br>", "\\n"), "<.*?>", string.Empty),
                    APS = statLines[4].TextContent,
                    CritChance = statLines[5].TextContent, //i have no idea if there will always be 2 trailing digits after the .
                    PDPS = statLines[6].TextContent,
                    EDPS = statLines[7].TextContent,
                    DPS = statLines.ElementAtOrDefault(8)?.TextContent ?? "N/A",
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = Regex.Replace(formattedStats, "<.*?>", string.Empty),
                    Type = upperCaseSingular,

                };

                _uniqueWeapons.Add(weapon);

            });

            Log($"Finished getting {lowerCasePlural}.");

            return Task.CompletedTask;

        }

        internal Task GetSingleAttributeArmoursAsync(IElement table, string attribute, string armorType, string upperCaseSingular, string lowerCasePlural)
        {

            Log($"Getting {lowerCasePlural}...");

            var armours = table.Children.Where(element => !element.TextContent.Contains("Stats"));

            Parallel.ForEach(armours, item =>
            {

                var statLines = item.GetElementsByTagName("td");
                var statElement = statLines[4];
                var formattedStats = statElement.InnerHtml.Replace("<br>", "\\n").Replace("<span class=\"item-stat-separator -unique\">", "\\n");

                var armour = new UniqueArmour
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = Regex.Replace(formattedStats, "<.*?>", string.Empty),
                    Type = upperCaseSingular,

                };

                armour.GetType().GetProperties().First(property => property.Name == attribute).SetValue(armour, int.Parse(statLines[2].TextContent));
                armour.GetType().GetProperties().First(property => property.Name == armorType).SetValue(armour, statLines[3].TextContent);

                _uniqueArmours.Add(armour);

            });

            Log($"Finished getting {lowerCasePlural}.");

            return Task.CompletedTask;

        }

        internal Task GetDoubleAttributeArmoursAsync(IElement table, string attributeOne, string attributeTwo, string armorTypeOne, string armorTypeTwo,
            string upperCaseSingular, string lowerCasePlural)
        {

            Log($"Getting {lowerCasePlural}...");

            var armours = table.Children.Where(element => !element.TextContent.Contains("Stats"));

            Parallel.ForEach(armours, item =>
            {

                var statLines = item.GetElementsByTagName("td");
                var statElement = statLines[4];
                var formattedStats = statElement.InnerHtml.Replace("<br>", "\\n").Replace("<span class=\"item-stat-separator -unique\">", "\\n");

                var armour = new UniqueArmour
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = Regex.Replace(formattedStats, "<.*?>", string.Empty),
                    Type = upperCaseSingular,

                };

                armour.GetType().GetProperties().First(property => property.Name == attributeOne).SetValue(armour, int.Parse(statLines[2].TextContent));
                armour.GetType().GetProperties().First(property => property.Name == attributeTwo).SetValue(armour, int.Parse(statLines[3].TextContent));
                armour.GetType().GetProperties().First(property => property.Name == armorTypeOne).SetValue(armour, statLines[4].TextContent);
                armour.GetType().GetProperties().First(property => property.Name == armorTypeTwo).SetValue(armour, statLines[5].TextContent);

                _uniqueArmours.Add(armour);

            });

            Log($"Finished getting {lowerCasePlural}.");

            return Task.CompletedTask;

        }

        //there is only one type of armor that currently has all 3 attributes, but i might as well make a method for future use
        internal Task GetTripleAttributeArmoursAsync(IElement table, string upperCaseSingular, string lowerCasePlural)
        {

            Log($"Getting {lowerCasePlural}...");

            var armours = table.Children.Where(element => !element.TextContent.Contains("Stats"));

            Parallel.ForEach(armours, item =>
            {

                var statLines = item.GetElementsByTagName("td");
                var statElement = statLines[4];
                var formattedStats = statElement.InnerHtml.Replace("<br>", "\\n").Replace("<span class=\"item-stat-separator -unique\">", "\\n");

                var armour = new UniqueArmour
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Strength = int.Parse(statLines[2].TextContent),
                    Dexterity = int.Parse(statLines[3].TextContent),
                    Intelligence = int.Parse(statLines[4].TextContent),
                    Armour = statLines[5].TextContent,
                    Evasion = statLines[6].TextContent,
                    EnergyShield = statLines[7].TextContent,
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = Regex.Replace(formattedStats, "<.*?>", string.Empty),
                    Type = upperCaseSingular,

                };

                _uniqueArmours.Add(armour);

            });

            Log($"Finished getting {lowerCasePlural}.");

            return Task.CompletedTask;

        }

        internal Task GetUniqueAccessoriesAsync(IElement table, string upperCaseSingular, string lowerCasePlural)
        {

            Log($"Getting {lowerCasePlural}...");

            var accessories = table.Children.Where(element => !element.TextContent.Contains("ItemStats"));

            Parallel.ForEach(accessories, item =>
            {

                var statLines = item.GetElementsByTagName("td");
                var statElement = statLines[2];
                var formattedStats = statElement.InnerHtml.Replace("Corrupted", string.Empty).Replace("<br>", "\\n").Trim().Replace("<span class=\"item-stat-separator -unique\">", "\\n");

                var accessory = new UniqueAccessory
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = Regex.Replace(formattedStats, "<.*?>", string.Empty),
                    IsCorrupted = statElement.TextContent.Contains("Corrupted"),
                    Type = upperCaseSingular,

                };

                _uniqueAccessories.Add(accessory);

            });

            Log($"Finished getting {lowerCasePlural}.");

            return Task.CompletedTask;

        }

        internal Task GetUniqueLifeManaFlasksAsync(IElement table, string upperCaseSingular, string lowerCasePlural, bool isManaFlask)
        {

            Log($"Getting {lowerCasePlural}...");

            var flasks = table.Children.Where(element => !element.TextContent.Contains("CapacityStats"));

            Parallel.ForEach(flasks, item =>
            {

                var statLines = item.GetElementsByTagName("td");
                var statElement = statLines[6];
                var formattedStats = statElement.InnerHtml.Replace("Corrupted", string.Empty).Replace("<br>", "\\n").Trim().Replace("<span class=\"item-stat-separator -unique\">", "\\n");

                var flask = new UniqueFlask
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Life = isManaFlask ? "0" : statLines[2].TextContent,
                    Mana = isManaFlask ? statLines[2].TextContent : "0",
                    Duration = statLines[3].TextContent,
                    Usage = statLines[4].TextContent,
                    Capacity = statLines[5].TextContent,
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = Regex.Replace(formattedStats, "<.*?>", string.Empty),
                    Type = upperCaseSingular,

                };

                _uniqueFlasks.Add(flask);

            });

            Log($"Finished getting {lowerCasePlural}.");

            return Task.CompletedTask;

        }

        internal Task GetUniqueHybridFlasksAsync(IElement table)
        {

            Log($"Getting hybrid flasks...");

            var flasks = table.Children.Where(element => !element.TextContent.Contains("CapacityStats"));

            Parallel.ForEach(flasks, item =>
            {

                var statLines = item.GetElementsByTagName("td");
                var statElement = statLines[7];
                var formattedStats = statElement.InnerHtml.Replace("Corrupted", string.Empty).Replace("<br>", "\\n").Trim().Replace("<span class=\"item-stat-separator -unique\">", "\\n");

                var flask = new UniqueFlask
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Life = statLines[2].TextContent,
                    Mana = statLines[3].TextContent,
                    Duration = statLines[4].TextContent,
                    Usage = statLines[5].TextContent,
                    Capacity = statLines[6].TextContent,
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = Regex.Replace(formattedStats, "<.*?>", string.Empty),
                    Type = "Hybrid Flask",

                };

                _uniqueFlasks.Add(flask);

            });

            Log($"Finished getting hybrid flasks.");

            return Task.CompletedTask;

        }

        internal Task GetUniqueUtilityFlasksAsync(IElement table)
        {

            Log($"Getting utility flasks...");

            var flasks = table.Children.Where(element => !element.TextContent.Contains("Stats"));

            Parallel.ForEach(flasks, item =>
            {

                var statLines = item.GetElementsByTagName("td");
                var buffElement = statLines[5];
                var formattedBuffs = buffElement.InnerHtml.Replace("<br>", "\\n").Replace("<span class=\"item-stat-separator -unique\">", "\\n");
                var statElement = statLines[6];
                var formattedStats = statElement.InnerHtml.Replace("Corrupted", string.Empty).Replace("<br>", "\\n").Trim().Replace("<span class=\"item-stat-separator -unique\">", "\\n");

                var flask = new UniqueFlask
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Duration = statLines[2].TextContent,
                    Usage = statLines[3].TextContent,
                    Capacity = statLines[5].TextContent,
                    BuffEffects = Regex.Replace(formattedBuffs, "<.*?>", string.Empty),
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = Regex.Replace(formattedStats, "<.*?>", string.Empty),
                    Type = "Utility Flask",

                };

                _uniqueFlasks.Add(flask);

            });

            Log($"Finished getting utility flasks.");

            return Task.CompletedTask;

        }

        internal Task GetUniqueJewelsAsync(IElement table, string obtainMethod)
        {

            Log($"Getting jewels...");

            var jewels = table.Children.Where(element => !element.TextContent.Contains("RadiusStats"));

            Parallel.ForEach(jewels, item =>
            {

                var statLines = item.GetElementsByTagName("td");
                var statElement = statLines[3];
                var formattedStats = statElement.InnerHtml.Replace("Corrupted", string.Empty).Replace("<br>", "\\n").Replace("<span class=\"item-stat-separator -unique\">", "\\n");

                var jewel = new UniqueJewel
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    Limit = statLines[1].TextContent,
                    Radius = statLines[2].TextContent,
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = Regex.Replace(formattedStats, "<.*?>", string.Empty),
                    IsCorrupted = statLines[3].TextContent.Contains("Corrupted"),
                    ObtainMethod = obtainMethod

                };

                _uniqueJewels.Add(jewel);

            });

            Log($"Finished getting jewels.");

            return Task.CompletedTask;

        }
        #endregion Uniques

        internal void Log(string message)
            => Console.WriteLine($"{DateTime.Now} {message}");

    }

}