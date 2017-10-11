using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Thomsonreuters.Developer.Example.TRTHRESTAPI.RawExtraction;

namespace Thomsonreuters.Developer.Example.TRTHRESTAPI.Data
{
    
    public interface ODataContextInterface
    {
        [JsonProperty("odata.context",Order =1)]
        string Metadata { get; set; }
    }
    public interface ODataTypeInterface
    {
        [JsonProperty("@odata.type",Order =1)]

        string Metadata { get; set; }
    }
    [Serializable]
    class AuthorizeResponse : ODataContextInterface
    {
        public string Metadata { get; set; }
        public string value { get; set; }
    }
    [Serializable]
    class ValidateToken : ODataContextInterface
    {
        public string Metadata { get; set; }
        private bool _isValid;
        private DateTime _expires;

        public bool IsValid
        {
            get { return _isValid; }
            set { _isValid = value; }
        }
        public DateTime Expires
        {
            get { return _expires; }
            set { _expires = value; }
        }
       

    }
    [Serializable]
    public class TokenInfo
    {
        public TokenInfo() { }

        private string _token;
        private bool _isValid;
        private DateTime _dateTime;

        public string Token
        {
            get { return _token; }
            set { _token = value; }
        }
        public bool IsValid
        {
            get { return _isValid; }
            set { _isValid = value; }
        }
        public DateTime Expires
        {
            get { return _dateTime; }
            set { _dateTime = value; }
        }
        public override string ToString()
        {
            return String.Format("====================\nToken={0}\nIsValid={1}\nExpires={2}\n====================",
                         Token, IsValid, Expires);
        }
    }

    [Serializable]
    public class Credentials
    {
        public Credentials() {
            _username = String.Empty;
            _password = String.Empty;
        }
        private String _username { get; set; }
        private String _password { get; set; }
        public String Username { get { return _username; } set { _username = value; } }
        public String Password { get { return _password; } set { _password = value; } }
        
    }

    [Serializable]
    public class IdentifierReqeust
    {
        private string _identifier;
        private string _identifierType;
        public string Identifier { get { return _identifier; } set { _identifier = value; } }
        public string IdentifierType { get { return _identifierType; } set { _identifierType = value; } }
    }

    [Serializable]
    public class IdentifierRequestList : ODataTypeInterface
    {
        public IdentifierRequestList()
        {
            Metadata = "#ThomsonReuters.Dss.Api.Extractions.ExtractionRequests.InstrumentIdentifierList";
            _instrumentIdentifiers = new List<IdentifierReqeust>();
        }
        public string Metadata { get; set; }
        List<IdentifierReqeust> _instrumentIdentifiers;
        [JsonProperty(Order = 2)]
        public List<IdentifierReqeust> InstrumentIdentifiers { get { return _instrumentIdentifiers; } set { _instrumentIdentifiers = value; } }

    }
   
    [Serializable]
    public class TimeAndSalesRequestCondition
    {
        private string _messageTimeStampIn;
        private bool _applyCorrectionsAndCancellations;
        private string _reportDateRangeType;
        private string _queryStartDate;
        private string _queryEndDate;
        private bool _displaySourceRIC;

        public string MessageTimeStampIn { get { return _messageTimeStampIn; } set { _messageTimeStampIn = value; } }
        public bool ApplyCorrectionsAndCancellations { get { return _applyCorrectionsAndCancellations; } set { _applyCorrectionsAndCancellations = value; } }
        public string ReportDateRangeType { get { return _reportDateRangeType; } set { _reportDateRangeType = value; } }
        public string QueryStartDate { get { return _queryStartDate; } set { _queryStartDate = value; } }
        public string QueryEndDate { get { return _queryEndDate; } set { _queryEndDate = value; } }
        public bool DisplaySourceRIC { get { return _displaySourceRIC; } set { _displaySourceRIC = value; } }
    }

    [Serializable]
    public class EoDRequestCondition
    {

        private string _startDate;
        private string _endDate;

        public string StartDate { get { return _startDate; } set { _startDate = value; } }
        public string EndDate { get { return _endDate; } set { _endDate = value; } }

    }
    [Serializable]
    public class RawExtractionRequest: ODataTypeInterface
    {
        public RawExtractionRequest()
        {
            _identifierList = new IdentifierRequestList();
            _contentFieldNames = new List<string>();
        }
        protected string _metaData;
        protected List<string> _contentFieldNames;
        protected IdentifierRequestList _identifierList;
        public string Metadata { get { return _metaData; } set { _metaData = value; } }
        [JsonProperty(Order = 2)]
        public List<string> ContentFieldNames { get { return _contentFieldNames; } set { _contentFieldNames = value; } }
        [JsonProperty(Order = 3)]
        public IdentifierRequestList IdentifierList { get { return _identifierList; } set { _identifierList = value; } }
    }
    [Serializable]
    public class TimeAndSalesRequest: RawExtractionRequest
    {
        public TimeAndSalesRequest()
        {
            _condition = new TimeAndSalesRequestCondition();
            _metaData = "#ThomsonReuters.Dss.Api.Extractions.ExtractionRequests.TickHistoryTimeAndSalesExtractionRequest";
        }
        TimeAndSalesRequestCondition _condition;
        [JsonProperty(Order = 4)]
        public TimeAndSalesRequestCondition Condition { get { return _condition; } set { _condition = value; } }
    }

    [Serializable]
    public class EoDRequest : RawExtractionRequest
    {
        public EoDRequest()
        {
            _condition = new EoDRequestCondition();
            _metaData = "#ThomsonReuters.Dss.Api.Extractions.ExtractionRequests.ElektronTimeseriesExtractionRequest";
        }
        EoDRequestCondition _condition;
        [JsonProperty(Order = 4)]
        public EoDRequestCondition Condition { get { return this._condition; } set { this._condition = value; } }
    }

    [Serializable]
    class PollStatusResponseMsg : ODataContextInterface
    {
        string _jobID;
        List<string> _notes;
        public string Metadata { get; set; }
        public string JobID { get { return _jobID; } set { _jobID = value; } }
        public List<string> Notes { get { return _notes; } set { _notes = value; } }
    }
}
