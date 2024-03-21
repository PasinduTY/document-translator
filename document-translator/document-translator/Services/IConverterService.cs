
using Aspose.Cells;
using Microsoft.AspNetCore.Components.Forms;

public interface IConverterService
{
    Task<string> ConvertToExcelAsync(IBrowserFile file, string guid);
    Task<string> CombineExcelToJson(MemoryStream memoryStreamOfTranslatedExcelFile, string operationGuid, string guidOfValueExcel);
    Task CreateFolderForOperation(string folderName);

    Task<MemoryStream> GetTheMemoryStreamFromValueExcel(string operationGuid, string uploadedDocumentGuid);
}

