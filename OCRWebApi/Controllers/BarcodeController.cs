using KeepDynamic.BarcodeReader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace OCRWebApi.Controllers
{
    public class BarcodeController : ApiController
    {

        //TODO: Use DI
        public string processImage(string image)
        {
            string[] barcodeValues = BarcodeReader.read(image, KeepDynamic.BarcodeReader.Type.CODE39);
            return barcodeValues[0];
        }

        public async Task<HttpResponseMessage> PostFormData()
        {
            //TODO: remove debug message here. Use Logging that's injected.
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

                System.Diagnostics.EventLog.WriteEntry("Application", debugMessage.ToString(), System.Diagnostics.EventLogEntryType.Information);

                var response = processImage(fileUri);

                var httpResponse= Request.CreateResponse(HttpStatusCode.OK);
                httpResponse.Content = new StringContent(response);
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
    }
}
