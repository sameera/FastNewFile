﻿using EnvDTE;
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

        private readonly string _projectPath;

        private string _defaultExtensionByProjectType;

        public string OverridingExtension { get; private set; }
        public string LastUsedExtension { get; private set; }

        public ProjectService(Project project, PackageService packageService, TemplateService templateService)
        {
            _project = project;

            switch (_project.CodeModel.Language)
            {
                case CodeModelLanguageConstants.vsCMLanguageCSharp:
                    _defaultExtensionByProjectType = ".cs";
                    break;
                case CodeModelLanguageConstants.vsCMLanguageVB:
                    _defaultExtensionByProjectType = ".vb";
                    break;
                default:
                    _defaultExtensionByProjectType = string.Empty;
                    break;
            }

            _packageService = packageService;
            _templateService = templateService;

            _projectPath = Path.GetDirectoryName(project.FullName);
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

        public Property GetProjectRoot()
        {
            Property prop;

            try
            {
                prop = _project.Properties.Item("FullPath");
            }
            catch (ArgumentException)
            {
                try
                {
                    // MFC projects don't have FullPath, and there seems to be no way to query existence
                    prop = _project.Properties.Item("ProjectDirectory");
                }
                catch (ArgumentException)
                {
                    // Installer projects have a ProjectPath.
                    prop = _project.Properties.Item("ProjectPath");
                }
            }

            return prop;
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
                Property prop = GetProjectRoot();

                if (prop != null)
                {
                    string value = prop.Value.ToString();

                    if (File.Exists(value))
                    {
                        folder = Path.GetDirectoryName(value);
                    }
                    else if (Directory.Exists(value))
                    {
                        folder = value;
                    }
                }
            }
            return folder;
        }

        public bool TryGetRelativePath(string path, out string relativePath)
        {
            if (path.StartsWith(_projectPath, StringComparison.OrdinalIgnoreCase) && path.Length > _projectPath.Length)
            {
                relativePath = path.Substring(_projectPath.Length + 1);
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
                var creator = this.GetCreator(_projectPath, item);
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