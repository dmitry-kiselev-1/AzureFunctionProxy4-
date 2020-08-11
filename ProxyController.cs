﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AzureFunctionProxy4
{
    internal class ProxyController
    {
        /// <summary>
        /// Proxy for selected site
        /// Xpath_syntax: https://www.w3schools.com/xml/xpath_syntax.asp
        /// </summary>
        //[HttpGet("{p1?}/{p2?}/{p3?}/{p4?}/{p5?}/{p6?}/{p7?}/{p8?}/{p9?}/{p10?}/{p11?}/{p12?}/{p13?}/{p14?}/{p15?}/{p16?}")]
        public async Task<IActionResult> Get(string requestPath)
        {
            var injection = "™"; // (char)0174

            var sourceDomain = @"https://habr.com/ru/";

            var localDestinationDomain = @"http://localhost:7071/api/Proxy/";
            var publishDestinationDomain = @"https://websiteproxy4.azurewebsites.net/proxy/";

            var destinationDomain = localDestinationDomain;

            var destinationPath = requestPath;
            var destination = sourceDomain[..8] + (sourceDomain[8..] + destinationPath?[10..])
                .Replace("//", "/");

            var web = new HtmlWeb();
            var doc = await web.LoadFromWebAsync(destination);

            // select all href attributes in the document
            foreach (var linkNode in doc.DocumentNode.SelectNodes("//a[@href]"))
            {
                var linkAttribute = linkNode.Attributes["href"];

                // absolute links
                if (linkAttribute.Value.Contains(sourceDomain))
                {
                    linkAttribute.Value = linkAttribute.Value.Replace(sourceDomain, destinationDomain);
                    Console.WriteLine($"absolute link:  {linkAttribute.Value}");
                }

                // relative links
                if (linkAttribute.Value.StartsWith("/"))
                {
                    linkAttribute.Value = destinationDomain[..8] + (destinationDomain[8..] + linkAttribute.Value)
                        .Replace("//", "/");
                    Console.WriteLine($"relative link: {linkAttribute.Value}");
                }
            }

            // select all elements in the document
            foreach (var textNode in doc.DocumentNode.SelectNodes("*//descendant-or-self::text()"))
            {
                if(textNode.InnerText.Length == 0) continue;

                var words = textNode.InnerText.Split(' ');
                var wordsModified = String.Join(" ",
                    words.Select(word => word.Length == 6 ? $"{word}{injection}" : word)
                );

                textNode.InnerHtml = wordsModified;
            }

            return new ContentResult() {
                Content = doc.DocumentNode.OuterHtml,
                ContentType = "text/html",
                StatusCode = 200 };
        }
    }
}