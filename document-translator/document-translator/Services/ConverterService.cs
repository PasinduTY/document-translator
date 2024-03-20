using Aspose.Cells;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json;
using System;

public class ConverterService : IConverterService
{
    private readonly ILogger<ITranslatorService> _logger;

    public ConverterService(ILogger<ITranslatorService> logger)
    {
        _logger = logger;
    }

    public async Task<string> ConvertToExcelAsync(IBrowserFile file, string operationGuid)
    {
        try
        {
            // Using MemoryStream and StreamReader to read the content of the file
            using var memoryStream = new MemoryStream();
            await file.OpenReadStream().CopyToAsync(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(memoryStream);
            string json = await reader.ReadToEndAsync();

            // Deserializing JSON content into a dictionary
            var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            // Creating workbooks and worksheets
            Workbook keysWorkbook = new Workbook();
            Worksheet keysWorksheet = keysWorkbook.Worksheets[0];
            Workbook valuesWorkbook = new Workbook();
            Worksheet valuesWorksheet = valuesWorkbook.Worksheets[0];

            // Populating worksheets with key-value pairs
            int row = 0;
            foreach (var kvp in jsonObject)
            {
                keysWorksheet.Cells[row, 0].PutValue(kvp.Key);
                valuesWorksheet.Cells[row, 0].PutValue(kvp.Value);
                row++;
            }

            // Generating a unique GUID for filenames
            Guid guid = Guid.NewGuid();
            string guidString = guid.ToString();

            // Setting filenames for workbooks
            keysWorkbook.FileName = guidString;
            valuesWorkbook.FileName = guidString;

            // Saving workbooks to temporary directories
            string keysFilePath = Path.Combine("Temporary_documents", "keys", operationGuid, $"{guidString}.xlsx");
            string valuesFilePath = Path.Combine("Temporary_documents", "values", operationGuid, $"{guidString}.xlsx");

            keysWorkbook.Save(keysFilePath);
            valuesWorkbook.Save(valuesFilePath);

            // Returning the generated GUID
            return guidString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while converting to Excel");
            throw;
        }
    }

    public async Task<string> CombineExcelToJson(MemoryStream memoryStreamOfTranslatedExcelFile, string operationGuid, string guidOfValueExcel)
    {
        try
        {
            string keyFolderPath = Path.Combine("Temporary_documents", "keys", operationGuid, $"{guidOfValueExcel}.xlsx");

            // Load keys workbook from file
            Workbook keysWorkbook = new Workbook(keyFolderPath);
            Worksheet keysWorksheet = keysWorkbook.Worksheets[0];

            // Create a workbook from the MemoryStream
            Workbook valuesWorkbook = new Workbook(memoryStreamOfTranslatedExcelFile);
            Worksheet valuesWorksheet = valuesWorkbook.Worksheets[0];

            Dictionary<string, string> combinedData = new Dictionary<string, string>();

            // Iterate through each row in the keys worksheet
            for (int i = 0; i <= keysWorksheet.Cells.MaxDataRow; i++)
            {
                string key = keysWorksheet.Cells[i, 0].Value.ToString();
                string value = valuesWorksheet.Cells[i, 0].Value.ToString();

                combinedData.Add(key, value);
            }

            // Serialize combined data to JSON with indentation
            string jsonOutput = JsonConvert.SerializeObject(combinedData, Formatting.Indented);

            return jsonOutput;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while combine excels and convert to json");
            throw;
        }
    }
    public async Task CreateFolderForOperation(string folderName)
    {
        try
        {
            string keyFolderPath = Path.Combine("Temporary_documents", "keys", folderName);
            string valueFolderPath = Path.Combine("Temporary_documents", "values", folderName);

            Directory.CreateDirectory(keyFolderPath);
            Directory.CreateDirectory(valueFolderPath);

            _logger.LogInformation($"Folders created for operation {folderName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred while creating folders for operation {folderName}");
            throw;
        }
    }

    public async Task<MemoryStream> GetTheMemoryStreamFromValueExcel(string operationGuid, string uploadedDocumentGuid)
    {
        try
        {
            string filePath = Path.Combine("Temporary_documents", "values", operationGuid, $"{uploadedDocumentGuid}.xlsx");

            if (!File.Exists(filePath))
            {
                _logger.LogError($"File does not exist: {filePath}");
                throw new FileNotFoundException("Value Excel file not found.", filePath);
            }

            byte[] fileBytes = File.ReadAllBytes(filePath);
            MemoryStream memoryStreamOfValueExcelFile = new MemoryStream(fileBytes);
            return memoryStreamOfValueExcelFile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred while getting memory stream from Value Excel for operation {operationGuid}");
            throw;
        }
    }
}

