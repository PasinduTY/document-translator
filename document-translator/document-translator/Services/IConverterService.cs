
using Aspose.Cells;
using Microsoft.AspNetCore.Components.Forms;

public interface IConverterService
{
    Task<Workbook[]> ConvertToExcelAsync(IBrowserFile file);
    Task CombineExcelToJson(Workbook[] books);
}

