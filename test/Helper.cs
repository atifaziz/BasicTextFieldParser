namespace MonoTests.Microsoft_VisualBasic.FileIO
{
    using System;

    static class Helper
    {
        public static void RemoveWarning  (object obj)
        {
        }

        public static string Join <T> (T [] array, string delimiter)
        {
            if (array == null)
                return Microsoft.VisualBasic.Strings.Join (null, delimiter);
            object [] obj = new object [array.Length];
            Array.Copy (array, obj, array.Length);
            return Microsoft.VisualBasic.Strings.Join (obj, delimiter);
        }
    }
}
