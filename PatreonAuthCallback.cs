using System.Collections;
using System.Net;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class PatreonAuthCallback : MonoBehaviour
{
    private const string ClientId = "******";
    private const string ClientSecret = "******";
    private const string RedirectUri = "http://localhost:8000/redirect";
    private const string ServerEndpoint = "http://localhost:8000/";
    private const string TokenEndpoint = "https://www.patreon.com/api/oauth2/token";

    private HttpListener httpListener;

    private IEnumerator Start()
    {
        httpListener = new HttpListener();
        httpListener.Prefixes.Add(ServerEndpoint);
        httpListener.Start();

        string authUrl = BuildAuthUrl();

        Application.OpenURL(authUrl);

        yield return StartCoroutine(HandleCallback());

        httpListener.Close();
    }

    private string BuildAuthUrl()
    {
        string clientId = ClientId;
        string redirectUri = RedirectUri;
        string scope = "identity.memberships identity";

        string authUrl = $"https://www.patreon.com/oauth2/authorize?response_type=code&client_id={clientId}&redirect_uri={redirectUri}&scope={scope}";

        return authUrl;
    }

    private IEnumerator HandleCallback()
    {

        HttpListenerContext context = httpListener.GetContext();

        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        byte[] responseData = Encoding.UTF8.GetBytes("Callback Received! Close Browser to Continue!");
        response.ContentLength64 = responseData.Length;
        response.OutputStream.Write(responseData, 0, responseData.Length);
        response.OutputStream.Close();

        string code = request.QueryString["code"];

        // Use Code to Get AccessToken
        GetAccessToken(code);

        yield break;
    }

    public void GetAccessToken(string code)
    {
        StartCoroutine(ExchangeCodeForToken(code));
    }

    private IEnumerator ExchangeCodeForToken(string code)
    {
        var formData = new WWWForm();
        formData.AddField("code", code);
        formData.AddField("client_id", ClientId);
        formData.AddField("client_secret", ClientSecret);
        formData.AddField("redirect_uri", RedirectUri);
        formData.AddField("grant_type", "authorization_code");

        using (var request = UnityWebRequest.Post(TokenEndpoint, formData))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {

                var jsonResponse = request.downloadHandler.text;
  
                var response = JsonUtility.FromJson<PatreonTokenResponse>(jsonResponse);

                var accessToken = response.access_token;
                var refreshToken = response.refresh_token;
                var expiresIn = response.expires_in;
                var scope = response.scope;

                Debug.Log("Access Token: " + accessToken);
                Debug.Log("Refresh Token: " + refreshToken);
                Debug.Log("Expires In: " + expiresIn);
                Debug.Log("Scope: " + scope);

                // Use AccessToken To Get UserInfo
                StartCoroutine(GetUserInfo(accessToken));
            }
            else
            {
                Debug.LogError("Token Request Failed: " + request.error);
            }
        }
    }

    IEnumerator GetUserInfo(string accessToken)
    {
        string url = "https://www.patreon.com/api/oauth2/v2/identity?include=memberships.user&fields%5Buser%5D=full_name,email&fields%5Bcampaign%5D=summary,is_monthly&fields%5Bmember%5D=patron_status,lifetime_support_cents,full_name,will_pay_amount_cents";

        UnityWebRequest www = UnityWebRequest.Get(url);
        www.SetRequestHeader("Authorization", "Bearer " + accessToken);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Request failed: " + www.error);
        }
        else
        {
            Debug.Log("Response: " + www.downloadHandler.text);

            // Do Something Here. Response would be a json.
        }
    }

    [System.Serializable]
    private class PatreonTokenResponse
    {
        public string access_token;
        public string refresh_token;
        public int expires_in;
        public string scope;
    }

}
