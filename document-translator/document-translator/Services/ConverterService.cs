using Aspose.Cells;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json;
using System;

public class ConverterService : IConverterService
{
    private IConfiguration _configuration;

    private readonly string blobServiceClientEndpoint;

    public ConverterService(IConfiguration configuration)
    {
        _configuration = configuration;
        blobServiceClientEndpoint = _configuration.GetConnectionString("Blob.Service.Client");

    }

    public async Task<string> ConvertToExcelAsync(IBrowserFile file, string operationGuid)
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
        keysWorkbook.FileName = $"{guid}";
        valuesWorkbook.FileName = $"{guid}";

        keysWorkbook.Save(@$"temp\keys\{operationGuid}\{guid}.xlsx");
        valuesWorkbook.Save(@$"temp\values\{operationGuid}\{guid}.xlsx");
        return $"{guid}";
    }

    public async Task<string> CombineExcelToJson(MemoryStream memoryStreamOfTranslatedExcelFile, string operationGuid, string guidOfValueExcel)
    {
        string keyFolderPath = @$"temp\keys\{operationGuid}\{guidOfValueExcel}.xlsx";

        Workbook keysWorkbook = new Workbook(keyFolderPath);
        Worksheet keysWorksheet = keysWorkbook.Worksheets[0];

        // Create a workbook from the MemoryStream
        Workbook valuesWorkbook = new Workbook(memoryStreamOfTranslatedExcelFile);

        Worksheet valuesWorksheet = valuesWorkbook.Worksheets[0];

        Dictionary<string, string> combinedData = new Dictionary<string, string>();

        for (int i = 0; i <= keysWorksheet.Cells.MaxDataRow; i++)
        {
            string key = keysWorksheet.Cells[i, 0].Value.ToString();
            string value = valuesWorksheet.Cells[i, 0].Value.ToString();

            combinedData.Add(key, value);
        }

        string jsonOutput = JsonConvert.SerializeObject(combinedData, Formatting.Indented);

        return jsonOutput;
    }

    public async Task CreateFolderForOperation(string folderName)
    {
        string keyFolderPath = @$"temp\keys\{folderName}";
        string valueFolderPath = @$"temp\values\{folderName}";

   
            Directory.CreateDirectory(keyFolderPath);
            Directory.CreateDirectory(valueFolderPath);
    }

    public async Task DeleteFolderForOperation(string folderName)
    {
        string keyFolderPath = @$"temp\keys\{folderName}";
        string valueFolderPath = @$"temp\values\{folderName}";

        Directory.Delete(keyFolderPath,true);

        Directory.Delete(valueFolderPath,true);
    }

    public async Task<MemoryStream> GetTheMemoryStreamFromValueExcel(string operationGuid, string uploadedDocumentGuid)
    {
        string filePath = @$"temp\values\{operationGuid}\{uploadedDocumentGuid}.xlsx";
        byte[] fileBytes = File.ReadAllBytes(filePath);
        MemoryStream memoryStreamOfValueExcelFile = new MemoryStream(fileBytes);
        return memoryStreamOfValueExcelFile;
    }
}

