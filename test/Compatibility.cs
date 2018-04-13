namespace Microsoft.VisualBasic.FileIO
{
    using System.IO;

    static class FileSystem
    {
        public static void WriteAllText(string file, string text, bool append)
        {
            if (append)
                File.AppendAllText(file, text);
            else
                File.WriteAllText(file, text);
        }

        public static void WriteAllBytes(string file, byte[] data, bool append)
        {
            using (var fs = File.OpenWrite(file))
            {
                if (append)
                    fs.Seek(0, SeekOrigin.End);
                fs.Write(data, 0, data.Length);
            }
        }
    }
}

namespace Microsoft.VisualBasic
{
    static class Strings
    {
        public static string Join(string[] array, string delimiter) =>
            (array?.Length ?? 0) == 0 ? null : string.Join(delimiter ?? " ", array);

        public static string Join(object[] array, string delimiter) =>
            (array?.Length ?? 0) == 0 ? null : string.Join(delimiter ?? " ", array);
    }
}
