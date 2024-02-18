using Scriban;
using Gaos.Dbo;
using Serilog;
using Gaos.Lang;

namespace Gaos.Templates
{
    public class TemplateService
    {
        public static string CLASS_NAME = typeof(TemplateService).Name;

        private IConfiguration Configuration;
        private Db db;
        private LanguageService languageService;

        private string TEMPLATES_FDOLDER_PATH;

        private static Dictionary<string, Template> templateCache = new Dictionary<string, Template>();

        public TemplateService(IConfiguration configuration, Db db, LanguageService languageService)
        {
            this.Configuration = configuration;
            this.db = db;
            this.languageService = languageService;

            if (Configuration["templates_fdolder_path"] == null)
            {
                throw new Exception("missing configuration value: templates_fdolder_path");
            }
            TEMPLATES_FDOLDER_PATH = Configuration["templates_fdolder_path"] ?? throw new InvalidOperationException();
        }

        private string getTemplateFullPath(string path)
        {
            string templateFullPath;
            Language language = languageService.GetLanguage();


            if (language == Language.english)
            {
                templateFullPath = TEMPLATES_FDOLDER_PATH + "/" + path + ".sbn";
            }
            else if (language == Language.russian)
            {
                templateFullPath = TEMPLATES_FDOLDER_PATH + "/" + path + "_ru" + ".sbn";
            }
            else if (language == Language.chinese)
            {
                templateFullPath = TEMPLATES_FDOLDER_PATH + "/" + path + "_zh" + ".sbn";
            }
            else if (language == Language.slovak)
            {
                templateFullPath = TEMPLATES_FDOLDER_PATH + "/" + path + "_sk" + ".sbn";
            }
            else
            {
                templateFullPath = TEMPLATES_FDOLDER_PATH + "/" + path + ".sbn";
            }

            return templateFullPath;
        }

        public Template ParseFile(string path, bool isUseCache = false)
        {
            const string METHOD_NAME = "ParseFile";

            if (isUseCache)
            {
                if (templateCache.ContainsKey(path))
                {
                    return templateCache[path];
                }
            }

            string fullPath = getTemplateFullPath(path);
            Template template = Template.Parse(System.IO.File.ReadAllText(fullPath));

            // Report any errors
            if (template.HasErrors)
            {
                foreach (var message in template.Messages)
                {
                    Console.WriteLine(message);
                }

                Log.Error($"{CLASS_NAME}:{METHOD_NAME}: error parsing tewmplete: {path}");
            }

            if (isUseCache)
            {
                templateCache.Add(path, template);
                
            }

            return template;
        }
    }
}
