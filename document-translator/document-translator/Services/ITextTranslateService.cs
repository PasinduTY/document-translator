
    public interface ITextTranslateService
{
    Task<string> TextTranslator(string textToTranslate, string targetLanguage);
}

