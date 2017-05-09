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
using PathOfExileDataScraper.Items;
using System.Net;

//I tried so hard to reduce the lines of code needed that now I'm scared to the fact that I may not
//even remember what some parts even do after taking a break from this.

namespace PathOfExileDataScraper
{

    class ItemScraperOne
    {
        static void Main(string[] args)
            => new ItemScraperOne().Start().GetAwaiter().GetResult();

        internal const string BaseUrl = "http://pathofexile.gamepedia.com";
        internal const string InMemoryDb = "Data Source = :memory:";
        internal const string FlatFileDb = "Data Source = PathOfExile.db";
        internal const string HtmlTagRegex = "<.*?>";
        internal readonly string[] TypesOfArmourSingular = new string[] { "Body Armour", "Boot", "Glove", "Helmet" };
        internal readonly string[] TypesOfArmourPlural = new string[] { "body armours", "boots", "gloves", "helmets" };
        internal readonly string[] TypesOfWeaponSingular = new string[] { "One Handed Axe", "Two Handed Axe", "Bow", "Claw", "Dagger", "Fishing Rod",
            "One Handed Mace", "Sceptre", "Two Handed Mace", "One Handed Sword", "Thrusting One Handed Sword", "Two Handed Sword", "Staff", "Wand" };
        internal readonly string[] TypesOfWeaponPlural = new string[] {"one handed axes", "two handed axes", "bows", "claws", "daggers", "fishing rods",
            "one handed maces", "sceptres", "two handed maces", "one handed swords", "thrusting one handed swords", "two handed swords", "staves", "wands" };

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
        #endregion Uniques

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

        internal async Task Start()
        {

            Log("Path of Exile Data Scraper");
            _web = new HttpClient { BaseAddress = new Uri(BaseUrl) };
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
            Log($"Getting generic flasks took {_stopwatch.Elapsed.TotalSeconds} seconds.");
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
            _stopwatch.Reset();

            await new ItemScraperTwo(_web, _parser, _stopwatch, _connection, HtmlTagRegex, BaseUrl).Run();

            //await new InfoScraper().Run(_connection, _web, _parser, _stopwatch);

            _connection.Close();

            Console.ReadKey();

        }

        internal async Task GetGenericWeapons()
        {

            Log("Getting generic weapons...");

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
                "'Url' TEXT, " +
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

            Log("Finished getting generic weapons.");
            Log("Inserting generic weapons into database...");
            await _connection.InsertAsync(_genericWeapons);
            Log($"Finished inserting generic weapons into database.");

        }

        internal async Task GetGenericArmours()
        {

            Log("Getting generic armours...");

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
                "'Type' TEXT,  " +
                "'Url' TEXT ) ");

            _genericArmours = new ConcurrentBag<GenericArmour>();

            await GetGenericArmoursAsync(GenericBodyArmoursUrl, "Body Armour", "body armours", false);
            await GetGenericArmoursAsync(GenericBootsUrl, "Boot", "boots", false);
            await GetGenericArmoursAsync(GenericGlovesUrl, "Glove", "gloves", false);
            await GetGenericArmoursAsync(GenericHelmetsUrl, "Helmet", "helmets", false);
            await GetGenericArmoursAsync(GenericShieldsUrl, "Shield", "shields", true);

            Log("Finished getting generic armours.");
            Log("Inserting generic armours into database...");
            await _connection.InsertAsync(_genericArmours);
            Log($"Finished inserting generic armours into database.");

        }

        internal async Task GetGenericAccessories()
        {

            Log("Getting generic accessories...");

            await _connection.ExecuteAsync("CREATE TABLE 'GenericAccessories' ( " +
                "'Name' TEXT NOT NULL, " +
                "'LevelReq' INTEGER, " +
                "'ImageUrl' TEXT, " +
                "'Stats' TEXT, " +
                "'IsCorrupted' INTEGER, " +
                "'Type' TEXT," +
                "'Url' TEXT )");

            _genericAccessories = new ConcurrentBag<GenericAccessory>();

            await GetGenericAcessoriesAsync(GenericAmuletsUrl, "Amulet", "amulets");
            await GetGenericAcessoriesAsync(GenericRingsUrl, "Ring", "rings");
            await GetGenericAcessoriesAsync(GenericQuiversUrl, "Quiver", "quivers");
            await GetGenericAcessoriesAsync(GenericBeltsUrl, "Belt", "belts");

            Log("Finished getting generic accessories.");
            Log("Inserting generic accessories into database...");
            await _connection.InsertAsync(_genericAccessories);
            Log($"Finished inserting generic accessories into database.");

        }

        internal async Task GetGenericFlasks()
        {

            Log("Getting generic flasks...");

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
                "'Type' TEXT," +
                "'Url' TEXT, " +
                "PRIMARY KEY('Name') )");

            _genericFlasks = new ConcurrentBag<GenericFlask>();

            await GetGenericLifeManaFlasks(GenericLifeFlasks, "Life Flask", "life flasks", false);
            await GetGenericLifeManaFlasks(GenericManaFlasks, "Mana Flask", "mana flasks", true);
            await GetGenericUtilityFlasks(GenericUtilityFlasks);
            await GetGenericUtilityFlasks(GenericCriticalUtiliyFlasks);

