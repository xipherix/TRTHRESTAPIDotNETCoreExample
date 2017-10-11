using System;
using System.Collections.Generic;
using Thomsonreuters.Developer.Example.TRTHRESTAPI.Security;
using Thomsonreuters.Developer.Example.TRTHRESTAPI.Data;
using Thomsonreuters.Developer.Example.TRTHRESTAPI.RawExtraction;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Request new Token from the server");
            var tokenAuthenticate = new Authentication();
            tokenAuthenticate.Credentials.Username = "<Your DSS Username>";
            tokenAuthenticate.Credentials.Password = "<Your DSS Password>";
            var dssToken=String.Format("Token{0}",tokenAuthenticate.GetToken().Result);
            Console.WriteLine("Token={0}",dssToken);

            // Prepare JSON Query
            //var extractionRequest = new TimeAndSalesRequest();
            var extractionRequest = new EoDRequest();
            //Set Contents Field Name
            //// Add RIC
            extractionRequest.IdentifierList.InstrumentIdentifiers.Add(new IdentifierReqeust { Identifier = "SCB.BK", IdentifierType = "Ric" });
            //For TimeAndSales request
            //extractionRequest.ContentFieldNames.Add("Trade - Price");
            //extractionRequest.ContentFieldNames.Add("Trade - Volume");
            //extractionRequest.ContentFieldNames.Add("Trade - Exchange Time");

            //For EoD Request
            extractionRequest.ContentFieldNames.Add("Instrument ID");
            extractionRequest.ContentFieldNames.Add("High");
            extractionRequest.ContentFieldNames.Add("Low");
            extractionRequest.ContentFieldNames.Add("Last");
            extractionRequest.ContentFieldNames.Add("Volume");
            extractionRequest.ContentFieldNames.Add("VWAP");
         
            // Set Condition
            // For Times And Sales Request
            //extractionRequest.Condition.MessageTimeStampIn = "GmtUtc";
            //extractionRequest.Condition.ApplyCorrectionsAndCancellations = false;
            //extractionRequest.Condition.ReportDateRangeType = "Range";
            //extractionRequest.Condition.QueryStartDate = "2016-09-06T00:00:00.000Z";
            //extractionRequest.Condition.QueryEndDate = "2016-09-07T00:00:00.000Z";
            //extractionRequest.Condition.DisplaySourceRIC = true;

            //For EoD
            extractionRequest.Condition.StartDate = "2000-01-06T00:00:00.000Z";
            extractionRequest.Condition.EndDate = "2016-09-07T00:00:00.000Z";


            var rawExtractionMgr = new RawExtractionManager();
            rawExtractionMgr.ExtractionRequest = extractionRequest;

            var extractionRequestContent = JsonConvert.SerializeObject(rawExtractionMgr,Formatting.Indented);
            
            Console.WriteLine("Start TickHistorical Raw Extraction");
            Console.WriteLine("The application pass the following request to Tick Historical Server");
            Console.WriteLine("=======================");
            Console.WriteLine(extractionRequestContent);
            Console.WriteLine("=======================\n");
            var rawExtract =  rawExtractionMgr.SendRAWExtractionRequest(dssToken, extractionRequestContent, "test.csv.gzip");
            rawExtract.Wait();
            Console.ReadKey();
        }
    }
}
