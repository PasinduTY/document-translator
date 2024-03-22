
using Aspose.Cells;
using Microsoft.AspNetCore.Components.Forms;

public interface ITranslatorService
{
    // methods used in json, excel convertion
    Task<string> ConvertToExcelAsync(IBrowserFile file, string guid);
    Task<string> CombineExcelToJson(MemoryStream memoryStreamOfTranslatedExcelFile, string operationGuid, string guidOfValueExcel);
    Task CreateFolderForOperation(string folderName);

    Task<MemoryStream> GetTheMemoryStreamFromValueExcel(string operationGuid, string uploadedDocumentGuid);


    // methods used in azure ai translator

    Task<bool> UploadDocumentsAsync(MemoryStream memoryStreamOfDocument, string blobName);
    Task<bool> TranslateAsync(string languageCode, string operationGuid);
    Task DownloadConvertedFiles(string operationGuid);

    // methods used in cleaning or resetting 
    Task DeleteKeyAndValueFolders(string operationGuid);
    Task DeleteFilesInInputContainerOfOperation(string operationGuid);
    Task DeleteFilesInOutputContainerOfOperation(string operationGuid);

    Task DeleteTranslatedDocumentFolder(string operationGuid);
    Task DeleteZipFolderInRoot(string operationGuid);


    //  Task CreateZipFromTranslatedDocumentsAsync(string operationGuid);




    // methods to clean all documents in containers. these are not used in UI. 
    Task CleanInputContainer();

    Task CleanOutputContainer();

}

