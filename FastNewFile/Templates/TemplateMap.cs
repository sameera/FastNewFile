namespace FastNewFile.Templates
{
    using FastNewFile.Services;
    using System.Collections.Generic;
    using M = TemplateMapping;

    /// <summary>
    /// Map of input patterns and their corresponding VS Templates. Ideally, these patterns would be configurable via the 
    /// Visual Studio Options dialog box.
    /// </summary>
    class TemplateMap : List<TemplateMapping>
    {
        internal PackageService _packageService { get; }

        /// <summary>
        /// Loads the default mappings.
        /// </summary>
        public void LoadDefaultMappings()
        {
            this.Add(new M(@"^c\:(?<name>.*)", "CSharp", "Class"));
            this.Add(new M(@"^i\:(?<name>.*)", "CSharp", "Interface"));
            this.Add(new M(@"^(?<name>I[A-Z].*)\.cs$", "CSharp", "Interface"));
            this.Add(new M(@"^(?<name>I[A-Z].*)\.vb$", "VisualBasic", "Interface"));
            /* NOTE: With VS 2015 Community Edition, CodeFile.cs was not replacing the $rootnamespace$ tag.
             * If this happens, open the CodeFile.vstemplate file 
             * (e.g. in C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\ItemTemplates\CSharp\Code\1033\CodeFile\CodeFile.vstemplate)
             * and replace
             
              <TemplateContent>
                <ProjectItem>CodeFile.cs</ProjectItem>
              </TemplateContent>

            * with

              <TemplateContent>
                <ProjectItem ReplaceParameters="true">CodeFile.cs</ProjectItem>
              </TemplateContent>

            */
            this.Add(new M(@"^e:(?<name>.*)\.cs", "CSharp", "CodeFile"));
            // Following mappings are the broadest from a language POV. So, they need to be at the end of the mappings
            this.Add(new M(@"^(?<name>.*)\.cs$", "CSharp", "Class"));
            this.Add(new M(@"^(?<name>.*)\.js$", "JavaScript", "JScript"));
        }
    }
}
