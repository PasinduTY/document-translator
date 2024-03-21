using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Radzen;
using System;
using System.Text.Json;

namespace document_translator.Components.Pages
{
    public partial class Home
    {
        string value;
        Dictionary<String, Language> languages;
        private int uploadedDocumentCount = 0;
        bool hideLoading = true;
        bool hideDownload = true;
        string fileType;
        string uploadedDocumentGuid = "";
        string operationGuid;
        bool isFolderForJsonConversionCreated = false;
        bool hideProgress = true;
        bool hideCount = true;
        bool hideDownCount = true;
        bool isBusy = false;
        bool resetDisable = true;
        int resetState = 0;
        double percentage;
        private async Task LoadFiles(InputFileChangeEventArgs e)
        {
            List<IBrowserFile> selectedFiles = e.GetMultipleFiles().ToList();
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
                if (resetDisable == false)
                {
                    resetDisable = true;
                }
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
                    ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = "Upload Successful", Duration = 4000 });
                    percentage = uploadedDocumentCount * 100 / selectedFiles.Count;
                    if (percentage == 100)
                    {
                        hideProgress = true;
                        hideCount = false;
                    }
                    StateHasChanged();
                }
            }
            if (resetDisable == true)
            {
                resetDisable = false;
                resetState = 1;
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
                //hideChooseAlert = false;
                ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Please select a language", Duration = 4000 });
            }
            else
            {
                isBusy = true;
                String langCode = languages.Where(language => language.Value.name == value).FirstOrDefault().Key;
                if (resetDisable == false)
                {
                    resetDisable = true;
                }
                bool translatedOrNot = await iTranslatorService.TranslateAsync(langCode, operationGuid);
                isBusy = false;
                if (resetDisable == true)
                {
                    resetDisable = false;
                    resetState = 2;
                }

                if (translatedOrNot)
                {
                    ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = "Translation Successful", Duration = 4000 });
                    hideDownload = false;
                    StateHasChanged();
                }
                else
                {
                    
                    StateHasChanged();
                    iResetService.DeleteFilesInInputContainerOfOperation(operationGuid);
                    iResetService.DeleteFilesInOutputContainerOfOperation(operationGuid);
                    if (isFolderForJsonConversionCreated)
                    {
                        await iResetService.DeleteKeyAndValueFolders(operationGuid);
                    }
                }
            }
        }

        private async Task onClickReset()
        {
            switch (resetState)
            {
                case 1:
                    iResetService.DeleteFilesInInputContainerOfOperation(operationGuid);
                    break;
                case 2:
                    iResetService.DeleteFilesInInputContainerOfOperation(operationGuid);
                    iResetService.DeleteFilesInOutputContainerOfOperation(operationGuid);
                    if (isFolderForJsonConversionCreated)
                    {
                        await iResetService.DeleteKeyAndValueFolders(operationGuid);
                    }
                    break;
            }
        }

        private async Task onClickDownload()
        {
            await DownloadAsZipFile();
            hideDownCount = false;
            ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = "Download Complete", Duration = 4000 });
            await CleanTheFiles();
        }

        void ShowNotification(NotificationMessage message)
        {
            NotificationService.Notify(message);

            Console.WriteLine($"{message.Severity} notification");
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
            await iResetService.DeleteTranslatedDocumentFolder(operationGuid);
            await iResetService.DeleteFilesInOutputContainerOfOperation(operationGuid);
            await iResetService.DeleteFilesInInputContainerOfOperation(operationGuid);

            if (isFolderForJsonConversionCreated)
            {
                await iResetService.DeleteKeyAndValueFolders(operationGuid);
            }
        }

    }
}