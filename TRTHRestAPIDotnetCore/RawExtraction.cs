using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.IO;
using Thomsonreuters.Developer.Example.TRTHRESTAPI.Data;
namespace Thomsonreuters.Developer.Example.TRTHRESTAPI.RawExtraction
{
    public enum TickHistoricalRawExtractionMetaEnum
    {
        TimeAndSale,
        EndOfDay
    };
    
    public class RawExtractionManager
    {
        RawExtractionRequest _extractionRequest;
        public RawExtractionManager()
        {
            _extractionRequest = new RawExtractionRequest();
            _rawExtractionUri = new Uri("https://hosted.datascopeapi.reuters.com/RestApi/v1/Extractions/ExtractRaw");
        }
        private Uri _rawExtractionUri;
        [JsonIgnore]
        public Uri RawExtractionUri { get { return _rawExtractionUri; } set { _rawExtractionUri = value; } }
        public RawExtractionRequest ExtractionRequest { get { return _extractionRequest; } set { _extractionRequest = value; } }
        public async Task<bool> SendRAWExtractionRequest(string dssToken, string extractionRequestContent, string outputFileName, bool autoDecompress = false,bool downloadFromAmzS3=false)
        {
            var handler = new HttpClientHandler() { AllowAutoRedirect = false, PreAuthenticate = false };
            if (autoDecompress)
                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (HttpClient client = new HttpClient(handler))
            {
                // ***Step1 Send RawExtraction Request***
                Console.WriteLine("Sending RawExtraction Request");
                Console.WriteLine("Waiting for response from server...");
              
                // Create Http Request and set header and request content Set HttpMethod to Post request.
                var extractionRequest = new HttpRequestMessage(HttpMethod.Post, _rawExtractionUri);
                extractionRequest.Headers.Add("Prefer", "respond-async");
                extractionRequest.Headers.Authorization = new AuthenticationHeaderValue(dssToken);
                extractionRequest.Content = new StringContent(extractionRequestContent);
                extractionRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                // Call SendAsync to send RAW Extraction
                var extractionResponse = await client.SendAsync(extractionRequest);

                Uri location = null;
                var statusResponseContent = String.Empty;
                if((extractionResponse.StatusCode != System.Net.HttpStatusCode.OK) && (extractionResponse.StatusCode != System.Net.HttpStatusCode.Accepted))
                {
                    Console.WriteLine("Request Failed Status Code:{0} Reason:{1}", extractionResponse.StatusCode, extractionResponse.ReasonPhrase);
                    return false;
                }
                
                if (extractionResponse.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    System.Console.WriteLine("Request Accepted");
                    location = extractionResponse.Headers.Location;
                    Console.WriteLine("Location: {0}", extractionResponse.Headers.Location);
                    Console.WriteLine("Polling Request status");
                    // *** Step2 Polling the status using the location provied with response from previous step.***
                    do
                    {
                        // Create a new HttpRequest and set HttpMethod to Get and pass location from previous steps to request Uri.
                        var extractionStatusRequest = new HttpRequestMessage(HttpMethod.Get, location);
                        extractionStatusRequest.Headers.Add("Prefer", "respond-async");
                        extractionStatusRequest.Headers.Authorization = new AuthenticationHeaderValue(dssToken);
                        var resp = await client.SendAsync(extractionStatusRequest);
                        // Show status 
                        IEnumerable<string> statusValue;
                        if (resp.Headers.TryGetValues("Status", out statusValue))
                        {
                            Console.WriteLine("Request Status:{0}", statusValue.First());
                        }
                        if (resp.StatusCode != HttpStatusCode.OK)
                            await Task.Delay(30000); // Wait for 30 sec accroding to TRTH Document.
                        else
                        {
                            statusResponseContent = await resp.Content.ReadAsStringAsync();
                            break;
                        }
                    } while (true);
                }else
                    statusResponseContent = await extractionResponse.Content.ReadAsStringAsync();

                Console.WriteLine("Request completed");
                // Deserialize response and get JobId and Notes from response message.
                var pollStatusObj = JsonConvert.DeserializeObject<PollStatusResponseMsg>(statusResponseContent);
                Console.WriteLine("Recevied JobID={0}\nNotes\n", pollStatusObj.JobID);
                Console.WriteLine("========================================");
                foreach (var note in pollStatusObj.Notes)
                {

                    Console.WriteLine(note);
                }
                Console.WriteLine("========================================");

                //*** Step3 retreive the data from Tick Historical Server using the JobID received from previous steps.

                //Contruct data retreival Uri using the JobId
                Uri retreiveDataUri = new Uri(String.Format("https://hosted.datascopeapi.reuters.com/RestApi/v1/Extractions/RawExtractionResults('{0}')/$value", (string)pollStatusObj.JobID));
                Console.WriteLine("Retreiving data from endpoint {0}", retreiveDataUri);

                // Create a new request and set HttpMethod to Get and set AcceptEncoding to gzip and defalte
                // The application will recieve data as gzip stream with CSV format.
                var retrieveDataRequest = new HttpRequestMessage(HttpMethod.Get, retreiveDataUri);
                retrieveDataRequest.Headers.Authorization = new AuthenticationHeaderValue(dssToken);
                //retrieveDataRequest.Headers.Add("Authorization", dssToken);
                // Add custom header to HttpClient to download data from AWS server
                if(downloadFromAmzS3)
                    retrieveDataRequest.Headers.Add("X-Direct-Download", "True");

                retrieveDataRequest.Headers.Add("Prefer", "respond-async");
                retrieveDataRequest.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                retrieveDataRequest.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
                Console.WriteLine("Here is request message\n {0}", retrieveDataRequest);


                var getDataResponse = await client.SendAsync(retrieveDataRequest);
                if (getDataResponse.StatusCode == HttpStatusCode.OK)
                {
                    Console.WriteLine("Data retrieveal completed\nWriting data to {0}", outputFileName);
                    using (var fileStream = File.Create(outputFileName))
                    {
                        await getDataResponse.Content.CopyToAsync(fileStream);
                    }
                    Console.WriteLine("Write data to {0} completed ", outputFileName);
                }
                else
                // Handle Redirect in case of application want to download data from amazon.
                if (getDataResponse.StatusCode == HttpStatusCode.Redirect)
                {
                    Console.WriteLine("Get Redirect, retrieving data from Amazon S3 Uri:{0}\n", getDataResponse.Headers.Location);
                    var retrieveAmzRequest = new HttpRequestMessage(HttpMethod.Get, getDataResponse.Headers.Location);
                    retrieveAmzRequest.Headers.Add("Prefer", "respond-async");
                    retrieveAmzRequest.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                    retrieveAmzRequest.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
                    var amzResponse = await client.SendAsync(retrieveAmzRequest);
                    Console.WriteLine("Amazon S3 Data retrieveal completed\nWriting data to {0}", outputFileName);
                    using (var fileStream = File.Create(outputFileName))
                    {
                        await amzResponse.Content.CopyToAsync(fileStream);
                    }
                    Console.WriteLine("Write data to {0} completed ", outputFileName);
                }
                else
                {
                    Console.WriteLine("Unable to get data Status Code:{0} Reason:{1}", getDataResponse.StatusCode, getDataResponse.ReasonPhrase);
                    return false;
                }

            }

            return true;
        }

    }
}