﻿using System;
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
using System.Collections.Concurrent;
using System.Diagnostics;

namespace PathOfExileDataScraper
{

    class Program
    {
        static void Main(string[] args)
            => new Program().Start().GetAwaiter().GetResult();

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
        #endregion

        #region Armors
        internal const string GenericBodyArmoursUrl = "/List_of_body_armours";
        internal const string GenericBootsUrl = "/List_of_boots";
        internal const string GenericGlovesUrl = "/List_of_gloves";
        internal const string GenericHelmetsUrl = "/List_of_helmets";
        internal const string GenericShieldsUrl = "/List_of_shields";
        #endregion

        #region Accessories
        internal const string GenericAmuletsUrl = "/List_of_amulets";
        internal const string GenericRingsUrl = "/List_of_rings";
        internal const string GenericQuiversUrl = "/List_of_quivers";
        internal const string GenericBeltsUrl = "/List_of_belts";
        #endregion

        #region Flasks
        internal const string GenericLifeFlasks = "/Life_Flasks";
        internal const string GenericManaFlasks = "/Mana_Flasks";
        internal const string GenericHybridFlasks = "/Hybrid_Flasks";
        internal const string GenericUtilityFlasks = "/Utility_Flasks";
        internal const string GenericCriticalUtiliyFlasks = "/Critical_Utility_Flasks";
        #endregion

        //yes, i could pass it through methods to stop persistance
        //but i did this for clarity
        internal ConcurrentBag<GenericWeapon> _genericWeapons;
        internal ConcurrentBag<GenericArmour> _genericArmours;
        internal ConcurrentBag<GenericAccessory> _genericAccessories;
        internal ConcurrentBag<GenericFlask> _genericFlasks;

        internal async Task Start()
        {

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
                "'APS' REAL, " +
                "'CritChance' TEXT, " +
                "'PDPS' REAL, " +
                "'EDPS' REAL, " +
                "'DPS' REAL, " +
                "'Stats' TEXT, " +
                "'ImageUrl' TEXT, " +
                "'Type' TEXT, " +
                "PRIMARY KEY('Name') )");

            _genericWeapons = new ConcurrentBag<GenericWeapon>();

            await GetGenericAxesAsync();
            await GetGenericBowsAsync();
            await GetGenericClawOrDaggerAsync(GenericClawsUrl, "Claw", "claws");
            await GetGenericClawOrDaggerAsync(GenericDaggersUrl, "Dagger", "daggers");
            await GetGenericFishingRodsAsync();
            await GetGenericMacesAsync();
            await GetGenericStavesAsync();
            await GetGenericSwordsAsync();
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
                "'Armour' INTEGER, " +
                "'Evasion' INTEGER, " +
                "'EnergyShield' INTEGER, " +
                "'BlockChance' INTEGER, " +
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
                "'Life' INTEGER, " +
                "'Mana' INTEGER, " +
                "'Duration' REAL, " +
                "'Usage' INTEGER, " +
                "'Capacity' INTEGER, " +
                "'ImageUrl' TEXT, " +
                "'BuffEffects' TEXT, " +
                "'Stats' TEXT, " +
                "'Type' TEXT, " +
                "PRIMARY KEY('Name') )");

            _genericFlasks = new ConcurrentBag<GenericFlask>();

