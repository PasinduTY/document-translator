
    public interface ISynchronousTranslationService
{
     Task TranslateDocument(string inputFilePath, string outputFilePath, String targetLanguage);
}

