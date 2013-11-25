using System.Threading.Tasks;
using System.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using KeepDynamic.BarcodeReader;
using System.Text;
using Aquaforest.OCR.Api;
using System.IO;

namespace OCRWebApi.Controllers
{
    public class OcrController : ApiController
    {
        public string Get()
        {
            return "API looks good!";
        }

        #region Saved code for debug purposes 

        //public string Get()
        //{
        //    var ocr = new Ocr();
        //    var preProcessor = new PreProcessor();

        //    preProcessor.Deskew = true;
        //    preProcessor.Autorotate = false;
        //    preProcessor.RemoveLines = true;
        //    preProcessor.Binarize = 150;
        //    preProcessor.Morph = "c2.2";

        //    ocr.License =
        //        "MD1BZHZhbmNlZDsxPWtlZXJ0aSAtIGt2a2lydGh5QGdtYWlsLmNvbTsyPTUyNDY4Njg5OTE5MTMwMjg5NTI7Mz1rZWVydGkgLSBrdmtpcnRoeUBnbWFpbC5jb207ND05OTk7NT1UcnVlOzUuMT1GYWxzZTs3PTYzNTE4ODYwODAwMDAwMDAwMDs4PTQxRDA3NEFFODJFQjI3QjM3RDdGMTUzQ0REQjVEQkNFNEVGRjdGREU5MEIwOTg1MjkwQ0JDREFCQTM3MEFBNzU7OT0xLjQxLjAuMA";
        //    ocr.ResourceFolder = @"C:\Aquaforest\OCRSDK\bin";
        //    ocr.EnableConsoleOutput = true;
        //    ocr.EnableTextOutput = true;
        //    //ocr.ReadBMPSource(@"C:\Users\KotaruV\Documents\Visual Studio 2012\Projects\Playground\OCRConsoleApp\OCRConsoleApp\images\3.jpg");
        //    ocr.ReadTIFFSource(@"C:\Users\KotaruV\Documents\Visual Studio 2012\Projects\Playground\OCRConsoleApp\OCRConsoleApp\images\4.tif");
        //    ocr.Recognize(preProcessor);
        //    ocr.SaveTextOutput(@"C:\Temp\1.txt", true);
        //    ocr.DeleteTemporaryFiles();
        //    return string.Empty;
        //}

        #endregion

        public async Task<HttpResponseMessage> PostFormData()
        {
            StringBuilder debugMessage = new StringBuilder();
            debugMessage.Append("Begin. "); 
            string fileUri = string.Empty;
            string messageCaption = string.Empty, taggedUserEmail = string.Empty;

            debugMessage.Append("Variables create. ");

            // Check if the request contains multipart/form-data.
            if (!Request.Content.IsMimeMultipartContent())
            {
                debugMessage.Append("Identified not a multipart post message. ");
                // if not throw exception
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            debugMessage.Append("Identified as multipart post message. ");

            // location to store data, in this case images.
            string root = HttpContext.Current.Server.MapPath("~/App_Data");
            var provider = new MultipartFormDataStreamProvider(root);

            debugMessage.Append("Found root path to be " + root + ". ");

            try
            {
                // Read the form data.
                await Request.Content.ReadAsMultipartAsync(provider);

                debugMessage.Append("Attempted to read form data. ");
                // Get image file. It's already saved in App Data folder
                foreach (MultipartFileData file in provider.FileData)
                {
                    fileUri = file.LocalFileName;
                    debugMessage.Append("File Name " + fileUri + ". ");
                }

                //string outputFileName = DateTime.Now.Date.ToString() + DateTime.Now.Month.ToString() +
                                        //DateTime.Now.Year.ToString() + DateTime.Now.Hour.ToString() +
                                        //DateTime.Now.Minute.ToString() + DateTime.Now.Second.ToString() +
                                        //DateTime.Now.Millisecond.ToString();

                //outputFileName = outputFileName + ".txt";

                //debugMessage.Append("Output file name " + outputFileName + ". ");

                System.Diagnostics.EventLog.WriteEntry("Application", debugMessage.ToString(), System.Diagnostics.EventLogEntryType.Information);

                var ocrText = processImage(fileUri, root + @"\" + "1.txt");

                var httpResponse = Request.CreateResponse(HttpStatusCode.OK);
                httpResponse.Content = new StringContent(ocrText);
                //httpResponse.Headers.Add("returnText", ocrText);

                return httpResponse;
            }
            catch (System.Exception e)
            {
                debugMessage.Append("Error " + e.Message + ". ");
                System.Diagnostics.EventLog.WriteEntry("Application", debugMessage.ToString(), System.Diagnostics.EventLogEntryType.Error);

                System.Diagnostics.EventLog.WriteEntry("Application", e.InnerException.ToString(), System.Diagnostics.EventLogEntryType.Error);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
                
            }
            finally
            {
                
            }
        }

        private string processImage(string imageInput, string outputFilePath)
        {
            var debugMessage = new StringBuilder();
            try
            {
                debugMessage.Append("Process Image output file path " + outputFilePath);
                var ocr = new Ocr();
                var preProcessor = new PreProcessor();

                debugMessage.Append("Objects created");

                preProcessor.Deskew = true;
                preProcessor.Autorotate = false;
                preProcessor.RemoveLines = true;
                preProcessor.Binarize = 96;
                preProcessor.Morph = "c2.2";

                ocr.License =
               "MD1BZHZhbmNlZDsxPWtlZXJ0aSAtIGt2a2lydGh5QGdtYWlsLmNvbTsyPTUyNDY4Njg5OTE5MTMwMjg5NTI7Mz1rZWVydGkgLSBrdmtpcnRoeUBnbWFpbC5jb207ND05OTk7NT1UcnVlOzUuMT1GYWxzZTs3PTYzNTE4ODYwODAwMDAwMDAwMDs4PTQxRDA3NEFFODJFQjI3QjM3RDdGMTUzQ0REQjVEQkNFNEVGRjdGREU5MEIwOTg1MjkwQ0JDREFCQTM3MEFBNzU7OT0xLjQxLjAuMA";
                
                ocr.ResourceFolder = @"C:\Aquaforest\OCRSDK\bin";
                ocr.EnableConsoleOutput = true;
                ocr.EnableTextOutput = true;

                //ocr.ReadBMPSource(@"C:\Users\KotaruV\Documents\Visual Studio 2012\Projects\Playground\OCRConsoleApp\OCRConsoleApp\images\3.jpg");
                //ocr.ReadTIFFSource(imageInput);
                ocr.ReadBMPSource(imageInput);

                debugMessage.Append("Read from bmp source. ");
                ocr.Recognize(preProcessor);
                debugMessage.Append("Recognize executed. ");
                
                ocr.SaveTextOutput(outputFilePath, true);
                var ocrText = getOcrTextFromFile(outputFilePath);
                debugMessage.Append("Saved text output. ");
                ocr.DeleteTemporaryFiles();
                debugMessage.Append("Deleted temporary files. ");
                return ocrText;
            }
            finally
            {
                System.Diagnostics.EventLog.WriteEntry("Application", debugMessage.ToString(), System.Diagnostics.EventLogEntryType.Error);
            }

        }

        private string getOcrTextFromFile(string filePath)
        {
            StreamReader reader = null;
            try
            {
                reader = new StreamReader(filePath);
                return reader.ReadToEnd();
            }
            finally
            {
                reader.Close();
            }
        }
    }
}
