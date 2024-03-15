
using Aspose.Cells;
using Microsoft.AspNetCore.Components.Forms;

public interface ITranslatorService
{
    Task<bool> Translate(string languageCode,string blobNameOfUploadedDocument);
    Task CleanInputContainer();

    Task CleanOutputContainer();

    Task DownloadeConvertedFiles();

    Task<bool> Upload(IBrowserFile file, string fileName);

    Task<string> Upload(Workbook file);


}

