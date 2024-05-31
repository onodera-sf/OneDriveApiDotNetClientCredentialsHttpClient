using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json;
using System.Web;
using System.Xml;

// 各種 ID などの定義
var cliendId = "XXXXXXXX";      // クライアント ID
var tenantId = "XXXXXXXX";      // テナント ID
var clientSecret = "XXXXXXXX";  // クライアント シークレット
var userId = "XXXXXXXX";        // ユーザー ID
var accessToken = "";           // アクセストークン

// 各種 URL の定義
var urlToken = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
var urlOneDriveGetRootFolders = $"https://graph.microsoft.com/v1.0/users/{userId}/drive/root/children";

// 使いまわすので最初に定義しておく
HttpClient httpClient = new();

// 「Name1=Value1&Name2=Value2...」形式のクエリパラメータ文字列を作成する
var valueDict = new Dictionary<string, string>
{
    { "client_id", cliendId },
    { "scope", "https://graph.microsoft.com/.default" },
    { "client_secret", clientSecret },
    { "grant_type", "client_credentials" },
};
var valueQS = string.Join("&", valueDict.Select(x => string.Format("{0}={1}", x.Key, HttpUtility.UrlEncode(x.Value))));
Console.WriteLine($"valueQS={valueQS}");

// 認証処理
using (HttpRequestMessage request = new(HttpMethod.Post, urlToken))
{
  request.Content = new StringContent(valueQS, Encoding.UTF8, "application/x-www-form-urlencoded");

  // データを送信し結果を受け取る
  using var response = httpClient.SendAsync(request).Result;

  Console.WriteLine($"IsSuccessStatusCode={response.IsSuccessStatusCode}");
  Console.WriteLine($"StatusCode={response.StatusCode}");
  Console.WriteLine($"ReasonPhrase={response.ReasonPhrase}");

  if (response.IsSuccessStatusCode == false)
  {
    Console.WriteLine("トークンの取得に失敗しました。");
    return;
  }

  // レスポンスからアクセストークンを受け取る。
  var tokenResultJson = response.Content.ReadAsStringAsync().Result;
  Console.WriteLine($"tokenResultJson={ConvertToIndentedJson(tokenResultJson)}");

  var tokenResult = JsonSerializer.Deserialize<TokenResponse>(tokenResultJson) ?? throw new Exception("JSON をデシリアライズできませんでした。");
  accessToken = tokenResult.access_token;
}

// OneDrive 処理
using (HttpRequestMessage request = new(HttpMethod.Get, urlOneDriveGetRootFolders))
{
  // アクセストークンをヘッダーに追加する
  request.Headers.Add("Authorization", "Bearer " + accessToken);

  // OneDrive API にリクエスト
  using var response = httpClient.SendAsync(request).Result;

  Console.WriteLine($"IsSuccessStatusCode={response.IsSuccessStatusCode}");
  Console.WriteLine($"StatusCode={response.StatusCode}");
  Console.WriteLine($"ReasonPhrase={response.ReasonPhrase}");

  if (response.IsSuccessStatusCode == false)
  {
    Console.WriteLine("OneDrive へのアクセスに失敗しました。");
    return;
  }

  // レスポンスからアクセストークンを受け取る。
  var apiResultJson = response.Content.ReadAsStringAsync().Result;
  Console.WriteLine($"apiResultJson={ConvertToIndentedJson(apiResultJson)}");
}


// JSON 文字列を整形するメソッド
static string ConvertToIndentedJson(string json)
{
  byte[] buffer = Encoding.UTF8.GetBytes(json);
  using MemoryStream stream = new();
  using XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, true, true);
  using XmlDictionaryReader reader = JsonReaderWriterFactory.CreateJsonReader(buffer, XmlDictionaryReaderQuotas.Max);
  writer.WriteNode(reader, true);
  writer.Flush();
  return Encoding.UTF8.GetString(stream.ToArray());
}

// 認証結果を格納するクラスです
public record TokenResponse(string token_type, int expires_in, int ext_expires_in, string access_token);
