using Microsoft.AspNetCore.Components.Forms;
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

        private async void LoadFiles(InputFileChangeEventArgs e)
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
                uploadedOrNot = await iTranslatorService.UploadDocuments(memoryStreamOfFile, blobName);
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
            SynchronousTranslationService.Translate();
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

        private async void onClickTranslate()
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                //hideChooseAlert = false;
                ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Please select a language", Duration = 4000 });
            }
            else
            {
                isBusy = true;
                String langCode = languages.Where(language => language.Value.name == value).FirstOrDefault().Key;
                bool translatedOrNot = await iTranslatorService.Translate(langCode, operationGuid);
                isBusy = false;



                if (translatedOrNot)
                {
                    //hideSuccess = false;
                    ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = "Translation Successful", Duration = 4000 });
                    hideDownload = false;
                    StateHasChanged();
                }
                else
                {
                    //hideFail = false;
                    ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Translation Unsuccessful", Duration = 4000 });
                    StateHasChanged();
                }
            }
        }

        private async void onClickDownload()
        {
            //hideSuccess = true
            hideDownCount = false;
            await iTranslatorService.DownloadConvertedFiles(operationGuid, iConverterService);
            ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = "Download Successful", Duration = 4000 });

            if (isFolderForJsonConversionCreated)
            {
                iConverterService.DeleteFolderForOperation(operationGuid);

            }
            
            // iTranslatorService.CleanInputContainer();
            // iTranslatorService.CleanOutputContainer();
        }
        private async void onClickReset()
        { 
        
        }
            void ShowNotification(NotificationMessage message)
        {
            NotificationService.Notify(message);

            Console.WriteLine($"{message.Severity} notification");
        }
    }
}