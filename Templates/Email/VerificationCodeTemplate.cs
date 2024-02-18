using Scriban;
using Scriban.Runtime;
using Gaos.Templates;
using Gaos.Lang;

namespace Gaos.Templates.Email
{
    public class VerificationCodeTemplate
    {
        public static string CLASS_NAME = typeof(VerificationCodeTemplate).Name;

        public static string GenerateVerificationCodeEmailTemplate(TemplateService templateSerivice, string verificationCode)
        {
            var scriptObject1 = new ScriptObject();
            scriptObject1.Add("verificationCode", verificationCode);

            var context = new TemplateContext();
            context.PushGlobal(scriptObject1);

            var template = templateSerivice.ParseFile("Email/VerificationCodeTemplate", true);

            var result = template.Render(context);


            return result;
        }
    }
}
