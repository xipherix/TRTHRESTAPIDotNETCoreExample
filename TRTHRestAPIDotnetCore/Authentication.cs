using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Thomsonreuters.Developer.Example.TRTHRESTAPI.Data;

namespace Thomsonreuters.Developer.Example.TRTHRESTAPI.Security
{
    [Serializable]
    public class Authentication
    {
        public Authentication()
        {
            _credential = new Credentials();
            _authenUri = new Uri("https://hosted.datascopeapi.reuters.com/RestApi/v1/Authentication/RequestToken");
        }
        private Credentials _credential;
        private Uri _authenUri;
        public Credentials Credentials { get { return _credential; } set { _credential = value; } }
        [JsonIgnore]
        public Uri AuthenUri { get { return _authenUri; } set { _authenUri = value; } }

        public async Task<string> GetToken()
        {
            return await GetToken(_credential.Username, _credential.Password, _authenUri);
        }
        public async Task<string> GetToken(string username, string password)
        {
            return await GetToken(username, password, _authenUri);
        }
        public async Task<string> GetToken(string username, string password,Uri authenUri)
        {

            _credential.Username = username;
            _credential.Password = password;
            var returnToken = "";
            using (HttpClient client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post,authenUri);
                request.Headers.Add("Prefer", "respond-async");
                request.Content = new StringContent(JsonConvert.SerializeObject(new Authentication() { Credentials = this._credential }, Formatting.Indented));
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var jsonData = await response.Content.ReadAsStringAsync();
                    returnToken = Newtonsoft.Json.JsonConvert.DeserializeObject<AuthorizeResponse>(jsonData).value;
                }
                else
                {
                    throw new Exception(String.Format("Unable to Login to Tick Historical Server\n {0}", response.ToString()));
                }
                return returnToken;
            }

        }
        public async static Task<TokenInfo> IsValidToken(string token)
        {
            using (HttpClient client=new HttpClient())
            {
                var validateUri = new Uri("https://hosted.datascopeapi.reuters.com/RestApi/v1/Authentication/ValidateToken" + string.Format("(Token='{0}')",token));

                var resp=await client.GetAsync(validateUri);
                Console.WriteLine("Get Validate Token Result");
                var msg=await resp.Content.ReadAsStringAsync();
                var validateToken = Newtonsoft.Json.JsonConvert.DeserializeObject<ValidateToken>(msg);
                Console.WriteLine("IsValid={0} Expires={1}",validateToken.IsValid,validateToken.Expires);
                var tokenInfo = new TokenInfo();
                tokenInfo.Token = token;
                tokenInfo.IsValid = validateToken.IsValid;
                tokenInfo.Expires = validateToken.Expires;
                return tokenInfo;
            }
        }

    }
}
