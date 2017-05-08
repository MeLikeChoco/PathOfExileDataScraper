using AngleSharp.Parser.Html;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PathOfExileDataScraper
{

    internal class InfoScraper
    {

        internal SqliteConnection _connection;
        internal HttpClient _web;
        internal HtmlParser _parser;
        internal Stopwatch _stopwatch;

        internal async Task Run(SqliteConnection connectionParams, HttpClient webParams, HtmlParser parserParams, Stopwatch stopWatchParams)
        {

            _connection = connectionParams;
            _web = webParams;
            _parser = parserParams;
            _stopwatch = stopWatchParams;



        }

    }

}
