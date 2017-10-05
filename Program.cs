using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;


namespace TRTHTestDotNetCore
{
    public interface IODataObj
    {
        [JsonProperty("odata.context")]
        string Metadata { get; set; }
    }
    public interface IODataRequestObj
    {
        [JsonProperty("@odata.type")]
        string Metadata { get; set; }
    }
    [Serializable]
    class AuthorizeResponse: IODataObj
    {
        public string Metadata { get; set; }
        public string value { get; set; }
    }
    [Serializable]
    public class Credentials
    {
        String _username { get; set; }
        String _password { get; set; }
        public String Username { get { return _username; } set { _username = value; } }
        public String Password { get { return _password; } set { _password = value; } }
        public static Credentials Create(string username, string password)
        {
            return new Credentials
            {
                Username = username,
                Password = password
            };
        }
    }
    [Serializable]
    class Authorization
    {
        Credentials _credential;
        public Credentials Credentials { get { return _credential; } set { _credential = value; } }
        public async static Task<string> GetToken(string username, string password)
        {
            string returnToken = "";
            string AuthenURI = @"https://hosted.datascopeapi.reuters.com/RestApi/v1/Authentication/RequestToken";
            using (HttpClient client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, new Uri(AuthenURI));
                request.Headers.Add("Prefer", "respond-async");
                var credential = Credentials.Create(username, password);

                request.Content= new StringContent(JsonConvert.SerializeObject(new Authorization {
                                 Credentials = Credentials.Create(username, password) }, Formatting.Indented));

                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var response = await client.SendAsync(request);
                if(response.IsSuccessStatusCode)
                { 
                    var jsonData = await response.Content.ReadAsStringAsync();
                    returnToken = Newtonsoft.Json.JsonConvert.DeserializeObject<AuthorizeResponse>(jsonData).value;
                    
                }else
                {
                    throw new Exception(String.Format("Unable to Login to Tick Historical Server\n {0}", response.ToString()));
                    //Console.WriteLine("Unable to login: {0}",response.ToString());
                }
                return returnToken;
            }
            
            
        }

    }
   
    [Serializable]
    class IdentifierReqeust
    {
        string _identifier;
        string _identifierType;
        public string Identifier { get { return _identifier;  } set { _identifier = value; } }
        public string IdentifierType { get { return _identifierType; } set { _identifierType = value; } }
    }
    [Serializable]
    class IdentifierRequestList: IODataRequestObj
    {
        public IdentifierRequestList()
        {
            Metadata = "#ThomsonReuters.Dss.Api.Extractions.ExtractionRequests.InstrumentIdentifierList";
            _instrumentIdentifiers = new List<IdentifierReqeust>();
        }
        public string Metadata { get; set; }
        List<IdentifierReqeust> _instrumentIdentifiers;
        public List<IdentifierReqeust> InstrumentIdentifiers { get { return _instrumentIdentifiers; } set { _instrumentIdentifiers = value; } }

    }
    [Serializable]
    class RequestCondition
    {
        string _messageTimeStampIn;
        bool _applyCorrectionsAndCancellations;
        string _reportDateRangeType;
        string _queryStartDate;
        string _queryEndDate;
        bool _displaySourceRIC;

        public string MessageTimeStampIn { get { return _messageTimeStampIn; } set { _messageTimeStampIn = value; } }
        public bool ApplyCorrectionsAndCancellations { get { return _applyCorrectionsAndCancellations; } set { _applyCorrectionsAndCancellations = value; } }
        public string ReportDateRangeType { get { return _reportDateRangeType; } set { _reportDateRangeType = value; } }
        public string QueryStartDate { get { return _queryStartDate; } set { _queryStartDate = value; } }
        public string QueryEndDate { get { return _queryEndDate; } set { _queryEndDate = value; } }
        public bool DisplaySourceRIC { get { return _displaySourceRIC; } set { _displaySourceRIC = value; } }
    }
    [Serializable]
    class TRTHExtractionRequest:IODataRequestObj
    {
        public string Metadata { get; set; }
        List<string> _contentFieldNames;
        IdentifierRequestList _identifierList;
        RequestCondition _condition;
        public List<string> ContentFieldNames { get { return _contentFieldNames; } set { _contentFieldNames = value; } }
        public IdentifierRequestList IdentifierList { get { return _identifierList; } set { _identifierList = value; } }
        public RequestCondition Condition  { get { return _condition; } set { _condition = value; } }
        public static TRTHExtractionRequest Create(string metadata,List<string> contentfieldnames,IdentifierRequestList identifierlist,RequestCondition requestcondition)
        {
            return new TRTHExtractionRequest
            {
                Metadata = metadata,
                ContentFieldNames = contentfieldnames,
                IdentifierList = identifierlist,
                Condition = requestcondition
            };

        }
    }
    [Serializable]
    class PollStatusResponseMsg:IODataObj
    {
        string _jobID;
        List<string> _notes;
       public string Metadata { get; set; }
       public string JobID { get { return _jobID; } set { _jobID = value; } }
       public List<string> Notes { get { return _notes; } set { _notes = value; } }
    }
    class RAWExtractionManager
    {
        TRTHExtractionRequest _extractionRequest;
        public TRTHExtractionRequest ExtractionRequest { get { return _extractionRequest; } set { _extractionRequest = value; } }
        public async static Task<bool> SendRAWExtractionRequest(string dssToken,string extractionRequestContent,string outputFileName,bool autoDecompress=false)
        {
            var handler = new HttpClientHandler() { AllowAutoRedirect = false, PreAuthenticate = false };
            if (autoDecompress)
                handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            using (HttpClient client = new HttpClient(handler))
            {
                // ***Step1 Send RawExtraction Request***
                Console.WriteLine("Sending RawExtraction Request");
                Uri rawExtractionUri = new Uri("https://hosted.datascopeapi.reuters.com/RestApi/v1/Extractions/ExtractRaw");
                // Create Http Request and set header and request content Set HttpMethod to Post request.
                var extractionRequest = new HttpRequestMessage(HttpMethod.Post, rawExtractionUri);
                extractionRequest.Headers.Add("Prefer", "respond-async");
                extractionRequest.Headers.Authorization = new AuthenticationHeaderValue(dssToken);
                extractionRequest.Content = new StringContent(extractionRequestContent);
                extractionRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                // Call SendAsync to send RAW Extraction
                var extractionResponse = await client.SendAsync(extractionRequest);

                Uri location = null;

                if (extractionResponse.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    System.Console.WriteLine("Request Accepted");
                    location = extractionResponse.Headers.Location;
                    Console.WriteLine("Location: {0}", extractionResponse.Headers.Location);

                }
                else
                {
                    Console.WriteLine("Request Failed Status Code:{0} Reason:{1}", extractionResponse.StatusCode, extractionResponse.ReasonPhrase);
                    return false;
                }

                // *** Step2 Polling the status using the location provied with response from previous step.***
                Console.WriteLine("Polling Request status");
                var statusResponseContent = "";
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
                retrieveDataRequest.Headers.Add("X-Direct-Download", "True");
                retrieveDataRequest.Headers.Add("Prefer", "respond-async");
                retrieveDataRequest.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                retrieveDataRequest.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
                Console.WriteLine("Here is request message\n {0}",retrieveDataRequest);


                var getDataResponse = await client.SendAsync(retrieveDataRequest);
                Console.WriteLine("Here is Response Message\n{0}",getDataResponse);
                Console.WriteLine("Content below\n{0}",await getDataResponse.Content.ReadAsStringAsync());
                if (getDataResponse.StatusCode == HttpStatusCode.OK)
                {
                    Console.WriteLine("Data retrieveal completed\nWriting data to {0}", outputFileName);
                    using (var fileStream = File.Create(outputFileName))
                    {
                        await getDataResponse.Content.CopyToAsync(fileStream);
                    }
                    Console.WriteLine("Write data to {0} completed ", outputFileName);
                }else 
                if(getDataResponse.StatusCode==HttpStatusCode.Redirect)
                {
                    Console.WriteLine("Get Redirect, retrieving data from Amazon S3 Uri:{0}\n",getDataResponse.Headers.Location);
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
    class Program
    {

        static void Main(string[] args)
        {

            System.Console.WriteLine("Login to Tick Historical Server V2");
            System.Console.Write("Input your DSS username:");
            var dssUserName = System.Console.ReadLine();
            Console.Write("Input your DSS password:");
            var dssPassword = "";
            ConsoleKeyInfo keyinfo;
            do
            {
                keyinfo = Console.ReadKey(true);
                if (!Char.IsWhiteSpace(keyinfo.KeyChar))
                {
                    dssPassword += keyinfo.KeyChar;
                    Console.Write("*");
                }

            } while (keyinfo.Key != ConsoleKey.Enter);

            var dssToken = String.Format("Token{0}", GetToken(dssUserName, dssPassword).Result);
            if (!string.IsNullOrEmpty(dssToken))
            { 
                Console.WriteLine("Token is {0}", dssToken);
                // Prepare JSON Query
                List<string> fieldNames = new List<string>();
                fieldNames.Add("Trade - Price");
                fieldNames.Add("Trade - Volume");
                fieldNames.Add("Trade - Exchange Time");
                IdentifierRequestList identifierlilst = new IdentifierRequestList();
                identifierlilst.InstrumentIdentifiers.Add(new IdentifierReqeust { Identifier = "SCB.BK", IdentifierType = "Ric" });
                RequestCondition reqCondition = new RequestCondition();
                reqCondition.MessageTimeStampIn = "GmtUtc";
                reqCondition.ApplyCorrectionsAndCancellations = false;
                reqCondition.ReportDateRangeType = "Range";
                reqCondition.QueryStartDate = "2016-09-06T00:00:00.000Z";
                reqCondition.QueryEndDate = "2016-09-07T00:00:00.000Z";
                reqCondition.DisplaySourceRIC = true;
                var extractionRequestContent = JsonConvert.SerializeObject(new RAWExtractionManager
                {
                    ExtractionRequest = TRTHExtractionRequest.Create("#ThomsonReuters.Dss.Api.Extractions.ExtractionRequests.TickHistoryTimeAndSalesExtractionRequest", fieldNames, identifierlilst, reqCondition)
                }, Formatting.Indented);
                Console.WriteLine("Start TickHistorical Raw Extraction");
                Console.WriteLine("The application pass the following request to Tick Historical Server");
                Console.WriteLine("=======================");
                Console.WriteLine(extractionRequestContent);
                Console.WriteLine("=======================\n");
                var rawExtract=RAWExtractionManager.SendRAWExtractionRequest(dssToken, extractionRequestContent, "test.csv.gzip");
                rawExtract.Wait();
            }
            else
                Console.WriteLine("Login Failed");
         
            Console.ReadKey();

        }
        protected async static Task<string> GetToken(string username,string password)
        {
            try {
                var trthToken = await Authorization.GetToken(username, password);
                return trthToken;

            }catch(Exception e)
            {
                Console.WriteLine(e);
            }
            return null;
       }
        
    }
}
