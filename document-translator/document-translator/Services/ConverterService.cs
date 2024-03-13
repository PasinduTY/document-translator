using Aspose.Cells;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Identity.Client.Extensibility;
using Newtonsoft.Json;
public class ConverterService : IConverterService
{
    public async Task<Workbook> ConvertToExcelAsync(IBrowserFile file)
    {
        var memoryStream = new MemoryStream();

        await file.OpenReadStream().CopyToAsync(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);

        var reader = new StreamReader(memoryStream);
        string json = await reader.ReadToEndAsync();


        // Now you can use the fileContent string where a string is expected
        var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

        Workbook keysWorkbook = new Workbook();
        Worksheet keysWorksheet = keysWorkbook.Worksheets[0];

        Workbook valuesWorkbook = new Workbook();
        Worksheet valuesWorksheet = valuesWorkbook.Worksheets[0];

        int row = 0;
        foreach (var kvp in jsonObject)
        {
            keysWorksheet.Cells[row, 0].PutValue(kvp.Key);
            valuesWorksheet.Cells[row, 0].PutValue(kvp.Value);
            row++;
        }

        Guid guid = Guid.NewGuid();

        // Print the generated GUID
        //Console.WriteLine($"Generated GUID: {guid}");


        keysWorkbook.Save(@$"C:\\Users\\pasindu.si\\Downloads\Keys_{guid}.xlsx");
        valuesWorkbook.Save(@$"C:\\Users\\pasindu.si\\Downloads\Values_{guid}.xlsx");
        return keysWorkbook;

        CombineToJson();
        
    }

    public void CombineToJson() {
        Workbook keysWorkbook = new Workbook(@"C:\\Users\\pasindu.si\\Downloads\Keys.xlsx");
        Worksheet keysWorksheet = keysWorkbook.Worksheets[0];

        Workbook valuesWorkbook = new Workbook(@"C:\\Users\\pasindu.si\\Downloads\Values.xlsx");
        Worksheet valuesWorksheet = valuesWorkbook.Worksheets[0];

        Dictionary<string, string> combinedData = new Dictionary<string, string>();

        for (int i = 0; i <= keysWorksheet.Cells.MaxDataRow; i++)
        {
            string key = keysWorksheet.Cells[i, 0].Value.ToString();
            string value = valuesWorksheet.Cells[i, 0].Value.ToString();

            combinedData.Add(key, value);
        }

        string jsonOutput = JsonConvert.SerializeObject(combinedData);

        File.WriteAllText(@"C:\\Users\\pasindu.si\\Downloads\output.json", jsonOutput);
    }




}
