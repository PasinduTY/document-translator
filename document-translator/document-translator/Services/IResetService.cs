public interface IResetService
{
    Task DeleteKeyAndValueFolders(string operationGuid);
    Task DeleteFilesInInputContainerOfOperation(string operationGuid);
    Task DeleteFilesInOutputContainerOfOperation(string operationGuid);
    Task DeleteZipFolderInRoot(string operationGuid);
}

