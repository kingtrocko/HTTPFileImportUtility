using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HTTPFileImportUtility;
using System.Dynamic;

namespace TestUploadUtility
{
    class Program
    {
        static void Main(string[] args)
        {
            string filePath = "C:\\ImportData\\TimeEntries.xls";
            Console.WriteLine("Waiting a response from the server...");
            string serverReponse = UploadFilePOST.UploadFileToHost("RHRGDemo", "abc+123", Processtype.TimeEntries, filePath);
            Console.WriteLine("\n The reponse of the server is:");
            
            dynamic xx = null;
            var n = xx.Lincense;
            ExpandoObject obj;
            Console.WriteLine(serverReponse);
            Console.ReadKey();

        }
    }
}
