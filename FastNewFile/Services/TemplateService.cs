using FastNewFile.Templates;
using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace FastNewFile.Services
{
    class TemplateService
    {
        private static TemplateMap _templates;
        private readonly object _templateLock = new object();

        private readonly PackageService _packageService;

        public TemplateService(PackageService packageService)
        {
            _packageService = packageService;
        }

        public TemplateMap GetTemplates()
        {
            TemplateMap templates = null;
            if (_templates == null)
                lock (_templateLock)
                    if (_templates == null)
                    {
                        string path = Path.Combine(
                                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                        "VisualStudio.FastNewFile",
                                        "Patterns.json");
                        if ((templates = this.LoadFromFile(path)) == null)
                        {
                            templates = new TemplateMap();
                            templates.LoadDefaultMappings();
                            this.WriteToFile(templates, path);
                        }
                        _templates = templates;
                    }
            return _templates;
        }

        public string GetItemTemplate(string templateName, string language)
        {
            return _packageService.GetSolution().GetProjectItemTemplate(templateName, language);
        }

        private TemplateMap LoadFromFile(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    using (var fileStream = File.OpenRead(path))
                    {
                        var deserializer = new DataContractJsonSerializer(typeof(TemplateMap));
                        return deserializer.ReadObject(fileStream) as TemplateMap;
                    }
                }
                catch (Exception e)
                {
                    _packageService.LogToOutputPane(string.Concat(
                        "There was error loading the mappings from: ",
                        path,
                        "\r\n",
                        e.Message));
                }
            }
            return null;
        }

        private void WriteToFile(TemplateMap map, string path)
        {
            try
            {
                using (var stream = new MemoryStream())
                {
                    var serizlier = new DataContractJsonSerializer(
                                        typeof(TemplateMap),
                                        new DataContractJsonSerializerSettings() {
                                            UseSimpleDictionaryFormat = true,
                                        });
                    serizlier.WriteObject(stream, map);

                    stream.Position = 0;
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllBytes(path, stream.ToArray());
                }
            }
            catch (System.Exception e)
            {
                _packageService.LogToOutputPane(string.Concat(
                    "Could not save the mapping file to: ",
                    path,
                    "\r\n",
                    e.Message));

            }
        }

        public TemplateMapping GetTemplate(string itemName, out string suggestedFileName)
        {
            Match match;
            foreach (var mapping in GetTemplates())
                if ((match = mapping.GetPatternExpression().Match(itemName)).Success && match.Groups.Count > 0)
                {
                    suggestedFileName = match.Groups["name"].Value;
                    return mapping;
                }

            suggestedFileName = null;
            return null;
        }
    }
}
