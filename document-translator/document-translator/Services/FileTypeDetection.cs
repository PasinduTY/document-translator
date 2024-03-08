
public class FileTypeDetection:IFileTypeDetection
{
    public string getFileType(int a)
    {
        switch( a){
           case 5:
                return "json";
            default:
                return "invalid";
        }
    }

    public void doSomething()
    {

    }
}

