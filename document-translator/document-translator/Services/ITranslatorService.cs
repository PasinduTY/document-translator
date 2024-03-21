
using Aspose.Cells;
using Microsoft.AspNetCore.Components.Forms;

public interface ITranslatorService
{
    Task<bool> TranslateAsync(string languageCode, string operationGuid);
    Task CleanInputContainer();

    Task CleanOutputContainer();

    Task DownloadConvertedFiles(string operationGuid);

    //  Task<bool> Upload(IBrowserFile file, string fileName);

    //  Task<string> Upload(Workbook file);

    Task<bool> UploadDocumentsAsync(MemoryStream memoryStreamOfDocument, string blobName);

    //  Task CreateZipFromTranslatedDocumentsAsync(string operationGuid);


}

