﻿namespace ResXManager.Model
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using ResXManager.Infrastructure;

    using TomsToolbox.Essentials;

    using JsonConvert = Newtonsoft.Json.JsonConvert;

    [Export(typeof(IService))]
    class WebFilesExporter : IService
    {
        private readonly ResourceManager _resourceManager;
        private readonly ITracer _tracer;

        [ImportingConstructor]
        public WebFilesExporter(ResourceManager resourceManager, ITracer tracer)
        {
            _resourceManager = resourceManager;
            _tracer = tracer;
            _resourceManager.ProjectFileSaved += (_, __) => Export();
            _resourceManager.Loaded += (_, __) => Export();
        }

        public void Start()
        {
        }

        private void Export()
        {
            try
            {
                var solutionFolder = _resourceManager.SolutionFolder;
                if (solutionFolder == null)
                    return;

                var configFilePath = Path.Combine(solutionFolder, "resx-manager.webexport.config");
                if (!File.Exists(configFilePath))
                    return;

                var config = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(configFilePath));

                var typeScriptFileDir = config.TypeScriptFileDir;
                var jsonFileDir = config.JsonFileDir;

                if (string.IsNullOrEmpty(typeScriptFileDir) || string.IsNullOrEmpty(jsonFileDir))
                    return;

                typeScriptFileDir = Directory.CreateDirectory(Path.Combine(solutionFolder, typeScriptFileDir)).FullName;
                jsonFileDir = Directory.CreateDirectory(Path.Combine(solutionFolder, jsonFileDir)).FullName;

                var typescript = new StringBuilder(TypescriptFileHeader);
                var jsonObjects = new Dictionary<CultureKey, JObject>();

                foreach (var entity in _resourceManager.ResourceEntities)
                {
                    var formatTemplates = new HashSet<string>();

                    var entityName = entity.BaseName;
                    var neutralLanguage = entity.Languages.FirstOrDefault();

                    typescript.AppendLine($@"export class {entityName} {{");

                    foreach (var node in neutralLanguage.GetNodes())
                    {
                        AppendTypescript(node, typescript, formatTemplates);
                    }

                    typescript.AppendLine(@"}");
                    typescript.AppendLine();

                    foreach (var language in entity.Languages.Skip(config.ExportNeutralJson ? 0 : 1))
                    {
                        var node = new JObject();

                        foreach (var resourceNode in language.GetNodes())
                        {
                            var key = resourceNode.Key;
                            if (formatTemplates.Contains(key))
                            {
                                key += FormatTemplateSuffix;
                            }

                            node.Add(key, JToken.FromObject(resourceNode.Text));
                        }

                        jsonObjects
                            .ForceValue(language.CultureKey, _ => GenerateJsonObjectWithComment())?
                            .Add(entityName, node);
                    }
                }

                var typeScriptFilePath = Path.Combine(typeScriptFileDir, "resources.ts");
                File.WriteAllText(typeScriptFilePath, typescript.ToString());

                foreach (var jsonObjectEntry in jsonObjects)
                {
                    var key = jsonObjectEntry.Key;
                    var value = jsonObjectEntry.Value.ToString();

                    var jsonFilePath = Path.Combine(jsonFileDir, $"resources{key}.json");
                    File.WriteAllText(jsonFilePath, value);
                }
            }
            catch (Exception ex)
            {
                _tracer.TraceError(ex.ToString());
            }
        }

        private const string FormatTemplateSuffix = @"_TEMPLATE";
        private static readonly Regex _formatPlaceholderExpression = new Regex(@"\$\{\s*(\w[.\w\d_]*)\s*\}");

        private static void AppendTypescript(ResourceNode node, StringBuilder typescript, HashSet<string> formatTemplates)
        {
            var text = node.Text ?? string.Empty;
            var key = node.Key;
            var value = JsonConvert.SerializeObject(text);

            var placeholders = _formatPlaceholderExpression.Matches(text)
                .OfType<Match>()
                .Select(m => m.Groups[1].Value)
                .Distinct()
                .ToList();

            if (placeholders.Any())
            {
                formatTemplates.Add(key);

                var args = string.Join(", ", placeholders.Select(item => $@"{item}: string"));
                typescript.AppendLine($"  private {key}{FormatTemplateSuffix} = {value};");
                typescript.AppendLine($"  {key} = (args: {{ {args} }}) => {{\r\n    return formatString(this.{key}{FormatTemplateSuffix}, args);\r\n  }}");
            }
            else
            {
                typescript.AppendLine($@"  {key} = {value};");
            }
        }

        private class Configuration
        {
            [JsonProperty("typeScriptFileDir")]
            public string TypeScriptFileDir { get; set; }
            [JsonProperty("jsonFileDir")]
            public string JsonFileDir { get; set; }
            [JsonProperty("exportNeutralJson")]
            public bool ExportNeutralJson { get; set; }
        }

        private static JObject GenerateJsonObjectWithComment()
        {
            var value = new JObject();
            value.Add("_comment", JToken.FromObject("Auto-generated; do not modify!"));
            return value;
        }

        private const string TypescriptFileHeader =
            @"/*
 * This code was generated by ResXResourceManager.
 * Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
 */

/* tslint:disable */

function formatString(template: string, args: any): string {
  let result = template;
  Object.entries(args).forEach(entry => {
    const key = entry[0];
    const replacement = `${entry[1]}`;
    const pattern = new RegExp('\\$\\{\\s*' + key + '\\s*\\}', 'gm');
    result = result.replace(pattern, replacement);
  });
  return result;
}

";
    }
}