            Log("Finished getting generic flasks.");
            Log("Inserting generic flasks into database...");
            await _connection.InsertAsync(_genericFlasks);
            Log($"Finished inserting generic flasks into database.");

        }

        internal async Task GetUniqueWeapons()
        {

            Log("Getting unique weapons...");

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
                "'Url' TEXT, " +
                "PRIMARY KEY('Name') )"); ;

            _uniqueWeapons = new ConcurrentBag<UniqueWeapon>();

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(UniqueWeaponsUrl));
            var tables = dom.GetElementById("mw-content-text").GetElementsByTagName("tbody");

            await GetUniqueDoubleAttributeWeapons(tables[0], "Strength", "Dexterity", "One Handed Axe", "one handed axes");
            await GetUniqueDoubleAttributeWeapons(tables[1], "Strength", "Dexterity", "Two Handed Axe", "two handed axes");
            await GetUniqueDoubleAttributeWeapons(tables[5], "Strength", "Dexterity", "Fishing Rod", "fishing rods");
            await GetUniqueDoubleAttributeWeapons(tables[9], "Strength", "Dexterity", "One Handed Sword", "one handed swords");
            await GetUniqueDoubleAttributeWeapons(tables[11], "Strength", "Dexterity", "Two Handed Sword", "two handed swords");

            await GetUniqueSingleAttributeWeapons(tables[2], "Dexterity", "Bow", "bows");
            await GetUniqueSingleAttributeWeapons(tables[10], "Dexterity", "Thrusting One Handed Sword", "thrusting one handed swords");

            await GetUniqueDoubleAttributeWeapons(tables[3], "Dexterity", "Intelligence", "Claw", "claws");
            await GetUniqueDoubleAttributeWeapons(tables[4], "Dexterity", "Intelligence", "Dagger", "daggers");

            await GetUniqueSingleAttributeWeapons(tables[6], "Strength", "One Handed Mace", "one handed maces");
            await GetUniqueSingleAttributeWeapons(tables[8], "Strength", "Two Handed Mace", "two handed maces");

            await GetUniqueDoubleAttributeWeapons(tables[7], "Strength", "Intelligence", "Sceptre", "sceptres");
            await GetUniqueDoubleAttributeWeapons(tables[12], "Strength", "Intelligence", "Staff", "staves");

            await GetUniqueSingleAttributeWeapons(tables[13], "Intelligence", "Wand", "wands");

            Log("Finished getting unique weapons.");
            Log("Inserting unique weapons into database...");
            await _connection.InsertAsync(_uniqueWeapons);
            Log($"Finished inserting unique weapons into database.");

        }

        internal async Task GetUniqueArmours()
        {

            Log("Getting unique armours...");

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
                "'Url' TEXT, " +
                "'Type' TEXT )");

            _uniqueArmours = new ConcurrentBag<UniqueArmour>();

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(UniqueArmoursUrl));
            var tables = dom.GetElementById("mw-content-text").GetElementsByTagName("tbody").Where(element => element.Children.Length > 1).ToArray(); //make sure it doesnt just contain the label
            var singleAttributeTableGroups = new int[] { 0, 7, 13, 20 }; //represents where each of the singlar attribute armor tables start
            var doubleAttributeTableGroups = new int[] { 3, 10, 16, 23 };

            for (int index = 0; index < 4; index++)
            {

                var i = singleAttributeTableGroups[index];
                var s = TypesOfArmourSingular[index];
                var p = TypesOfArmourPlural[index];

                await GetUniqueSingleAttributeArmours(tables[i], "Strength", "Armour", s, p);
                await GetUniqueSingleAttributeArmours(tables[i + 1], "Dexterity", "Evasion", s, p);
                await GetUniqueSingleAttributeArmours(tables[i + 2], "Intelligence", "EnergyShield", s, p);

            }

            for (int index = 0; index < 4; index++)
            {

                var i = doubleAttributeTableGroups[index];
                var s = TypesOfArmourSingular[index];
                var p = TypesOfArmourPlural[index];

                await GetUniqueDoubleAttributeArmours(tables[i], "Strength", "Dexterity", "Armour", "Evasion", s, p);
                await GetUniqueDoubleAttributeArmours(tables[i], "Strength", "Intelligence", "Armour", "EnergyShield", s, p);
                await GetUniqueDoubleAttributeArmours(tables[i], "Dexterity", "Intelligence", "Evasion", "EnergyShield", s, p);

            }

            await GetUniqueTripleAttributeArmours(tables[6], TypesOfArmourSingular[0], TypesOfArmourPlural[0]);

            Log("Finished getting unique armours.");
            Log("Inserting unique armours into database...");
            await _connection.InsertAsync(_uniqueArmours);
            Log($"Finished inserting unique armours into database.");

        }

        internal async Task GetUniqueAccessories()
        {

            Log("Getting unique accessories...");

            await _connection.ExecuteAsync("CREATE TABLE 'UniqueAccessories' ( " +
                "'Name' TEXT NOT NULL, " +
                "'LevelReq' INTEGER, " +
                "'Stats' TEXT, " +
                "'IsCorrupted' INTEGER, " +
                "'ImageUrl' TEXT, " +
                "'Type' TEXT," +
                "'Url' TEXT )");

            _uniqueAccessories = new ConcurrentBag<UniqueAccessory>();

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(UniqueAccessoriesUrl));
            var tables = dom.GetElementById("mw-content-text").GetElementsByTagName("tbody");

            await GetUniqueAccessories(tables[0], "Amulet", "amulets");
            await GetUniqueAccessories(tables[1], "Belt", "belts");
            await GetUniqueAccessories(tables[2], "Ring", "rings");
            await GetUniqueAccessories(tables[3], "Quiver", "quivers");
            await GetUniqueAccessories(tables[4], "Quiver", "quivers"); //i have no clue why there's a random quiver by itself here

            Log("Finished getting unique accessories.");
            Log("Inserting unique accessories into database...");
            await _connection.InsertAsync(_uniqueAccessories);
            Log($"Finished inserting unique accessories into database.");

        }

        internal async Task GetUniqueFlasks()
        {

            Log("Getting unique flasks...");

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
                "'Type' TEXT," +
                "'Url' TEXT )");

            _uniqueFlasks = new ConcurrentBag<UniqueFlask>();

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(UniqueFlasksUrl));
            var tables = dom.GetElementById("mw-content-text").GetElementsByTagName("tbody");

            //possible way to shorten: use counters
            await GetUniqueLifeManaFlasks(tables[0], "Life Flask", "life flasks", false);
            await GetUniqueLifeManaFlasks(tables[1], "Mana Flask", "mana flasks", true);
            await GetUniqueHybridFlasks(tables[2]);
            await GetUniqueUtilityFlasks(tables[3]);

            Log("Finished getting unique flasks");
            Log("Inserting unique flasks into database...");
            await _connection.InsertAsync(_uniqueFlasks);
            Log($"Finished inserting unique flasks into database.");

        }

        internal async Task GetUniqueJewels()
        {

            Log("Getting unique jewels...");

            await _connection.ExecuteAsync("CREATE TABLE 'UniqueJewels' ( " +
                "'Name' TEXT NOT NULL, " +
                "'Limit' TEXT, " +
                "'Radius' TEXT, " +
                "'Stats' TEXT, " +
                "'ImageUrl' TEXT, " +
                "'IsCorrupted' INTEGER, " +
                "'ObtainMethod' TEXT, " +
                "'Url' TEXT ) ");

            _uniqueJewels = new ConcurrentBag<UniqueJewel>();

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(UniqueJewelsUrl));
            var tables = dom.GetElementById("mw-content-text").GetElementsByTagName("tbody");

            await GetUniqueJewels(tables[0], "Drop");
            await GetUniqueJewels(tables[1], "Corruption");
            await GetUniqueJewels(tables[2], "Labryninth");
            await GetUniqueJewels(tables[3], "Beta");

            Log("Finished getting unique jewels.");
            Log("Inserting unique jewels into database...");
            await _connection.InsertAsync(_uniqueJewels);
            Log($"Finished unique jewels into database.");

        }

        #region Generics
        #region Generic Weapons
        internal Task GetGenericAxesSwordsAsync(IHtmlDocument dom, string upperCaseSingular, string lowerCasePlural, int tableToUse)
        {

            var mainDom = dom.GetElementById("mw-content-text");
            var tables = mainDom.GetElementsByTagName("table");

            var oneHandedAxes = tables.ElementAtOrDefault(tableToUse).GetElementsByTagName("tbody").First().Children.Where(element => !element.TextContent.Contains("DPSStats")); //.Where(e => e.TextContent != "ItemDamageAPSCritDPSStats");

            Log($"Getting {lowerCasePlural}...");

            Parallel.ForEach(oneHandedAxes, axe =>
            {

                var statLines = axe.GetElementsByTagName("td");
                var weapon = GetGenericDoubleAttributeWeapons(statLines).Result;

                weapon.Strength = statLines[2].TextContent;
                weapon.Dexterity = statLines[2].TextContent;
                weapon.Type = upperCaseSingular;

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
                var weapon = GetGenericSingleAttributeWeapons(statLines).Result;

                weapon.Dexterity = statLines[2].TextContent;
                weapon.Type = upperCaseSingular;

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
            var weapons = mainDom.GetElementsByTagName("tbody").First().Children.Where(element => !element.TextContent.Contains("CritDPS"));

            Parallel.ForEach(weapons, claw =>
            {

                var statLines = claw.GetElementsByTagName("td");

                var weapon = new GenericWeapon
                {

                    Name = statLines.First().GetElementsByTagName("a").First().TextContent,
                    LevelReq = statLines[1].TextContent,
                    Damage = statLines[2].TextContent,
                    APS = statLines[3].TextContent,
                    CritChance = statLines[4].TextContent, //i have no idea if there will always be 2 trailing digits after the .
                    DPS = statLines[5].TextContent,
                    ImageUrl = statLines.First().GetElementsByTagName("img").First().GetAttribute("src"),
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
                var weapon = GetGenericDoubleAttributeWeapons(statLines).Result;

                weapon.Strength = statLines[2].TextContent;
                weapon.Dexterity = statLines[3].TextContent;
                weapon.Type = upperCaseSingular;

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
                var weapon = GetGenericSingleAttributeWeapons(statLines).Result;

                weapon.Strength = statLines[2].TextContent;
                weapon.Type = upperCaseSingular;

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
            var weapons = mainDom.GetElementsByTagName("tbody").First().Children.Where(element => !element.TextContent.Contains("DPSStats"));

            Parallel.ForEach(weapons, claw =>
            {

                var statLines = claw.GetElementsByTagName("td");
                var weapon = GetGenericDoubleAttributeWeapons(statLines).Result;

                weapon.Dexterity = statLines[2].TextContent;
                weapon.Intelligence = statLines[3].TextContent;
                weapon.Type = upperCaseSingular;

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

            var wands = weapons.First().Children.Where(element => !element.TextContent.Contains("DPSStats"));

            Parallel.ForEach(wands, wand =>
            {

                var statLines = wand.GetElementsByTagName("td");
                var weapon = GetGenericSingleAttributeWeapons(statLines).Result;

                weapon.Intelligence = statLines[2].TextContent;
                weapon.Type = "Wand"; //only one type of wand

                _genericWeapons.Add(weapon);

            });

            Log("Finished getting wands.");

        }

        internal Task<GenericWeapon> GetGenericSingleAttributeWeapons(IHtmlCollection<IElement> statLines)
        {

            var info = statLines.First().FirstElementChild.FirstElementChild;
            var formattedStats = statLines.ElementAtOrDefault(9)?.InnerHtml ?? statLines[7].InnerHtml;
            formattedStats = formattedStats.Replace("<br>", "\\n").Replace(" <br> ", "\\n");

            return Task.FromResult(new GenericWeapon
            {

                Name = info.TextContent,
                LevelReq = statLines[1].TextContent,
                Damage = statLines[3].TextContent,
                APS = statLines[4].TextContent,
                CritChance = statLines[5].TextContent,
                DPS = statLines.ElementAtOrDefault(8)?.TextContent ?? statLines[6].TextContent, //need to account for bows, why are they different -.-
                ImageUrl = statLines.First().GetElementsByTagName("img").First().GetAttribute("src"),
                Stats = Regex.Replace(formattedStats, HtmlTagRegex, string.Empty),
                Url = BaseUrl + info.GetAttribute("href"),

            });

        }

        internal Task<GenericWeapon> GetGenericDoubleAttributeWeapons(IHtmlCollection<IElement> statLines)
        {

            var info = statLines.First().FirstElementChild.FirstElementChild;
            var formattedStats = statLines[8].TextContent.Replace("<br>", "\\n").Replace(" <br> ", "\\n");

            return Task.FromResult(new GenericWeapon
            {

                Name = info.TextContent,
                LevelReq = statLines[1].TextContent,
                Damage = statLines[4].TextContent,
                APS = statLines[5].TextContent,
                CritChance = statLines[6].TextContent,
                DPS = statLines[7].TextContent,
                ImageUrl = statLines.First().GetElementsByTagName("img").First().GetAttribute("src"),
                Stats = Regex.Replace(formattedStats, HtmlTagRegex, string.Empty),
                Url = BaseUrl + info.GetAttribute("href"),

            });

        }
        #endregion Generic Weapons

        #region Generic Armours
        internal async Task GetGenericArmoursAsync(string url, string upperCaseSingular, string lowerCasePlural, bool isShield)
        {

            Log($"Getting armor {lowerCasePlural}...");

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(url));
            var mainDom = dom.GetElementById("mw-content-text");
            var armours = mainDom.GetElementsByTagName("tbody");

            var strengthArmours = armours.First().Children.Where(element => !element.TextContent.Contains("Stats"));

            Parallel.ForEach(strengthArmours, armor =>
            {

                var statLines = armor.GetElementsByTagName("td");
                var armour = GetGenericSingleAttributeArmours(statLines, isShield).Result;

                armour.Strength = statLines[2].TextContent;
                armour.Armour = statLines[3].TextContent;
                armour.Type = upperCaseSingular;

                _genericArmours.Add(armour);

            });

            Log($"Finished getting armor {lowerCasePlural}.");
            Log($"Getting evasion {lowerCasePlural}...");

            var evasionArmours = armours.ElementAtOrDefault(1).Children.Where(element => !element.TextContent.Contains("Stats"));

            Parallel.ForEach(evasionArmours, armor =>
            {

                var statLines = armor.GetElementsByTagName("td");
                var armour = GetGenericSingleAttributeArmours(statLines, isShield).Result;

                armour.Dexterity = statLines[2].TextContent;
                armour.Evasion = statLines[3].TextContent;
                armour.Type = upperCaseSingular;

                _genericArmours.Add(armour);

            });

            Log($"Finished getting evasion {lowerCasePlural}.");
            Log($"Getting energy shield {lowerCasePlural}...");

            var energyShieldArmours = armours.ElementAtOrDefault(2).Children.Where(element => !element.TextContent.Contains("Stats"));

            Parallel.ForEach(energyShieldArmours, armor =>
            {

                var statLines = armor.GetElementsByTagName("td");
                var armour = GetGenericSingleAttributeArmours(statLines, isShield).Result;

                armour.Intelligence = statLines[2].TextContent;
                armour.EnergyShield = statLines[3].TextContent;
                armour.Type = upperCaseSingular;

                _genericArmours.Add(armour);

            });

            Log($"Finished getting energy shield {lowerCasePlural}.");
            Log($"Getting armour/evasion {lowerCasePlural}...");

            var armourEvasionArmours = armours.ElementAtOrDefault(3).Children.Where(element => !element.TextContent.Contains("Stats"));

            Parallel.ForEach(armourEvasionArmours, armor =>
            {

                var statLines = armor.GetElementsByTagName("td");
                var armour = GetGenericDoubleAttributeArmours(statLines, isShield).Result;

                armour.Strength = statLines[2].TextContent;
                armour.Dexterity = statLines[3].TextContent;
                armour.Armour = statLines[4].TextContent;
                armour.Evasion = statLines[5].TextContent;
                armour.Type = upperCaseSingular;

                _genericArmours.Add(armour);

            });

            Log($"Finished getting armour/evasion {lowerCasePlural}.");
            Log($"Getting armour/energy shield {lowerCasePlural}...");

            var armourEnergyArmours = armours.ElementAtOrDefault(4).Children.Where(element => !element.TextContent.Contains("Stats"));

            Parallel.ForEach(armourEnergyArmours, armor =>
            {

                var statLines = armor.GetElementsByTagName("td");
                var armour = GetGenericDoubleAttributeArmours(statLines, isShield).Result;

                armour.Strength = statLines[2].TextContent;
                armour.Intelligence = statLines[3].TextContent;
                armour.Armour = statLines[4].TextContent;
                armour.Intelligence = statLines[5].TextContent;
                armour.Type = upperCaseSingular;

                _genericArmours.Add(armour);

            });

            Log($"Finished getting armour/energy shield {lowerCasePlural}.");
            Log($"Getting evasion/energy shield {lowerCasePlural}...");

            var evasionEnergyArmours = armours.ElementAtOrDefault(5).Children.Where(element => !element.TextContent.Contains("Stats"));

            Parallel.ForEach(evasionEnergyArmours, armor =>
            {

                var statLines = armor.GetElementsByTagName("td");
                var armour = GetGenericDoubleAttributeArmours(statLines, isShield).Result;

                armour.Dexterity = statLines[2].TextContent;
                armour.Intelligence = statLines[3].TextContent;
                armour.Evasion = statLines[4].TextContent;
                armour.Intelligence = statLines[5].TextContent;
                armour.Type = upperCaseSingular;

                _genericArmours.Add(armour);

            });

            Log($"Finished getting evasion/energy shield {lowerCasePlural}.");

        }

        internal Task<GenericArmour> GetGenericSingleAttributeArmours(IHtmlCollection<IElement> statLines, bool isShield)
        {

            var info = statLines.First().FirstElementChild.FirstElementChild;
            var formattedStats = isShield ? statLines[5].InnerHtml : statLines[4].InnerHtml;
            formattedStats = formattedStats.Replace("<br>", "\\n").Replace(" <br> ", "\\n");

            return Task.FromResult(new GenericArmour
            {

                Name = info.TextContent,
                LevelReq = statLines[1].TextContent,
                BlockChance = isShield ? statLines[4].TextContent : "N/A",
                Stats = Regex.Replace(formattedStats, HtmlTagRegex, string.Empty),
                ImageUrl = statLines.First().GetElementsByTagName("img").First().GetAttribute("src"),
                Url = BaseUrl + info.GetAttribute("href"),

            });

        }

        internal Task<GenericArmour> GetGenericDoubleAttributeArmours(IHtmlCollection<IElement> statLines, bool isShield)
        {

            var info = statLines.First().FirstElementChild.FirstElementChild;
            var formattedStats = isShield ? statLines[7].InnerHtml : statLines[6].InnerHtml;
            formattedStats = formattedStats.Replace("<br>", "\\n").Replace(" <br> ", "\\n");

            return Task.FromResult(new GenericArmour
            {

                Name = info.TextContent,
                LevelReq = statLines[1].TextContent,
                BlockChance = isShield ? statLines[6].TextContent : "N/A",
                Stats = Regex.Replace(formattedStats, HtmlTagRegex, string.Empty),
                ImageUrl = statLines.First().GetElementsByTagName("img").First().GetAttribute("src"),
                Url = BaseUrl + info.GetAttribute("href"),

            });

        }
        #endregion Generic Armours

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
                var info = statLines.First().FirstElementChild.FirstElementChild;
                var statsHtml = statLines[2].InnerHtml;
                var formattedStats = statsHtml.Replace("Corrupted", string.Empty).Replace("<br>", "\\n").Replace(" <br> ", "\\n");

                var item = new GenericAccessory
                {

                    Name = info.TextContent,
                    LevelReq = statLines[1].TextContent,
                    ImageUrl = statLines.First().GetElementsByTagName("img").First().GetAttribute("src"),
                    Stats = formattedStats,
                    IsCorrupted = statsHtml.Contains("Corrupted"),
                    Type = upperCaseSingular,
                    Url = BaseUrl + info.GetAttribute("href"),

                };

                _genericAccessories.Add(item);

            });

            Log($"Finished getting {lowerCasePlural}.");

        }

        #region Generic Flasks
        internal async Task GetGenericLifeManaFlasks(string url, string upperCaseSingular, string lowerCasePlural, bool IsManaFlasks)
        {

            Log($"Getting {lowerCasePlural}...");

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(url));
            var mainDom = dom.GetElementById("mw-content-text");
            var flasksTable = mainDom.GetElementsByTagName("tbody").First();

            var flasks = flasksTable.GetElementsByTagName("tr").Where(element => !element.TextContent.Contains("Capacity"));

            Parallel.ForEach(flasks, flask =>
            {

                var statLines = flask.GetElementsByTagName("td");
                var info = statLines.First().FirstElementChild.FirstElementChild;

                var item = new GenericFlask
                {

                    Name = info.TextContent,
                    LevelReq = statLines[1].TextContent,
                    Life = IsManaFlasks ? "0" : statLines[2].TextContent,
                    Mana = IsManaFlasks ? statLines[2].TextContent : "0",
                    Duration = statLines[3].TextContent,
                    Usage = statLines[4].TextContent,
                    Capacity = statLines[5].TextContent,
                    ImageUrl = statLines.First().GetElementsByTagName("img").First().GetAttribute("src"),
                    Type = upperCaseSingular,
                    Url = BaseUrl + info.GetAttribute("href"),

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
            var flasksTable = mainDom.GetElementsByTagName("tbody").First();

            var flasks = flasksTable.GetElementsByTagName("tr").Where(element => !element.TextContent.Contains("Capacity"));

            Parallel.ForEach(flasks, flask =>
            {

                var statLines = flask.GetElementsByTagName("td");
                var info = statLines.First().FirstElementChild.FirstElementChild;

                var item = new GenericFlask
                {

                    Name = info.TextContent,
                    LevelReq = statLines[1].TextContent,
                    Life = statLines[2].TextContent,
                    Mana = statLines[3].TextContent,
                    Duration = statLines[4].TextContent,
                    Usage = statLines[5].TextContent,
                    Capacity = statLines[6].TextContent,
                    ImageUrl = statLines.First().GetElementsByTagName("img").First().GetAttribute("src"),
                    Type = "Hybrid Flask",
                    Url = BaseUrl + info.GetAttribute("href"),

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
            var flasksTable = mainDom.GetElementsByTagName("tbody").First();

            var flasks = flasksTable.GetElementsByTagName("tr").Where(element => !element.TextContent.Contains("Capacity"));

            Parallel.ForEach(flasks, flask =>
            {

                var statLines = flask.GetElementsByTagName("td");
                var info = statLines.First().FirstElementChild.FirstElementChild;

                var item = new GenericFlask
                {

                    Name = info.TextContent,
                    LevelReq = statLines[1].TextContent,
                    Duration = statLines[2].TextContent,
                    Usage = statLines[3].TextContent,
                    Capacity = statLines[4].TextContent,
                    BuffEffects = Regex.Replace(statLines[5].InnerHtml.Replace("<br>", "\\n"), HtmlTagRegex, string.Empty),
                    Stats = statLines[6].TextContent,
                    ImageUrl = statLines.First().GetElementsByTagName("img").First().GetAttribute("src"),
                    Type = "Utility Flask",
                    Url = BaseUrl + info.GetAttribute("href"),

                };

                _genericFlasks.Add(item);

            });

            Log($"Finished getting utility flasks.");

        }
        #endregion Generic Flasks
        #endregion Generics

        #region Uniques
        #region Unique Weapons
        internal Task GetUniqueSingleAttributeWeapons(IElement table, string attribute, string upperCaseSingular, string lowerCasePlural)
        {

            Log($"Getting {lowerCasePlural}...");

            var weapons = table.Children.Where(element => !element.TextContent.Contains("DPSStats"));

            Parallel.ForEach(weapons, item =>
            {

                var statLines = item.GetElementsByTagName("td");
                var info = statLines.First().FirstElementChild.FirstElementChild;
                var statElement = statLines[9];
                var formattedStats = statElement.InnerHtml.Replace("<br>", "\\n").Replace(" <br> ", "\\n").Replace("<span class=\"item-stat-separator -unique\">", "\\n");

                var weapon = new UniqueWeapon
                {

                    Name = info.TextContent,
                    LevelReq = statLines[1].TextContent,
                    Damage = statLines[3].TextContent,
                    APS = statLines[4].TextContent,
                    CritChance = statLines[5].TextContent,
                    PDPS = statLines[6].TextContent,
                    EDPS = statLines[7].TextContent,
                    DPS = statLines[8].TextContent,
                    ImageUrl = statLines.First().GetElementsByTagName("img").First().GetAttribute("src"),
                    Url = BaseUrl + info.GetAttribute("href"),
                    Stats = Regex.Replace(formattedStats, HtmlTagRegex, string.Empty),
                    Type = upperCaseSingular,

                };

                weapon.GetType().GetProperty(attribute).SetValue(weapon, statLines[2].TextContent);

                _uniqueWeapons.Add(weapon);

            });

            Log($"Finished getting {lowerCasePlural}.");

            return Task.CompletedTask;

        }

        internal Task GetUniqueDoubleAttributeWeapons(IElement table, string attributeOne, string attributeTwo,
            string upperCaseSingular, string lowerCasePlural)
        {

            Log($"Getting {lowerCasePlural}...");

            var weapons = table.Children.Where(element => !element.TextContent.Contains("DPSStats"));

            Parallel.ForEach(weapons, item =>
            {

                var statLines = item.GetElementsByTagName("td");
                var info = statLines.First().FirstElementChild.FirstElementChild;
                var statElement = upperCaseSingular == "Fishing Rod" ? statLines[9] : statLines[10]; //dam fishing rods always ruining my PoE day
                var formattedStats = statElement.InnerHtml.Replace("<br>", "\\n").Replace(" <br> ", "\\n").Replace("<span class=\"item-stat-separator -unique\">", "\\n");

                var weapon = new UniqueWeapon
                {

                    Name = info.TextContent,
                    LevelReq = statLines[1].TextContent,
                    Damage = statLines[4].TextContent,
                    APS = statLines[5].TextContent,
                    CritChance = statLines[6].TextContent,
                    PDPS = statLines[7].TextContent,
                    EDPS = statLines[8].TextContent,
                    DPS = upperCaseSingular == "Fishing Rod" ? "N/A" : statLines[9].TextContent,
                    ImageUrl = statLines.First().GetElementsByTagName("img").First().GetAttribute("src"),
                    Url = BaseUrl + info.GetAttribute("href"),
                    Stats = Regex.Replace(formattedStats, HtmlTagRegex, string.Empty),
                    Type = upperCaseSingular,

                };

                weapon.GetType().GetProperty(attributeOne).SetValue(weapon, statLines[2].TextContent);
                weapon.GetType().GetProperty(attributeTwo).SetValue(weapon, statLines[3].TextContent);

                _uniqueWeapons.Add(weapon);

            });

            Log($"Finished getting {lowerCasePlural}.");

            return Task.CompletedTask;

        }
        #endregion Unique Weapons

        #region Unique Armours
        internal Task GetUniqueSingleAttributeArmours(IElement table, string attribute, string armorType, string upperCaseSingular, string lowerCasePlural)
        {

            Log($"Getting {lowerCasePlural}...");

            var armours = table.Children.Where(element => !element.TextContent.Contains("Stats"));

            Parallel.ForEach(armours, item =>
            {

                var statLines = item.GetElementsByTagName("td");
                var info = statLines.First().FirstElementChild.FirstElementChild;
                var statElement = statLines[4];
                var formattedStats = statElement.InnerHtml.Replace("<br>", "\\n").Replace("<span class=\"item-stat-separator -unique\">", "\\n");

                var armour = new UniqueArmour
                {

                    Name = info.TextContent,
                    LevelReq = statLines[1].TextContent,
                    ImageUrl = statLines.First().GetElementsByTagName("img").First().GetAttribute("src"),
                    Url = BaseUrl + info.GetAttribute("href"),
                    Stats = Regex.Replace(formattedStats, HtmlTagRegex, string.Empty),
                    Type = upperCaseSingular,

                };

                armour.GetType().GetProperty(attribute).SetValue(armour, statLines[2].TextContent);
                armour.GetType().GetProperty(armorType).SetValue(armour, statLines[3].TextContent);

                _uniqueArmours.Add(armour);

            });

            Log($"Finished getting {lowerCasePlural}.");

            return Task.CompletedTask;

        }

        internal Task GetUniqueDoubleAttributeArmours(IElement table, string attributeOne, string attributeTwo, string armorTypeOne, string armorTypeTwo,
            string upperCaseSingular, string lowerCasePlural)
        {

            Log($"Getting {lowerCasePlural}...");

            var armours = table.Children.Where(element => !element.TextContent.Contains("Stats"));

            Parallel.ForEach(armours, item =>
            {

                var statLines = item.GetElementsByTagName("td");
                var info = statLines.First().FirstElementChild.FirstElementChild;
                var statElement = statLines[4];
                var formattedStats = statElement.InnerHtml.Replace("<br>", "\\n").Replace("<span class=\"item-stat-separator -unique\">", "\\n");

                var armour = new UniqueArmour
                {

                    Name = info.TextContent,
                    LevelReq = statLines[1].TextContent,
                    ImageUrl = statLines.First().GetElementsByTagName("img").First().GetAttribute("src"),
                    Url = BaseUrl + info.GetAttribute("href"),
                    Stats = Regex.Replace(formattedStats, HtmlTagRegex, string.Empty),
                    Type = upperCaseSingular,

                };

                armour.GetType().GetProperty(attributeOne).SetValue(armour, statLines[2].TextContent);
                armour.GetType().GetProperty(attributeTwo).SetValue(armour, statLines[3].TextContent);
                armour.GetType().GetProperty(armorTypeOne).SetValue(armour, statLines[4].TextContent);
                armour.GetType().GetProperty(armorTypeTwo).SetValue(armour, statLines[5].TextContent);

                _uniqueArmours.Add(armour);

            });

            Log($"Finished getting {lowerCasePlural}.");

            return Task.CompletedTask;

        }

        //there is only one type of armor that currently has all 3 attributes, but i might as well make a method for future use
        internal Task GetUniqueTripleAttributeArmours(IElement table, string upperCaseSingular, string lowerCasePlural)
        {

            Log($"Getting {lowerCasePlural}...");

            var armours = table.Children.Where(element => !element.TextContent.Contains("Stats"));

            Parallel.ForEach(armours, item =>
            {

                var statLines = item.GetElementsByTagName("td");
                var info = statLines.First().FirstElementChild.FirstElementChild;
                var statElement = statLines[4];
                var formattedStats = statElement.InnerHtml.Replace("<br>", "\\n").Replace("<span class=\"item-stat-separator -unique\">", "\\n");

                var armour = new UniqueArmour
                {

                    Name = info.TextContent,
                    LevelReq = statLines[1].TextContent,
                    Strength = statLines[2].TextContent,
                    Dexterity = statLines[3].TextContent,
                    Intelligence = statLines[4].TextContent,
                    Armour = statLines[5].TextContent,
                    Evasion = statLines[6].TextContent,
                    EnergyShield = statLines[7].TextContent,
                    ImageUrl = statLines.First().GetElementsByTagName("img").First().GetAttribute("src"),
                    Url = BaseUrl + info.GetAttribute("href"),
                    Stats = Regex.Replace(formattedStats, HtmlTagRegex, string.Empty),
                    Type = upperCaseSingular,

                };

                _uniqueArmours.Add(armour);

            });

            Log($"Finished getting {lowerCasePlural}.");

            return Task.CompletedTask;

        }
        #endregion Unique Armours

        internal Task GetUniqueAccessories(IElement table, string upperCaseSingular, string lowerCasePlural)
        {

            Log($"Getting {lowerCasePlural}...");

            var accessories = table.Children.Where(element => !element.TextContent.Contains("ItemStats"));

            Parallel.ForEach(accessories, item =>
            {

                var statLines = item.GetElementsByTagName("td");
                var info = statLines.First().FirstElementChild.FirstElementChild;
                var statElement = statLines[2];
                var formattedStats = statElement.InnerHtml.Replace("Corrupted", string.Empty).Replace("<br>", "\\n").Trim().Replace("<span class=\"item-stat-separator -unique\">", "\\n");

                var accessory = new UniqueAccessory
                {

                    Name = info.TextContent,
                    LevelReq = statLines[1].TextContent,
                    ImageUrl = statLines.First().GetElementsByTagName("img").First().GetAttribute("src"),
                    Stats = Regex.Replace(formattedStats, HtmlTagRegex, string.Empty),
                    IsCorrupted = statElement.TextContent.Contains("Corrupted"),
                    Type = upperCaseSingular,
                    Url = BaseUrl + info.GetAttribute("href"),

                };

                _uniqueAccessories.Add(accessory);

            });

            Log($"Finished getting {lowerCasePlural}.");

            return Task.CompletedTask;

        }

        #region Unique Flasks
        internal Task GetUniqueLifeManaFlasks(IElement table, string upperCaseSingular, string lowerCasePlural, bool isManaFlask)
        {

            Log($"Getting {lowerCasePlural}...");

            var flasks = table.Children.Where(element => !element.TextContent.Contains("CapacityStats"));

            Parallel.ForEach(flasks, item =>
            {

                var statLines = item.GetElementsByTagName("td");
                var info = statLines.First().FirstElementChild.FirstElementChild;
                var statElement = statLines[6];
                var formattedStats = statElement.InnerHtml.Replace("Corrupted", string.Empty).Replace("<br>", "\\n").Trim().Replace("<span class=\"item-stat-separator -unique\">", "\\n");

                var flask = new UniqueFlask
                {

                    Name = info.TextContent,
                    LevelReq = statLines[1].TextContent,
                    Life = isManaFlask ? "0" : statLines[2].TextContent,
                    Mana = isManaFlask ? statLines[2].TextContent : "0",
                    Duration = statLines[3].TextContent,
                    Usage = statLines[4].TextContent,
                    Capacity = statLines[5].TextContent,
                    ImageUrl = statLines.First().GetElementsByTagName("img").First().GetAttribute("src"),
                    Stats = Regex.Replace(formattedStats, HtmlTagRegex, string.Empty),
                    Type = upperCaseSingular,
                    Url = BaseUrl + info.GetAttribute("href"),

                };

                _uniqueFlasks.Add(flask);

            });

            Log($"Finished getting {lowerCasePlural}.");

            return Task.CompletedTask;

        }

        internal Task GetUniqueHybridFlasks(IElement table)
        {

            Log($"Getting hybrid flasks...");

            var flasks = table.Children.Where(element => !element.TextContent.Contains("CapacityStats"));

            Parallel.ForEach(flasks, item =>
            {

                var statLines = item.GetElementsByTagName("td");
                var info = statLines.First().FirstElementChild.FirstElementChild;
                var statElement = statLines[7];
                var formattedStats = statElement.InnerHtml.Replace("Corrupted", string.Empty).Replace("<br>", "\\n").Trim().Replace("<span class=\"item-stat-separator -unique\">", "\\n");

                var flask = new UniqueFlask
                {

                    Name = info.TextContent,
                    LevelReq = statLines[1].TextContent,
                    Life = statLines[2].TextContent,
                    Mana = statLines[3].TextContent,
                    Duration = statLines[4].TextContent,
                    Usage = statLines[5].TextContent,
                    Capacity = statLines[6].TextContent,
                    ImageUrl = statLines.First().GetElementsByTagName("img").First().GetAttribute("src"),
                    Stats = Regex.Replace(formattedStats, HtmlTagRegex, string.Empty),
                    Type = "Hybrid Flask",
                    Url = BaseUrl + info.GetAttribute("href"),

                };

                _uniqueFlasks.Add(flask);

            });

            Log($"Finished getting hybrid flasks.");

            return Task.CompletedTask;

        }

        internal Task GetUniqueUtilityFlasks(IElement table)
        {

            Log($"Getting utility flasks...");

            var flasks = table.Children.Where(element => !element.TextContent.Contains("Stats"));

            Parallel.ForEach(flasks, item =>
            {

                var statLines = item.GetElementsByTagName("td");
                var info = statLines.First().FirstElementChild.FirstElementChild;
                var buffElement = statLines[5];
                var formattedBuffs = buffElement.InnerHtml.Replace("<br>", "\\n").Replace("<span class=\"item-stat-separator -unique\">", "\\n");
                var statElement = statLines[6];
                var formattedStats = statElement.InnerHtml.Replace("Corrupted", string.Empty).Replace("<br>", "\\n").Trim().Replace("<span class=\"item-stat-separator -unique\">", "\\n");

                var flask = new UniqueFlask
                {

                    Name = info.TextContent,
                    LevelReq = statLines[1].TextContent,
                    Duration = statLines[2].TextContent,
                    Usage = statLines[3].TextContent,
                    Capacity = statLines[5].TextContent,
                    BuffEffects = Regex.Replace(formattedBuffs, HtmlTagRegex, string.Empty),
                    ImageUrl = statLines.First().GetElementsByTagName("img").First().GetAttribute("src"),
                    Stats = Regex.Replace(formattedStats, HtmlTagRegex, string.Empty),
                    Type = "Utility Flask",
                    Url = BaseUrl + info.GetAttribute("href"),

                };

                _uniqueFlasks.Add(flask);

            });

            Log($"Finished getting utility flasks.");

            return Task.CompletedTask;

        }
        #endregion Unique Flasks

        internal Task GetUniqueJewels(IElement table, string obtainMethod)
        {

            Log($"Getting jewels...");

            var jewels = table.Children.Where(element => !element.TextContent.Contains("Stats"));

            Parallel.ForEach(jewels, item =>
            {

                var statLines = item.GetElementsByTagName("td");
                var info = statLines.First().FirstElementChild.FirstElementChild;
                var statElement = statLines[3];
                var formattedStats = statElement.InnerHtml.Replace("Corrupted", string.Empty).Replace("<br>", "\\n").Replace("<span class=\"item-stat-separator -unique\">", "\\n");

                var jewel = new UniqueJewel
                {

                    Name = info.TextContent,
                    Limit = statLines[1].TextContent,
                    Radius = statLines[2].TextContent,
                    ImageUrl = statLines.First().GetElementsByTagName("img").First().GetAttribute("src"),
                    Stats = Regex.Replace(formattedStats, HtmlTagRegex, string.Empty),
                    IsCorrupted = statLines[3].TextContent.Contains("Corrupted"),
                    ObtainMethod = obtainMethod,
                    Url = BaseUrl + info.GetAttribute("href"),

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