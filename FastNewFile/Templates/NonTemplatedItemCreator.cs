using EnvDTE;
using FastNewFile.Services;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;

namespace FastNewFile.Templates
{
    class NonTemplatedItemCreator : IItemCreator
    {
        private readonly string[] _relativePath;
        private readonly string _folder;

        private readonly ProjectService _projectService;

        public NonTemplatedItemCreator(ProjectService projectService, string rootFolder, string[] relativePath)
        {
            _relativePath = relativePath;
            _folder = rootFolder;
            _projectService = projectService;
        }

        public ItemInfo Create(Project project)
        {
            string file = Path.Combine(_folder, Path.Combine(_relativePath));
            string dir = Path.GetDirectoryName(file);

            try
            {
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }
            catch (Exception)
            {
                MessageBox.Show("Error creating the folder: " + dir);
                return ItemInfo.Empty;
            }

            if (!File.Exists(file))
            {
                int position = WriteFile(file);

                try
                {
                    if (AddFileToActiveProject(project, file))
                    {
                        EnvDTE.Window window = _projectService.OpenFile(file);

                        // Move cursor into position
                        if (position > 0)
                        {
                            TextSelection selection = (TextSelection)window.Selection;
                            selection.CharRight(Count: position - 1);
                        }

                        return new ItemInfo() {
                            Extension = Path.GetExtension(file),
                            FileName = file
                        };
                    }

                }
                catch { /* Something went wrong. What should we do about it? */ }
            }
            else
            {
                MessageBox.Show($"The file '{file}' already exist.");
            }
            return ItemInfo.Empty;
        }

        private bool AddFileToActiveProject(Project project, string fileName)
        {
            if (project == null || project.Kind == "{8BB2217D-0F2D-49D1-97BC-3654ED321F3B}") // ASP.NET 5 projects
                return false;

            string projectDirPath = _projectService.GetProjectRoot();

            if (!fileName.StartsWith(projectDirPath, StringComparison.OrdinalIgnoreCase))
                return false;

            var pi = project.ProjectItems.AddFromFile(fileName);
            if (fileName.EndsWith("__dummy__"))
            {
                pi.Delete();
                return false;
            }
            else
            {
                return true;
            }
        }

        private int WriteFile(string file)
        {
            Encoding encoding = new UTF8Encoding(true);
            string extension = Path.GetExtension(file);

            string assembly = Assembly.GetExecutingAssembly().Location;
            string folder = Path.GetDirectoryName(assembly).ToLowerInvariant();
            string template = Path.Combine(folder, "Templates\\", extension);

            if (File.Exists(template))
            {
                string content = File.ReadAllText(template);
                int index = content.IndexOf('$');
                content = content.Remove(index, 1);
                File.WriteAllText(file, content, encoding);
                return index;
            }

            File.WriteAllText(file, string.Empty, encoding);
            return 0;
        }
    }
}
