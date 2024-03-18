
using Aspose.Cells;
using Microsoft.AspNetCore.Components.Forms;

public interface IConverterService
{
    Task<String> ConvertToExcelAsync(IBrowserFile file);
    Task CombineExcelToJson(Workbook[] books, string blobNameOfUploadedDocument);
}

