using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Radzen;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace document_translator.Components.Pages
{
    public partial class DocumentTranslator
    {
        string value;
        Dictionary<String, Language> languages;
        int uploadedDocumentCount = 0;
       // bool hideDownload = true;
        string operationGuid;
        bool jsonTranslationInitialized = false;
        bool isUploading = false;
        bool isUploaded = false;
        bool isTranslating = false;
        bool isTranslated=false;
        //     bool hideProgress = true;
        //bool hideCount = true;
        bool isDownloaded = false;
       // bool isBusy = false; //
       // bool resetDisable = true;
       // int resetState = 0;
       // bool uploadedOrNot;
        double percentage; //
        private async Task LoadFiles(InputFileChangeEventArgs e)
        {
            List<IBrowserFile> selectedFiles = e.GetMultipleFiles().ToList();
            isUploading = true;
            operationGuid = Guid.NewGuid().ToString();
            foreach (var file in e.GetMultipleFiles())
            {
                string blobName;
                MemoryStream memoryStreamOfFile;
                string contentType = file.ContentType;
                Console.Write(contentType);
                if (file.ContentType == "application/json")
                {
                    Console.Write(contentType);
                    if (jsonTranslationInitialized != true)
                    {
                        iTranslatorService.CreateFolderForOperation(operationGuid);
                        jsonTranslationInitialized = true;
                    }
                    string uploadedDocumentGuid = await iTranslatorService.ConvertToExcelAsync(file, operationGuid);
                    memoryStreamOfFile = await iTranslatorService.GetTheMemoryStreamFromValueExcel(operationGuid, uploadedDocumentGuid);
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
                bool isUploaded = await iTranslatorService.UploadDocumentsAsync(memoryStreamOfFile, blobName);
                if (isUploaded)
                {
                    handleUplodedPerecetage(selectedFiles.Count);
                }
                else
                {
                    ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Upload is not Successful. Try Again", Duration = 4000 });

                }
            }
           // resetState = 1;
        }
        void OnChange()
        {
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
            ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = "Upload Successful", Duration = 4000 });
           // if (percentage == 100)
           // {
                isUploading = false;
                isUploaded = true;
               // hideCount = false;
            //}
        }

        List<string> dropdownList = new List<string>();

        protected override async Task OnInitializedAsync()
        {
            
            //iSynchronousTranslationService.TranslateDocument("C:/Users/pasindu.si/Downloads/1.txt", "D:/Syn/output.txt", "hi");
            //iTextTranslateService.TextTranslator("I would really like to drive your car around the block a few times.", "fr");
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
                ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Please select a language", Duration = 4000 });
            }
            else
            {
                isTranslating = true;
                String langCode = languages.Where(language => language.Value.name == value).FirstOrDefault().Key;
              /*  if (!resetDisable)
                {
                    resetDisable = true;
               }*/
                bool translatedOrNot = await iTranslatorService.TranslateAsync(langCode, operationGuid);
                isTranslating= false;
             /*   if (resetDisable)
                {
                    resetDisable = false;
                    resetState = 2;
                }*/

                if (translatedOrNot)
                {
                    ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = "Translation Successful", Duration = 4000 });
                    //hideDownload = false;
                    isTranslated = true;
                    //StateHasChanged();
                }
                else
                {
                    //StateHasChanged();
                    ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Translation is not Successful. Try Again", Duration = 4000 });
                    iTranslatorService.DeleteFilesInInputContainerOfOperation(operationGuid);
                    iTranslatorService.DeleteFilesInOutputContainerOfOperation(operationGuid);
                    if (jsonTranslationInitialized)
                    {
                        await iTranslatorService.DeleteKeyAndValueFolders(operationGuid);
                    }
                }
            }
        }

        private async Task onClickReset()
        {
            if(!isUploaded&& !isTranslated)
            {
                ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = "You haven't start the translation to cancel", Duration = 4000 });
            }
            else if(isUploaded && !isTranslated)
            {
                uploadedDocumentCount = 0;
                iTranslatorService.DeleteFilesInInputContainerOfOperation(operationGuid);
                ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = "Successfully canceled operation. Upload documents again", Duration = 4000 });

            }
            else if (isUploaded && isTranslated)
            {
                uploadedDocumentCount = 0;
                iTranslatorService.DeleteFilesInInputContainerOfOperation(operationGuid);
                iTranslatorService.DeleteFilesInOutputContainerOfOperation(operationGuid);
                if (jsonTranslationInitialized)
                {
                    await iTranslatorService.DeleteKeyAndValueFolders(operationGuid);
                }
                ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = "Successfully canceled operation. Upload documents again", Duration = 4000 });
            }
            isUploaded = false;
            isTranslated = false;
        }

        private async Task onClickDownload()
        {
            await DownloadAsZipFile();
            ShowNotification(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = "Download Complete", Duration = 4000 });
            isTranslated = false;
            isUploaded = false;
            isTranslating = false;
            isUploading = false;
            uploadedDocumentCount= 0;
            //await CleanTheFiles();
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

    }
}