using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Linq;

namespace FastNewFile.Services
{
    class PackageService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly DTE2 _dte;

        public Project GetActiveProject()
        {
            try
            {
                if (_dte.ActiveSolutionProjects is Array activeSolutionProjects && activeSolutionProjects.Length > 0)
                {
                    return activeSolutionProjects.GetValue(0) as Project;
                }
            }
            catch (Exception)
            {
                // Pass through and return null
            }

            return null;
        }

        public PackageService(Package package)
        {
            _serviceProvider = package;
            _dte = _serviceProvider.GetService(typeof(DTE)) as DTE2;
        }

        public object GetService(Type type)
        {
            return _serviceProvider.GetService(type);
        }

        public UIHierarchyItem GetSelectedItem()
        {
            var items = (Array)_dte.ToolWindows.SolutionExplorer.SelectedItems;
            return items.OfType<UIHierarchyItem>().FirstOrDefault();
        }

        public void LogToOutputPane(string message)
        {
            EnvDTE.Window window = _dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);
            OutputWindow outputWindow = (OutputWindow)window.Object;
            var outputPane = outputWindow.OutputWindowPanes.Cast<OutputWindowPane>().FirstOrDefault(p => p.Name == "Debug");
            if (outputPane != null)
            {
                outputPane.Activate();
                outputPane.OutputString(message);
            }
        }

        public Window OpenFile(string file)
        {
            return _dte.ItemOperations.OpenFile(file);
        }

        public Solution2 GetSolution()
        {
            return (Solution2)_dte.Solution;
        }
    }
}
