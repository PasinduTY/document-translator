
    public interface ISynchronousTranslationService
{
    Task<byte[]> TranslateDocument(byte[] inputDocument, string targetLanguage);
    public string GetFileType(byte[] fileBytes);
    public bool IdentifyJson(byte[] fileBytes);

}

