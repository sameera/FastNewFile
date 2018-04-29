namespace FastNewFile.Templates
{
    using EnvDTE;
    using FastNewFile.Services;
    using System;
    using System.Linq;
    using System.Xml.Linq;

    class TemplatedItemCreator : IItemCreator
    {
        private readonly TemplateMapping _template;
        private readonly string[] _relativePath;

        private readonly TemplateService _templateService;

        public TemplatedItemCreator(
                TemplateService templateService, 
                TemplateMapping template, 
                string[] relativePath)
        {
            _templateService = templateService;
            _template = template;
            _relativePath = relativePath;
        }

        public ItemInfo Create(Project project)
        {
            string templatePath = _templateService.GetItemTemplate(_template.TemplateName, _template.Language);
            string ext = GetTargetExtension(templatePath);

            string itemName = _relativePath[_relativePath.Length - 1];
            if (!itemName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                itemName += ext;

            var parent = GetItemParent(project);
            parent.AddFromTemplate(templatePath, itemName);

            return new ItemInfo() {
                Extension = ext,
                FileName = itemName
            };
        }

        private ProjectItems GetItemParent(Project project)
        {
            var parent = project.ProjectItems;
            ProjectItem existingItem(string name, ProjectItems items) => items
                                                        .Cast<ProjectItem>()
                                                        .FirstOrDefault(p => p.Name == name);

            for (int i = 0; i < _relativePath.Length - 1; i++)
            {
                var p = existingItem(_relativePath[i], parent);
                if (p == null)
                    parent = parent.AddFolder(_relativePath[i]).ProjectItems;
                else
                    parent = p.ProjectItems;
            }
            return parent;
        }

        private string GetTargetExtension(string templateFile)
        {
            var doc = XDocument.Load(templateFile);
            XNamespace ns = "http://schemas.microsoft.com/developer/vstemplate/2005";
            string defaultName = doc.Document.Descendants(ns + "TemplateData")
                                    .First()
                                    .Descendants(ns + "DefaultName")
                                    .First()
                                    .Value;
            return System.IO.Path.GetExtension(defaultName);

        }
    }
}