            await GetGenericLifeManaFlasks()

        }

        internal async Task GetGenericAxesAsync()
        {

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(GenericAxesUrl));
            var mainDom = dom.GetElementById("mw-content-text");
            var tables = mainDom.GetElementsByTagName("table");

            var oneHandedAxes = tables.FirstOrDefault().GetElementsByTagName("tbody").FirstOrDefault().Children.Where(element => !element.TextContent.Contains("DPSStats")); //.Where(e => e.TextContent != "ItemDamageAPSCritDPSStats");

            Log("Getting one handed axes...");

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
                    APS = double.Parse(statLines[5].TextContent),
                    CritChance = statLines[6].TextContent, //i have no idea it there will always be 2 trailing digits after the .
                    DPS = double.Parse(statLines[7].TextContent),
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = statLines.ElementAtOrDefault(8)?.TextContent ?? "N/A",
                    Type = "One Handed Axe",

                };

                _genericWeapons.Add(weapon);

            });

            Log("\nFinished getting one handed axes.");

            var twoHandedAxes = tables.ElementAtOrDefault(1).GetElementsByTagName("tbody").FirstOrDefault().Children.Where(element => !element.TextContent.Contains("Stats"));

            Log("Getting two handed axes...");

            Parallel.ForEach(twoHandedAxes, axe =>
            {

                var statLines = axe.GetElementsByTagName("td");

                var weapon = new GenericWeapon
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Strength = int.Parse(statLines[2].TextContent),
                    Dexterity = int.Parse(statLines[3].TextContent),
                    Damage = statLines[4].TextContent,
                    APS = double.Parse(statLines[5].TextContent),
                    CritChance = statLines[6].TextContent, //i have no idea it there will always be 2 trailing digits after the .
                    DPS = double.Parse(statLines[7].TextContent),
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = statLines.ElementAtOrDefault(8)?.TextContent ?? "N/A",
                    Type = "Two Handed Axe",

                };

                _genericWeapons.Add(weapon);

            });

            Log("\nFinished getting two handed axes.");

        }

        internal async Task GetGenericBowsAsync()
        {

            Log("Getting bows...");

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(GenericBowsUrl));
            var mainDom = dom.GetElementById("mw-content-text");
            var weapons = mainDom.GetElementsByTagName("tbody").FirstOrDefault().Children.Where(element => !element.TextContent.Contains("Stats"));

            Parallel.ForEach(weapons, bow =>
            {

                var statLines = bow.GetElementsByTagName("td");

                var weapon = new GenericWeapon
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Dexterity = int.Parse(statLines[2].TextContent),
                    Damage = statLines[3].TextContent,
                    APS = double.Parse(statLines[4].TextContent),
                    CritChance = statLines[5].TextContent, //i have no idea it there will always be 2 trailing digits after the .
                    PDPS = double.TryParse(statLines[6].TextContent, out double pdpsParams) ? pdpsParams : 0,
                    EDPS = double.TryParse(statLines[7].TextContent, out double edpsParams) ? edpsParams : 0,
                    DPS = double.Parse(statLines[8].TextContent),
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = statLines.ElementAtOrDefault(9)?.TextContent ?? "N/A",
                    Type = "Bow",

                };

                _genericWeapons.Add(weapon);

            });

            Log("\nFinished getting bows.");

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
                    APS = double.Parse(statLines[5].TextContent),
                    CritChance = statLines[6].TextContent, //i have no idea it there will always be 2 trailing digits after the .
                    DPS = double.Parse(statLines[7].TextContent),
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = statLines.ElementAtOrDefault(8).TextContent,
                    Type = $"{upperCaseSingular}",

                };

                _genericWeapons.Add(weapon);

            });

            Log($"\nFinished getting {lowerCasePlural}.");

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
                    APS = double.Parse(statLines[3].TextContent),
                    CritChance = statLines[4].TextContent, //i have no idea it there will always be 2 trailing digits after the .
                    DPS = double.Parse(statLines[5].TextContent),
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Type = "Fishing Rod",

                };

                _genericWeapons.Add(weapon);

            });

            Log("\nFinished getting fishing rods.");

        }

        internal async Task GetGenericMacesAsync()
        {

            Log("Getting one handed maces...");

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(GenericMacesUrl));
            var mainDom = dom.GetElementById("mw-content-text");
            var weapons = mainDom.GetElementsByTagName("tbody");

            var oneHandedMaces = weapons.FirstOrDefault().Children.Where(element => !element.TextContent.Contains("DPSStats"));

            Parallel.ForEach(oneHandedMaces, mace =>
            {

                var statLines = mace.GetElementsByTagName("td");

                var weapon = new GenericWeapon
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Strength = int.Parse(statLines[2].TextContent),
                    Damage = statLines[3].TextContent,
                    APS = double.Parse(statLines[4].TextContent),
                    CritChance = statLines[5].TextContent, //i have no idea it there will always be 2 trailing digits after the .
                    DPS = double.Parse(statLines[6].TextContent),
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = statLines.ElementAtOrDefault(7)?.TextContent ?? "N/A",
                    Type = "One Handed Mace",

                };

                _genericWeapons.Add(weapon);

            });

            Log("\nFinished getting one handed maces.");
            Log("Getting sceptres...");

            var spectres = weapons.ElementAtOrDefault(1).Children.Where(element => !element.TextContent.Contains("DPSStats"));

            Parallel.ForEach(spectres, mace =>
            {

                var statLines = mace.GetElementsByTagName("td");

                var weapon = new GenericWeapon
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Strength = int.Parse(statLines[2].TextContent),
                    Intelligence = int.Parse(statLines[3].TextContent),
                    Damage = statLines[4].TextContent,
                    APS = double.Parse(statLines[5].TextContent),
                    CritChance = statLines[6].TextContent, //i have no idea it there will always be 2 trailing digits after the .
                    DPS = double.Parse(statLines[7].TextContent),
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = statLines.ElementAtOrDefault(8)?.TextContent ?? "N/A",
                    Type = "Sceptre",

                };

                _genericWeapons.Add(weapon);

            });

            Log("\nFinished getting sceptres.");
            Log("Getting two handed maces...");

            var twoHandedMaces = weapons.ElementAtOrDefault(2).Children.Where(element => !element.TextContent.Contains("DPSStats"));

            Parallel.ForEach(twoHandedMaces, mace =>
            {

                var statLines = mace.GetElementsByTagName("td");

                var weapon = new GenericWeapon
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Strength = int.Parse(statLines[2].TextContent),
                    Damage = statLines[3].TextContent,
                    APS = double.Parse(statLines[4].TextContent),
                    CritChance = statLines[5].TextContent, //i have no idea it there will always be 2 trailing digits after the .
                    DPS = double.Parse(statLines[6].TextContent),
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = statLines.ElementAtOrDefault(7)?.TextContent ?? "N/A",
                    Type = "Two Handed Mace",

                };

                _genericWeapons.Add(weapon);

            });

            Log("\nFinished getting two handed maces.");

        }

        internal async Task GetGenericStavesAsync()
        {

            Log("Getting staves...");

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(GenericStavesUrl));
            var mainDom = dom.GetElementById("mw-content-text");
            var weapons = mainDom.GetElementsByTagName("tbody");

            var staves = weapons.FirstOrDefault().Children.Where(element => !element.TextContent.Contains("DPSStats"));

            Parallel.ForEach(staves, staff =>
            {

                var statLines = staff.GetElementsByTagName("td");

                var weapon = new GenericWeapon
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Strength = int.Parse(statLines[2].TextContent),
                    Intelligence = int.Parse(statLines[3].TextContent),
                    Damage = statLines[4].TextContent,
                    APS = double.Parse(statLines[5].TextContent),
                    CritChance = statLines[6].TextContent, //i have no idea it there will always be 2 trailing digits after the .
                    DPS = double.Parse(statLines[7].TextContent),
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = statLines.ElementAtOrDefault(8)?.TextContent ?? "N/A",
                    Type = "Staff",

                };

                _genericWeapons.Add(weapon);

            });

            Log("\nFinished getting staves.");

        }

        internal async Task GetGenericSwordsAsync()
        {

            Log("Getting one handed swords...");

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(GenericSwordsUrl));
            var mainDom = dom.GetElementById("mw-content-text");
            var weapons = mainDom.GetElementsByTagName("tbody");

            var oneHandedSwords = weapons.FirstOrDefault().Children.Where(element => !element.TextContent.Contains("DPSStats"));

            Parallel.ForEach(oneHandedSwords, sword =>
            {

                var statLines = sword.GetElementsByTagName("td");

                var weapon = new GenericWeapon
                {
                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Strength = int.Parse(statLines[2].TextContent),
                    Dexterity = int.Parse(statLines[3].TextContent),
                    Damage = statLines[4].TextContent,
                    APS = double.Parse(statLines[5].TextContent),
                    CritChance = statLines[6].TextContent, //i have no idea it there will always be 2 trailing digits after the .
                    DPS = double.Parse(statLines[7].TextContent),
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = statLines.ElementAtOrDefault(8)?.TextContent ?? "N/A",
                    Type = "One Handed Sword",

                };

                _genericWeapons.Add(weapon);

            });

            Log("\nFinished getting one handed swords.");
            Log("Getting thrusting one handed swords...");

            var thrustingSwords = weapons.ElementAtOrDefault(1).Children.Where(element => !element.TextContent.Contains("DPSStats"));

            Parallel.ForEach(thrustingSwords, sword =>
            {

                var statLines = sword.GetElementsByTagName("td");

                var weapon = new GenericWeapon
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Dexterity = int.Parse(statLines[2].TextContent),
                    Damage = statLines[3].TextContent,
                    APS = double.Parse(statLines[4].TextContent),
                    CritChance = statLines[5].TextContent, //i have no idea it there will always be 2 trailing digits after the .
                    DPS = double.Parse(statLines[6].TextContent),
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = statLines.ElementAtOrDefault(7)?.TextContent ?? "N/A",
                    Type = "Thrusting One Handed Sword",

                };

                _genericWeapons.Add(weapon);

            });

            Log("\nFinished getting thrusting one handed swords.");
            Log("Getting two handed swords...");

            var twoHandedSwords = weapons.ElementAtOrDefault(2).Children.Where(element => !element.TextContent.Contains("DPSStats"));

            Parallel.ForEach(twoHandedSwords, sword =>
            {

                var statLines = sword.GetElementsByTagName("td");

                var weapon = new GenericWeapon
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Strength = int.Parse(statLines[2].TextContent),
                    Dexterity = int.Parse(statLines[3].TextContent),
                    Damage = statLines[4].TextContent,
                    APS = double.Parse(statLines[5].TextContent),
                    CritChance = statLines[6].TextContent, //i have no idea it there will always be 2 trailing digits after the .
                    DPS = double.Parse(statLines[7].TextContent),
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = statLines.ElementAtOrDefault(8)?.TextContent ?? "N/A",
                    Type = "Two Handed Sword",

                };

                _genericWeapons.Add(weapon);

            });

            Log("\nFinished getting two handed swords.");

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
                    APS = double.Parse(statLines[4].TextContent),
                    CritChance = statLines[5].TextContent, //i have no idea it there will always be 2 trailing digits after the .
                    DPS = double.Parse(statLines[6].TextContent),
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = statLines.ElementAtOrDefault(7)?.TextContent ?? "N/A",
                    Type = "Wand",

                };

                _genericWeapons.Add(weapon);

            });

            Log("\nFinished getting wands.");

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
                    Armour = int.Parse(statLines[3].TextContent),
                    BlockChance = isShield ? statLines[4].TextContent : "N/A",
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = isShield ? statLines.ElementAtOrDefault(5).TextContent : statLines.ElementAtOrDefault(4).TextContent,
                    Type = $"{upperCaseSingular}",

                };

                _genericArmours.Add(armour);

            });

            Log($"\nFinished getting armor {lowerCasePlural}.");
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
                    Evasion = int.Parse(statLines[3].TextContent),
                    BlockChance = isShield ? statLines[4].TextContent : "N/A",
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = isShield ? statLines.ElementAtOrDefault(5).TextContent : statLines.ElementAtOrDefault(4).TextContent,
                    Type = $"{upperCaseSingular}",

                };

                _genericArmours.Add(armour);

            });

            Log($"\nFinished getting evasion {lowerCasePlural}.");
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
                    EnergyShield = int.Parse(statLines[3].TextContent),
                    BlockChance = isShield ? statLines[4].TextContent : "N/A",
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = isShield ? statLines.ElementAtOrDefault(5).TextContent : statLines.ElementAtOrDefault(4).TextContent,
                    Type = $"{upperCaseSingular}",

                };

                _genericArmours.Add(armour);

            });

            Log($"\nFinished getting energy shield {lowerCasePlural}.");
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
                    Armour = int.Parse(statLines[4].TextContent),
                    Evasion = int.Parse(statLines[5].TextContent),
                    BlockChance = isShield ? statLines[6].TextContent : "N/A",
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = isShield ? statLines.ElementAtOrDefault(7).TextContent : statLines.ElementAtOrDefault(6).TextContent,
                    Type = $"{upperCaseSingular}",

                };

                _genericArmours.Add(armour);

            });

            Log($"\nFinished getting armour/evasion {lowerCasePlural}.");
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
                    Armour = int.Parse(statLines[4].TextContent),
                    EnergyShield = int.Parse(statLines[5].TextContent),
                    BlockChance = isShield ? statLines[6].TextContent : "N/A",
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = isShield ? statLines.ElementAtOrDefault(7).TextContent : statLines.ElementAtOrDefault(6).TextContent,
                    Type = $"{upperCaseSingular}",

                };

                _genericArmours.Add(armour);

            });

            Log($"\nFinished getting armour/energy shield {lowerCasePlural}.");
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
                    Evasion = int.Parse(statLines[4].TextContent),
                    EnergyShield = int.Parse(statLines[5].TextContent),
                    BlockChance = isShield ? statLines[6].TextContent : "N/A",
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Stats = isShield ? statLines.ElementAtOrDefault(7).TextContent : statLines.ElementAtOrDefault(6).TextContent,
                    Type = $"{upperCaseSingular}",

                };

                _genericArmours.Add(armour);

            });

            Log($"\nFinished getting evasion/energy shield {lowerCasePlural}.");

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

            Log($"\nFinished getting {lowerCasePlural}.");

        }

        internal async Task GetGenericLifeManaFlasks(string url, string upperCaseSingular, string lowerCasePlural, bool IsManaFlasks)
        {

            Log($"Getting {lowerCasePlural}...");

            var dom = await _parser.ParseAsync(await _web.GetStringAsync(url));
            var mainDom = dom.GetElementById("mw-content-text");
            var flasksTable = mainDom.GetElementsByTagName("tbody").FirstOrDefault();

            var flasks = flasksTable.GetElementsByTagName("tr").Where(element => !element.TextContent.Contains("Stats"));

            Parallel.ForEach(flasks, flask =>
            {

                var statLines = flask.GetElementsByTagName("td");

                var item = new GenericFlask
                {

                    Name = statLines.FirstOrDefault().Children.FirstOrDefault().GetElementsByTagName("a").FirstOrDefault().TextContent,
                    LevelReq = int.TryParse(statLines[1].TextContent, out int levelParams) ? levelParams : 0,
                    Life = IsManaFlasks ? 0 : int.Parse(statLines[2].TextContent),
                    Mana = IsManaFlasks ? int.Parse(statLines[2].TextContent) : 0,
                    Duration = int.Parse(statLines[3].TextContent),
                    Usage = int.Parse(statLines[4].TextContent),
                    Capacity = int.Parse(statLines[5].TextContent),
                    ImageUrl = statLines.FirstOrDefault().GetElementsByTagName("img").FirstOrDefault().GetAttribute("src"),
                    Type = $"{upperCaseSingular}",

                };

                _genericFlasks.Add(item);

            });

            Log($"\nFinished getting {lowerCasePlural}.");

        }

        internal void Log(string message)
            => Console.WriteLine(message);

        internal void InLineLog(string message)
            => Console.Write($"\r{message}");

    }

}