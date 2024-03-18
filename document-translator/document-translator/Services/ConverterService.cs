using Aspose.Cells;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json;

public class ConverterService : IConverterService
{
    private IConfiguration _configuration;

    private readonly string blobServiceClientEndpoint;

    public ConverterService(IConfiguration configuration)
    {
        _configuration = configuration;
        blobServiceClientEndpoint = _configuration.GetConnectionString("Blob.Service.Client");

    }

    public async Task<String> ConvertToExcelAsync(IBrowserFile file)
    {
        //List<Workbook> workbooks = new List<Workbook>();

        //var memoryStream = new MemoryStream();

        //await file.OpenReadStream().CopyToAsync(memoryStream);

        //memoryStream.Seek(0, SeekOrigin.Begin);

        //var reader = new StreamReader(memoryStream);

        //string json = await reader.ReadToEndAsync();

        //// Now you can use the fileContent string where a string is expected
        //var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

        //Workbook keysWorkbook = new Workbook();
        //Worksheet keysWorksheet = keysWorkbook.Worksheets[0];

        //Workbook valuesWorkbook = new Workbook();
        //Worksheet valuesWorksheet = valuesWorkbook.Worksheets[0];

        //int row = 0;
        //foreach (var kvp in jsonObject)
        //{
        //    keysWorksheet.Cells[row, 0].PutValue(kvp.Key);
        //    valuesWorksheet.Cells[row, 0].PutValue(kvp.Value);
        //    row++;
        //}
        //Guid guid = Guid.NewGuid();
        //keysWorkbook.FileName = "Keys";
        //valuesWorkbook.FileName = "Values";

        ////valuesWorksheet.FileName = "ss";
        //workbooks.Add(keysWorkbook);
        //workbooks.Add(valuesWorkbook);
        //// keysWorkbook.Save("Keys.xlsx");
        //// valuesWorkbook.Save("Values.xlsx");
        //return workbooks.ToArray();

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


        keysWorkbook.Save(@$"Keys_{guid}.xlsx");
        valuesWorkbook.Save(@$"Values_{guid}.xlsx");
        return $"{guid}";


        /*
        //CombineToJson();
        var stream = file.OpenReadStream();

        // Create a MemoryStream and copy the content of the file stream into it
        using (MemoryStream memoryStream = new MemoryStream())
        {
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0; // Reset the position to the beginning of the stream

            // Load the Excel file directly from the MemoryStream
            Workbook keysWorkbook = new Workbook(memoryStream);
            return keysWorkbook;
            // Save the workbook to a new Excel file
            //keysWorkbook.Save("Keys.xlsx");
        }
        */
    }

    public async Task CombineExcelToJson(Workbook[] books, string blobNameOfUploadedDocument)
    {
        //Workbook keysWorkbook = new Workbook(@"C:\\Users\\pasindu.si\\Downloads\Keys.xlsx");
        Worksheet keysWorksheet = books[0].Worksheets[0];

        BlobServiceClient blobServiceClient = new BlobServiceClient(blobServiceClientEndpoint);

        // Get a reference to the container
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient($"translateddocs");
        Console.WriteLine(books[1].FileName);
        // Get a reference to the blob
        BlobClient blobClient = containerClient.GetBlobClient($"{blobNameOfUploadedDocument}");

        // Download the blob content to a MemoryStream
        var memoryStream = new MemoryStream();
        blobClient.DownloadTo(memoryStream);

        // Create a workbook from the MemoryStream
        Workbook valuesWorkbook = new Workbook(memoryStream);
        //Workbook valuesWorkbook = new Workbook(@"C:\\Users\\pasindu.si\\Downloads\Values.xlsx");
        Worksheet valuesWorksheet = valuesWorkbook.Worksheets[0];

        Dictionary<string, string> combinedData = new Dictionary<string, string>();

        for (int i = 0; i <= keysWorksheet.Cells.MaxDataRow; i++)
        {
            string key = keysWorksheet.Cells[i, 0].Value.ToString();
            string value = valuesWorksheet.Cells[i, 0].Value.ToString();

            combinedData.Add(key, value);
        }

        string jsonOutput = JsonConvert.SerializeObject(combinedData, Formatting.Indented);

        File.WriteAllText(@"C:\\Users\\pasindu.si\\Downloads\output.json", jsonOutput);
    }

}

