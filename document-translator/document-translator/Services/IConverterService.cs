
using Aspose.Cells;
using Microsoft.AspNetCore.Components.Forms;

public interface IConverterService
{
<<<<<<< Updated upstream
    Task<Workbook> ConvertToExcelAsync(IBrowserFile file);
=======
    Task<Workbook[]> ConvertToExcelAsync(IBrowserFile file);
    Task CombineExcelToJson(Workbook[] books);
>>>>>>> Stashed changes
}

