using EnvDTE;
using EnvDTE80;
using FastNewFile.Templates;
using System;
using System.IO;
using System.Windows;

namespace FastNewFile.Services
{
    class ProjectService
    {
        private readonly Project _project;
        private readonly PackageService _packageService;
        private readonly TemplateService _templateService;

        private readonly string _projectRoot;

        private string _defaultExtensionByProjectType;

        public string OverridingExtension { get; private set; }
        public string LastUsedExtension { get; private set; }

        public ProjectService(Project project, PackageService packageService, TemplateService templateService)
        {
            // If the selected item on the Solution Explorer is a Solution Folder, some of the properties of 'project'
            // will be null or empty. Examples are CodeModel and FullName both used below.

            _project = project;
            _defaultExtensionByProjectType = string.Empty;

            switch (_project.CodeModel?.Language)
            {
                case CodeModelLanguageConstants.vsCMLanguageCSharp:
                    _defaultExtensionByProjectType = ".cs";
                    break;
                case CodeModelLanguageConstants.vsCMLanguageVB:
                    _defaultExtensionByProjectType = ".vb";
                    break;
            }

            _packageService = packageService;
            _templateService = templateService;

            bool wasRetreived = this.TryGetProperty("FullPath", out Property prop)
                        // MFC projects don't have FullPath, and there seems to be no way to query existence
                        || this.TryGetProperty("ProjectDirectory", out prop)
                        || this.TryGetProperty("ProjectPath", out prop);

            if (wasRetreived)
            {
                _projectRoot = prop.Value.ToString();
            }
            else
            {
                _projectRoot = Path.GetDirectoryName(_project.DTE.Solution.FullName);
            }
        }

        public string GetProjectDefaultExtension()
        {
            // On certain projects (e.g. a project started with File > Add Existing Web site..) 
            // Code Model is null.
            if (this.OverridingExtension != null)
            {
                return OverridingExtension;
            }
            /* TODO
            else if (_lastUsedExtension == string.Empty)
            {
                // Indicates that this is the first file we are creating.
               //_overridingExtension = // TODO: Load from project specific storage
            }
            */
            else if (LastUsedExtension != null)
            {
                return LastUsedExtension;
            }
            else
            {
                return _defaultExtensionByProjectType;
            }
        }

        public EnvDTE.Window OpenFile(string file)
        {
            return _packageService.OpenFile(file);
        }

        public void SelectCurrentItem()
        {
            var dte = _project.DTE;
            if (dte.Version == "11.0") // This errors in VS2012 for some reason.
                return;

            System.Threading.ThreadPool.QueueUserWorkItem((o) => {
                try
                {
                    dte.ExecuteCommand("View.TrackActivityInSolutionExplorer");
                    dte.ExecuteCommand("View.TrackActivityInSolutionExplorer");
                }
                catch { /* Ignore any exceptions */ }
            });
        }

        public string GetProjectRoot()
        {
            return _projectRoot;
            //bool wasRetreived = this.TryGetProperty("FullPath", out Property prop)
            //            // MFC projects don't have FullPath, and there seems to be no way to query existence
            //            || this.TryGetProperty("ProjectDirectory", out prop)
            //            || this.TryGetProperty("ProjectPath", out prop);

            //if (wasRetreived)
            //{
            //    return prop.Value.ToString();
            //}
            //else
            //{
            //    return Path.GetDirectoryName(_project.DTE.Solution.FullName);
            //}
        }

        private bool TryGetProperty(string propertyName, out Property property)
        {
            try
            {
                property = _project.Properties.Item(propertyName);
                return true;
            }
            catch (ArgumentException)
            {
                property = null;
                return false;
            }
        }

        public string GetParentFolder(UIHierarchyItem item)
        {
            DTE _dte = _project.DTE;
            if (_dte.ActiveWindow is Window2 window && window.Type == vsWindowType.vsWindowTypeDocument)
            {
                // if a document is active, use the document's containing directory
                Document doc = _dte.ActiveDocument;
                if (doc != null && !string.IsNullOrEmpty(doc.FullName))
                {
                    ProjectItem docItem = _dte.Solution.FindProjectItem(doc.FullName);

                    if (docItem != null)
                    {
                        string fileName = docItem.Properties.Item("FullPath").Value.ToString();
                        if (File.Exists(fileName))
                            return Path.GetDirectoryName(fileName);
                    }
                }
            }

            string folder = null;

            Project project = item.Object as Project;

            if (item.Object is ProjectItem projectItem)
            {
                string fileName = projectItem.FileNames[0];

                if (File.Exists(fileName))
                {
                    folder = Path.GetDirectoryName(fileName);
                }
                else
                {
                    folder = fileName;
                }
            }
            else if (project != null)
            {
                //string prop = GetProjectRoot();

                //if (prop != null)
                //{
                //    string value = prop.Value.ToString();

                //    if (File.Exists(value))
                //    {
                //        folder = Path.GetDirectoryName(value);
                //    }
                //    else if (Directory.Exists(value))
                //    {
                //        folder = value;
                //    }
                //}
                folder = _projectRoot;
            }
            return folder;
        }

        public bool TryGetRelativePath(string path, out string relativePath)
        {
            if (path.StartsWith(_projectRoot, StringComparison.OrdinalIgnoreCase) && path.Length > _projectRoot.Length)
            {
                relativePath = path.Substring(_projectRoot.Length + 1);
                return true;
            }
            else
            {
                relativePath = null;
                return false;
            }
        }

        private IItemCreator GetCreator(string rootFolder, string relativePath)
        {
            var path = relativePath.Split(Path.DirectorySeparatorChar);
            var template = _templateService.GetTemplate(path[path.Length - 1], out string fileName);

            if (template != null)
            {
                path[path.Length - 1] = fileName;
                return new TemplatedItemCreator(_templateService, template, path);
            }
            else
            {
                return new NonTemplatedItemCreator(this, rootFolder, path);
            }
        }

        public void AddItem(string item)
        {
            try
            {
                var creator = this.GetCreator(_projectRoot, item);
                var info = creator.Create(_project);

                SelectCurrentItem();
                
                // If the same extension was used continuously, make it the default extension
                if (info != ItemInfo.Empty && LastUsedExtension != _defaultExtensionByProjectType && info.Extension == LastUsedExtension)
                {
                    // TODO: Save extension to project-specific storage
                    OverridingExtension = info.Extension;
                }
                LastUsedExtension = info.Extension;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Cannot Add New File", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
