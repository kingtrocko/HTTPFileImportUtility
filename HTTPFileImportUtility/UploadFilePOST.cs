using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;

namespace HTTPFileImportUtility
{
    public enum Processtype
    {
        Employees = 1,
        TimeEntries = 2,
        AccrualBalances = 3
    }

    public static class UploadFilePOST
    {
        private static readonly Encoding encoding = Encoding.UTF8;
        
        /// <summary>
        ///     Uploads the specified file into the the WebApps Management Center system using the HTTP POST method.
        /// </summary>
        /// <param name="companyName">Company Short Name, as setup in WebApps.</param>
        /// <param name="companyPassword">Company import password, as setup in WebApps.</param>
        /// <param name="processType">The target process type which determine the post url of the HTTP POST Request.
        /// This value should match with the file template that is being uploaded.
        /// </param>
        /// <param name="filePath">The current full path of the file.</param>
        /// <returns>
        ///     The reponse of the server as a System.String Object.
        /// </returns>
        public static string UploadFileToHost(string companyName, string companyPassword, Processtype processType, string filePath)
        {
            string postUrl = "";
            byte[] fileData = GetFileData(filePath);
            Dictionary<string, object> postParameters = new Dictionary<string, object>();
            postParameters.Add("CompName", companyName);
            postParameters.Add("CompPass", companyPassword);
            postParameters.Add("InputType", "Excel"); // Excel, CSV, ExcelCSV, XML
            postParameters.Add("OutputType", "0"); // Text = 0, HTL = 1, XML = 2
            postParameters.Add("DoUpdate", "1"); // Send 1 as a flag to update system with supplied data otherwise, data is NOT imported.
            postParameters.Add("ACTION", "IMPORT");
            postParameters.Add("UploadFile", new FileParameter(fileData, Path.GetFileName(filePath), "application/vnd.ms-excel"));

            switch (processType)
            {
                case Processtype.Employees:       postUrl = "http://www.saashr.com/ta/imports/EmpInfo.jsp";         break;
                case Processtype.AccrualBalances: postUrl = "http://www.saashr.com/ta/imports/AccrualBalances.jsp"; break;
                case Processtype.TimeEntries:     postUrl = "http://www.saashr.com/ta/imports/TimeEntries.jsp";     break;
            }

            HttpWebResponse webResponse = FormDataPost(postUrl, postParameters);
            StreamReader responseReader = new StreamReader(webResponse.GetResponseStream());
            //webResponse.Close();
            return responseReader.ReadToEnd();
        }

        private static byte[] GetFileData(string filePath)
        {
            FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            byte[] data = new byte[fs.Length];
            fs.Read(data, 0, data.Length);
            fs.Close();
            return data;
        }

        private static HttpWebResponse FormDataPost(string postUrl, Dictionary<string, object> postParameters)
        {
            string formDataBoundary = String.Format("----------{0:N}", Guid.NewGuid());
            string contentType = "multipart/form-data; boundary=" + formDataBoundary;
            string userAgent = "Simplex Group";

            byte[] formData = GetMultipartFormData(postParameters, formDataBoundary);

            return PostForm(postUrl, userAgent, contentType, formData);
        }

        private static HttpWebResponse PostForm(string postUrl, string userAgent, string contentType, byte[] formData)
        {
            HttpWebRequest request = WebRequest.Create(postUrl) as HttpWebRequest;

            if (request == null)
            {
                throw new NullReferenceException("request is not a http request");
            }

            // Set up the request properties.
            request.Method = "POST";
            request.ContentType = contentType;
            request.UserAgent = userAgent;
            request.CookieContainer = new CookieContainer();
            request.ContentLength = formData.Length;

            // Send the form data to the request.
            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(formData, 0, formData.Length);
                requestStream.Close();
            }
            
            //Send the request to the server
            return request.GetResponse() as HttpWebResponse;
        }

        private static byte[] GetMultipartFormData(Dictionary<string, object> postParameters, string boundary)
        {
            Stream formDataStream = new System.IO.MemoryStream();
            bool needsCLRF = false;

            foreach (var param in postParameters)
            {
                // add a CRLF to allow multiple parameters to be added.
                // Skip it on the first parameter, add it to subsequent parameters.
                if (needsCLRF)
                    formDataStream.Write(encoding.GetBytes("\r\n"), 0, encoding.GetByteCount("\r\n"));

                needsCLRF = true;

                if (param.Value is FileParameter)
                {
                    FileParameter fileToUpload = (FileParameter)param.Value;

                    // Add just the first part of this param, since we will write the file data directly to the Stream
                    string header = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\n\r\n",
                        boundary,
                        param.Key,
                        fileToUpload.FileName ?? param.Key,
                        fileToUpload.ContentType ?? "application/octet-stream");

                    formDataStream.Write(encoding.GetBytes(header), 0, encoding.GetByteCount(header));

                    // Write the file data directly to the Stream, rather than serializing it to a string.
                    formDataStream.Write(fileToUpload.File, 0, fileToUpload.File.Length);
                }
                else
                {
                    string postData = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}",
                        boundary,
                        param.Key,
                        param.Value);
                    formDataStream.Write(encoding.GetBytes(postData), 0, encoding.GetByteCount(postData));
                }
            }

            // Add the end of the request.  Start with a newline
            string footer = "\r\n--" + boundary + "--\r\n";
            formDataStream.Write(encoding.GetBytes(footer), 0, encoding.GetByteCount(footer));

            formDataStream.Position = 0;
            byte[] formData = new byte[formDataStream.Length];
            formDataStream.Read(formData, 0, formData.Length);
            formDataStream.Close();
            return formData;
        }

        private class FileParameter
        {
            public byte[] File { get; set; }
            public string FileName { get; set; }
            public string ContentType { get; set; }
            public FileParameter(byte[] file) : this(file, null) { }
            public FileParameter(byte[] file, string filename) : this(file, filename, null) { }
            public FileParameter(byte[] file, string filename, string contenttype)
            {
                File = file;
                FileName = filename;
                ContentType = contenttype;
            }
        }
    }
}
