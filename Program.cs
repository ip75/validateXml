using System;
using CommandLine;
using CommandLine.Text;
using Serilog;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Schema;

namespace validateXml
{
    internal class Program
    {
        public class Options
        {
            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
            public bool Verbose { get; set; }
            [Option('s', "schemes", Default = ".", Required = false, HelpText = "Directory where xsd files located.")]
            public string XsdPath { get; set; }
            [Option('x', "xml", Default = ".", Required = false, HelpText = "Directory where xml files located.")]
            public string XmlPath { get; set; }
        }
        private static void Main(string[] args)
        {
            var logger = new LoggerConfiguration()
                .WriteTo.ColoredConsole()
                .CreateLogger();

            var errors = new List<Error>();
            var result = Parser.Default.ParseArguments<Options>(args)
                .WithParsed((options) =>
                {
                    var schemas = new XmlSchemaSet();
                    var xsdFiles = Directory.EnumerateFiles(options.XsdPath, "*.xsd");

                    foreach (var xsd in xsdFiles)
                    {
                        logger.Information($"add {xsd} file to schema set");
                        schemas.Add("", xsd);
                    }

                    var xmlFiles = Directory.EnumerateFiles(options.XmlPath, "*.xml");

                    try
                    {
                        foreach (var xml in xmlFiles)
                        {
                            logger.Information($"validating {xml} file...");
                            var xmlDoc = XDocument.Load(xml);
                            xmlDoc.Validate(schemas, (validatedObject, validationEventArgs) =>
                            {
                                logger.Error($"{validationEventArgs.Message}");
                            });
                        }
                    }
                    catch (Exception exception)
                    {
                        logger.Error($"error while validation: {exception.Message}");
                    }
                })
                .WithNotParsed(err => { errors = err.ToList(); });

            if (errors.Any())
            {
                logger.Error(HelpText.AutoBuild(result));
            }

            logger.Information("Validation finished");
        }
    }
}
