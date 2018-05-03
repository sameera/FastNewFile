using FastNewFile.Templates;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.IO;

namespace FastNewFile.Services
{
    class NewFileBuilder
    {
        public const string DummyFileName = "__dummy__";
        private readonly PackageService _packageService;
        private readonly TemplateService _templateSerivice;

        public NewFileBuilder(Package package)
        {
            _packageService = new PackageService(package);
            _templateSerivice = new TemplateService(_packageService);
        }

        public void Build()
        {
            var item = _packageService.GetSelectedItem();
            if (item == null) return;

            var project = _packageService.GetActiveProject();
            if (project == null) return;

            var projectService = new ProjectService(project, _packageService, _templateSerivice);

            string folder = projectService.GetParentFolder(item);
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return;

            string defaultExt = projectService.GetProjectDefaultExtension();
            string input = PromptForFileName(
                                folder,
                                defaultExt
                            ).TrimStart('/', '\\').Replace("/", "\\");

            if (string.IsNullOrEmpty(input))
                return;
            else if (input.EndsWith("\\"))
            {
                input = input + DummyFileName;
            }

            TemplateMap templates = _templateSerivice.GetTemplates();

            if (projectService.TryGetRelativePath(folder, out string relativePath))
            {
                relativePath = CombinePaths(relativePath, input);
                // I'm intentionally avoiding the use of Path.Combine because input may contain pattern characters
                // such as ':' which will cause Path.Combine to handle differently. We simply need a string concat here
            }
            else
            {
                relativePath = input;
            }

            projectService.AddItem(relativePath);
        }

        private string PromptForFileName(string folder, string defaultExt)
        {
            DirectoryInfo dir = new DirectoryInfo(folder);

            IVsUIShell uiShell = (IVsUIShell)_packageService.GetService(typeof(SVsUIShell));

            var dialog = new FileNameDialog(uiShell, dir.Name, defaultExt);
            //get the owner of this dialog  
            uiShell.GetDialogOwnerHwnd(out IntPtr hwnd);
            dialog.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            uiShell.EnableModeless(0);
            try
            {
                WindowHelper.ShowModal(dialog, hwnd);
                return (dialog.DialogResult.HasValue && dialog.DialogResult.Value) ? dialog.Input : string.Empty;
            }
            finally
            {
                // This will take place after the window is closed.  
                uiShell.EnableModeless(1);
            }
        }

        private static string CombinePaths(string path1, string path2)
        {
            if (path1.Length == 0)
            {
                return path2;
            }
            else if (path1.EndsWith("\\"))
            {
                return string.Concat(path1, path2);
            }
            else
            {
                return string.Concat(path1, "\\", path2);
            }
        }
    }
}
