using System.IO;

namespace LinkDownloader.Extensions
{
    public static class FileInfoExtensions
    {
        public static bool IsFileLocked(this FileInfo file)
        {
                // FileStream stream = null;
            try
            {
                // using (stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                // BUG: this line might also cause file locking
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None)) {}
                // {
                //     stream.Close();
                // }
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            // finally
            // {
            //     stream.Close();
            // }

            //file is not locked
            return false;
        }
    }
}
