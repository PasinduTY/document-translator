using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Radzen;
using System.Text.Json;

namespace document_translator.Components.Pages
{
    public partial class Home
    {
        string value;
        Dictionary<String, Language> languages;
        private int uploadedDocumentCount = 0;
        bool hideLoading = true;
        bool hideSuccess = true;
        bool hideFail = true;
        bool hideDownload = true;
        string fileType;
        string uploadedDocumentGuid = "";
        string operationGuid;
        bool isFolderForJsonConversionCreated = false;
        bool hideProgress = true;
        bool hideCount = true;
        bool hideDownCount = true;
        bool isBusy = false;
        bool hideChooseAlert = true;
        double percentage;
        private List<IBrowserFile> selectedFiles = new List<IBrowserFile>();

        private async Task LoadFiles(InputFileChangeEventArgs e)
        {
            selectedFiles = e.GetMultipleFiles().ToList();
            int selectedFilesCount = selectedFiles.Count;
            hideProgress = false;
            Guid guid = Guid.NewGuid();
            operationGuid = $"{guid}";
            foreach (var file in e.GetMultipleFiles())
            {
                string blobName = "";
                MemoryStream memoryStreamOfFile;
                bool uploadedOrNot;
                string contentType = file.ContentType;
                fileType = contentType.Split('/')[1];

                if (fileType == "json")
                {
                    if (isFolderForJsonConversionCreated != true)
                    {
                        iConverterService.CreateFolderForOperation(operationGuid);
                        isFolderForJsonConversionCreated = true;
                    }
                    uploadedDocumentGuid = await iConverterService.ConvertToExcelAsync(file, operationGuid);
                    memoryStreamOfFile = await iConverterService.GetTheMemoryStreamFromValueExcel(operationGuid, uploadedDocumentGuid);
                    blobName = $"{operationGuid}/json/{uploadedDocumentGuid}.xlsx";
                }
                else
                {
                    var stream = file.OpenReadStream();
                    memoryStreamOfFile = new MemoryStream();
                    await stream.CopyToAsync(memoryStreamOfFile);
                    memoryStreamOfFile.Position = 0;
                    string fileName = file.Name;
                    blobName = $"{operationGuid}/{fileName}";
                }
                uploadedOrNot = await iTranslatorService.UploadDocumentsAsync(memoryStreamOfFile, blobName);
                if (uploadedOrNot)
                {
                    uploadedDocumentCount++;
                    percentage = uploadedDocumentCount * 100 / selectedFilesCount;
                    if (percentage == 100)
                    {
                        hideProgress = true;
                        hideCount = false;
                    }
                    StateHasChanged();
                }
            }
        }
        void OnChange()
        {
        }

        List<string> dropdownList = new List<string>();

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            // Call the API to get languages
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync("https://api.cognitive.microsofttranslator.com/languages?api-version=3.0");

            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                // Deserialize JSON and extract languages
                LanguageResponse? languageResponse = JsonSerializer.Deserialize<LanguageResponse>(json);
                languages = languageResponse.translation;
                foreach (var entry in languageResponse.translation)
                {
                    dropdownList.Add(entry.Value.name);
                }
                value = " ";/* dropdownList.FirstOrDefault(); */


            }
        }

        private class LanguageResponse
        {
            public Dictionary<String, Language> translation { get; set; }
        }

        private class Language
        {
            public string name { get; set; }
            public string nativeName { get; set; }
            public string dir { get; set; }
        }

        private async Task onClickTranslate()
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                hideChooseAlert = false;
                //ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Error Summary", Detail = "Error Detail", Duration = 4000 });
            }
            else
            {
                isBusy = true;
                String langCode = languages.Where(language => language.Value.name == value).FirstOrDefault().Key;
                bool translatedOrNot = await iTranslatorService.TranslateAsync(langCode, operationGuid);
                isBusy = false;



                if (translatedOrNot)
                {
                    hideSuccess = false;
                    hideDownload = false;
                    StateHasChanged();
                }
                else
                {
                    hideFail = false;
                    StateHasChanged();
                }
            }
        }

        private async Task onClickDownload()
        {
            await DownloadAsZipFile();
            // await CleanTheFiles();
            // iTranslatorService.CleanInputContainer();
            // iTranslatorService.CleanOutputContainer();
        }
        private async Task onClickReset()
        { 
        
        }

        private async Task DownloadAsZipFile()
        {
            await iTranslatorService.DownloadConvertedFiles(operationGuid);
            string fileName = "Translated_Files.zip";
            var fileURL = $"/translated_files_as_zip/{operationGuid}/Translated_Files.zip";
            await JSRuntime.InvokeVoidAsync("triggerFileDownload", fileName, fileURL);
        }

        private async Task CleanTheFiles()
        {
            await iResetService.DeleteZipFolderInRoot(operationGuid);
            await iResetService.DeleteFilesInOutputContainerOfOperation(operationGuid);
            await iResetService.DeleteFilesInInputContainerOfOperation(operationGuid);

            if (isFolderForJsonConversionCreated)
            {
                await iResetService.DeleteKeyAndValueFolders(operationGuid);
            }
        }

    }
}