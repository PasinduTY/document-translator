using Aspose.Cells;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using Radzen.Blazor.Rendering;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace document_translator.Components.Pages
{
    public partial class DocumentTranslator
    {
        string value;
        Dictionary<String, Language> languages;
        int uploadedDocumentCount = 0;
        string operationGuid;
        bool jsonTranslationInitialized = false;
        bool isUploading = false;
        bool isUploaded = false;
        bool isTranslating = false;
        bool isTranslated=false;
        bool isDownloaded = false;
        double percentage;
        private async Task LoadFiles(InputFileChangeEventArgs e)
        {
            List<IBrowserFile> selectedFiles = e.GetMultipleFiles().ToList();
            isUploading = true;
            operationGuid = Guid.NewGuid().ToString();
            foreach (var file in e.GetMultipleFiles())
            {
                string blobName="";
                MemoryStream memoryStreamOfFile=null;
                string contentType = file.ContentType;
                Console.Write(contentType);
                if (file.ContentType == "application/json")
                {
                        try
                        {
                        if (jsonTranslationInitialized != true)
                        {
                            iTranslatorService.CreateFolderForOperation(operationGuid);
                            jsonTranslationInitialized = true;
                        }
                            string uploadedDocumentGuid = await iTranslatorService.ConvertToExcelAsync(file, operationGuid);
                            memoryStreamOfFile = await iTranslatorService.GetTheMemoryStreamFromValueExcel(operationGuid, uploadedDocumentGuid);
                            blobName = $"{operationGuid}/json/{uploadedDocumentGuid}.xlsx";
                        }
                        catch (Exception ex) {
                            //fill
                        }
                }
                else
                {
                    try
                    {
                        var stream = file.OpenReadStream();
                        memoryStreamOfFile = new MemoryStream();
                        await stream.CopyToAsync(memoryStreamOfFile);
                        memoryStreamOfFile.Position = 0;
                        string fileName = file.Name;
                        blobName = $"{operationGuid}/{fileName}";
                    }catch (Exception ex) {
                        //fill
                    }

                }
                bool isUploaded = await iTranslatorService.UploadDocumentsAsync(memoryStreamOfFile, blobName);
                if (isUploaded)
                {
                    handleUplodedPerecetage(selectedFiles.Count);
                }
            }
            handleUploadCompletion(selectedFiles.Count, uploadedDocumentCount);
        }

        void ShowNotification(NotificationMessage message)
        {
            notificationService.Notify(message);

            Console.WriteLine($"{message.Severity} notification");
        }
        void handleUplodedPerecetage(int fileCount)
        {
            uploadedDocumentCount++;
            percentage = uploadedDocumentCount * 100 / fileCount;
            StateHasChanged();
        }

        void handleUploadCompletion(int fileCount, int uploadedCount)
        {
            isUploading = false;
            isUploaded = true;
            percentage = 0;
            if (uploadedCount == fileCount)
            {
                ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = "Uploaded Successfully", Duration = 4000 });
            }
            else if (uploadedCount == 0)
            {
                ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Uploading unsuccessfull. Please Try Again", Duration = 4000 });
                resetTranslation();
            }
            else
            {
                ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Warning, Summary = $"Only {uploadedDocumentCount} are uploaded", Duration = 4000 });
            }
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
                value = " ";
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

        void OnChange()
        {
            if (isTranslated)
            {
                iTranslatorService.DeleteFilesInOutputContainerOfOperation(operationGuid);
                isTranslated = false;
            }
        }
        private async Task onClickTranslate()
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Please select a language", Duration = 4000 });
            }
            else
            {
                isTranslating = true;
                String langCode = languages.Where(language => language.Value.name == value).FirstOrDefault().Key;
                bool translatedOrNot = await iTranslatorService.TranslateAsync(langCode, operationGuid);
                isTranslating= false;
                if (translatedOrNot)
                {
                    ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = "Translation Successful", Duration = 4000 });
                    isTranslated = true;
                }
                else
                {
                    ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Translation is not Successful. Try Again", Duration = 4000 });
                    iTranslatorService.DeleteFilesInOutputContainerOfOperation(operationGuid);
                }
            }
        }

        private async Task onClickReset()
        {
           // var  result = await dialogService.Confirm("Are you sure?", "Cancel Translation", new ConfirmOptions() { OkButtonText = "Yes", CancelButtonText = "No" });

            if (!isUploaded&& !isTranslated)
            {
                ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "You haven't start the translation to cancel", Duration = 4000 });

            }

            else if(isUploaded && !isTranslated)
            {
                dialogService.Confirm("Are you sure?", "MyTitle", new ConfirmOptions() { OkButtonText = "Yes", CancelButtonText = "No" });
                iTranslatorService.DeleteFilesInInputContainerOfOperation(operationGuid);

            }
            else if (isUploaded && isTranslated)
            {
                iTranslatorService.DeleteFilesInInputContainerOfOperation(operationGuid);
                iTranslatorService.DeleteFilesInOutputContainerOfOperation(operationGuid);
                if (jsonTranslationInitialized)
                {
                    await iTranslatorService.DeleteKeyAndValueFolders(operationGuid);
                }            }
            resetTranslation();
        }

        private async Task onClickDownload()
        {
            try
            {
                await DownloadAsZipFile();
                ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = "Download Complete", Duration = 4000 });
                resetTranslation();
                await Task.Delay(3000);
                await CleanTheFiles();
            }
            catch (Exception ex)
            {
                ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Error with downloading translated documents. Please Download again", Duration = 4000 });
            }
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
            await iTranslatorService.DeleteZipFolderInRoot(operationGuid);
            await iTranslatorService.DeleteTranslatedDocumentFolder(operationGuid);
            await iTranslatorService.DeleteFilesInOutputContainerOfOperation(operationGuid);
            await iTranslatorService.DeleteFilesInInputContainerOfOperation(operationGuid);

            if (jsonTranslationInitialized)
            {
                await iTranslatorService.DeleteKeyAndValueFolders(operationGuid);
            }
        }

        private void resetTranslation()
        {
             uploadedDocumentCount = 0;
             jsonTranslationInitialized = false;
             isUploading = false;
             isUploaded = false;
             isTranslating = false;
             isTranslated = false;
             isDownloaded = false;
        }
    }
}