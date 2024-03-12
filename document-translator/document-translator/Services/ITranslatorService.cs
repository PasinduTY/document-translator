
using Microsoft.AspNetCore.Components.Forms;

public interface ITranslatorService
{
    Task<bool> Translate(string languageCode);
    Task CleanInputContainer();

    Task CleanOutputContainer();

    Task DownloadeConvertedFiles();

    Task<bool> Upload(IBrowserFile file, string fileName);


}

