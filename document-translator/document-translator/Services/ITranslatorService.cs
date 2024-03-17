
using Aspose.Cells;
using Microsoft.AspNetCore.Components.Forms;

public interface ITranslatorService
{
    Task<bool> Translate(string languageCode,string operationGuid);
    Task CleanInputContainer();

    Task CleanOutputContainer();

    Task DownloadConvertedFiles(string operationGuid, IConverterService iconverterService);

  //  Task<bool> Upload(IBrowserFile file, string fileName);

  //  Task<string> Upload(Workbook file);

    Task  <bool> UploadDocuments(MemoryStream memoryStreamOfDocument, string blobName);


}

