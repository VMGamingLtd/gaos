namespace Gaos.Lang
{
    public enum Language
    {
        english,
        russian,
        chinese,
        slovak

    }

    public class LanguageService
    {
        public Language GetLanguage()
        {
            return Language.english;
        }
    }
}
