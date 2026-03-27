using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace arches_arcgispro_addin
{
    internal class ArchesConfig
    {
        [JsonProperty("instance_url")]
        public string InstanceUrl { get; set; }

        [JsonProperty("client_id")]
        public string ClientId { get; set; }

        [JsonProperty("callback_port")]
        public int? CallbackPort { get; set; }

        [JsonProperty("auth_timeout_seconds")]
        public int? AuthTimeoutSeconds { get; set; }
    }

    public static class OAuthHelper
    {
        private const int DefaultCallbackPort = 53821;
        private const int DefaultAuthTimeoutSeconds = 180;
        private const string TokenFileName = "arches_addin_tokens.json";

        private static int CallbackPort = DefaultCallbackPort;
        private static int AuthTimeoutSeconds = DefaultAuthTimeoutSeconds;
        private static string RedirectUri => $"http://127.0.0.1:{CallbackPort}/callback/";

        private static string TokenFilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ArchesArcGISProAddIn",
            TokenFileName
        );

        /// <summary>
        /// Load instance URL and client ID from arches_config.json bundled with the add-in.
        /// Returns true if the config is valid and ready to use, false if setup is needed.
        /// </summary>
        public static bool LoadConfig()
        {
            string configPath = GetConfigPath();

            if (!File.Exists(configPath))
                return false;

            string json = File.ReadAllText(configPath);
            var config = JsonConvert.DeserializeObject<ArchesConfig>(json);

            if (string.IsNullOrWhiteSpace(config.InstanceUrl) ||
                string.IsNullOrWhiteSpace(config.ClientId) ||
                config.InstanceUrl.Contains("YOUR_") ||
                config.ClientId.Contains("YOUR_"))
            {
                return false;
            }

            StaticVariables.archesInstanceURL = config.InstanceUrl.TrimEnd('/') + "/";
            StaticVariables.myClientid = config.ClientId;
            CallbackPort = config.CallbackPort ?? DefaultCallbackPort;
            AuthTimeoutSeconds = config.AuthTimeoutSeconds ?? DefaultAuthTimeoutSeconds;
            return true;
        }

        /// <summary>
        /// Save instance URL and client ID to arches_config.json.
        /// </summary>
        public static void SaveConfig(string instanceUrl, string clientId)
        {
            string configPath = GetConfigPath();

            // Read existing config to preserve optional settings
            ArchesConfig config;
            if (File.Exists(configPath))
            {
                string existingJson = File.ReadAllText(configPath);
                config = JsonConvert.DeserializeObject<ArchesConfig>(existingJson);
            }
            else
            {
                config = new ArchesConfig();
            }

            config.InstanceUrl = instanceUrl;
            config.ClientId = clientId;

            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(configPath, json);

            // Reload so static variables are set
            LoadConfig();
        }

        private static string GetConfigPath()
        {
            string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(assemblyDir, "arches_config.json");
        }

        /// <summary>
        /// Run the full OAuth2 Authorization Code + PKCE flow via the system browser.
        /// </summary>
        public static async Task<Dictionary<string, dynamic>> AuthorizeAsync()
        {
            string codeVerifier = GenerateCodeVerifier();
            string codeChallenge = GenerateCodeChallenge(codeVerifier);
            string state = GenerateRandomString(16);

            using var listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{CallbackPort}/callback/");
            listener.Start();

            try
            {
                string authorizeUrl =
                    $"{StaticVariables.archesInstanceURL}o/authorize/" +
                    $"?response_type=code" +
                    $"&client_id={Uri.EscapeDataString(StaticVariables.myClientid)}" +
                    $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
                    $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
                    $"&code_challenge_method=S256" +
                    $"&state={Uri.EscapeDataString(state)}";

                Process.Start(new ProcessStartInfo
                {
                    FileName = authorizeUrl,
                    UseShellExecute = true
                });

                // Wait for the browser redirect with a timeout
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(AuthTimeoutSeconds));
                var listenerTask = listener.GetContextAsync();
                var completedTask = await Task.WhenAny(
                    listenerTask,
                    Task.Delay(Timeout.Infinite, cts.Token)
                );

                if (completedTask != listenerTask)
                {
                    throw new OperationCanceledException("Sign-in timed out. Please try again.");
                }

                var context = listenerTask.Result;
                string returnedCode = context.Request.QueryString["code"];
                string returnedState = context.Request.QueryString["state"];
                string error = context.Request.QueryString["error"];

                // Respond to the browser
                string responseHtml = "<html><body><h2>Sign-in complete</h2><p>You can close this tab and return to ArcGIS Pro.</p></body></html>";
                byte[] buffer = Encoding.UTF8.GetBytes(responseHtml);
                context.Response.ContentType = "text/html";
                context.Response.ContentLength64 = buffer.Length;
                await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                context.Response.Close();

                if (!string.IsNullOrEmpty(error))
                {
                    throw new Exception($"Authorization denied: {error}");
                }

                if (returnedState != state)
                {
                    throw new Exception("Invalid state parameter. Possible CSRF attack.");
                }

                if (string.IsNullOrEmpty(returnedCode))
                {
                    throw new Exception("No authorization code received.");
                }

                return await ExchangeCodeForTokensAsync(returnedCode, codeVerifier);
            }
            finally
            {
                listener.Stop();
            }
        }

        /// <summary>
        /// Exchange the authorization code for access and refresh tokens.
        /// </summary>
        private static async Task<Dictionary<string, dynamic>> ExchangeCodeForTokensAsync(
            string code, string codeVerifier)
        {
            HttpClient client = await ArchesHttpClient.GetHttpClient();
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", RedirectUri),
                new KeyValuePair<string, string>("client_id", StaticVariables.myClientid),
                new KeyValuePair<string, string>("code_verifier", codeVerifier),
            });

            var response = await client.PostAsync(
                StaticVariables.archesInstanceURL + "o/token/", content);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            dynamic responseJSON = JsonConvert.DeserializeObject<dynamic>(responseBody);

            var result = new Dictionary<string, dynamic>
            {
                { "access_token", (string)responseJSON["access_token"] },
                { "refresh_token", (string)responseJSON["refresh_token"] },
                { "expires_in", (double)responseJSON["expires_in"] },
                { "token_type", (string)responseJSON["token_type"] },
                { "scope", (string)responseJSON["scope"] },
                { "timestamp", DateTime.Now }
            };

            SaveRefreshToken((string)responseJSON["refresh_token"]);
            return result;
        }

        /// <summary>
        /// Refresh the access token using a stored refresh token.
        /// </summary>
        public static async Task<Dictionary<string, dynamic>> RefreshTokenAsync()
        {
            string refreshToken = StaticVariables.archesToken != null
                ? StaticVariables.archesToken["refresh_token"]
                : LoadRefreshToken();

            if (string.IsNullOrEmpty(refreshToken))
            {
                throw new InvalidOperationException("No refresh token available. Please sign in.");
            }

            HttpClient client = await ArchesHttpClient.GetHttpClient();
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", refreshToken),
                new KeyValuePair<string, string>("client_id", StaticVariables.myClientid),
            });

            var response = await client.PostAsync(
                StaticVariables.archesInstanceURL + "o/token/", content);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            dynamic responseJSON = JsonConvert.DeserializeObject<dynamic>(responseBody);

            var result = new Dictionary<string, dynamic>
            {
                { "access_token", (string)responseJSON["access_token"] },
                { "refresh_token", (string)responseJSON["refresh_token"] },
                { "expires_in", (double)responseJSON["expires_in"] },
                { "token_type", (string)responseJSON["token_type"] },
                { "scope", (string)responseJSON["scope"] },
                { "timestamp", DateTime.Now }
            };

            SaveRefreshToken((string)responseJSON["refresh_token"]);
            StaticVariables.archesToken = result;
            return result;
        }

        /// <summary>
        /// Attempt to silently refresh using a stored refresh token.
        /// Returns true if successful, false if the user needs to sign in via browser.
        /// </summary>
        public static async Task<bool> TrySilentRefreshAsync()
        {
            try
            {
                string refreshToken = LoadRefreshToken();
                if (string.IsNullOrEmpty(refreshToken))
                    return false;

                // Temporarily set the token so RefreshTokenAsync can use it
                StaticVariables.archesToken = new Dictionary<string, dynamic>
                {
                    { "refresh_token", refreshToken }
                };

                await RefreshTokenAsync();
                return true;
            }
            catch
            {
                StaticVariables.archesToken = null;
                return false;
            }
        }

        /// <summary>
        /// Encrypt and save the refresh token using DPAPI.
        /// </summary>
        private static void SaveRefreshToken(string refreshToken)
        {
            try
            {
                string dir = Path.GetDirectoryName(TokenFilePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                byte[] plainBytes = Encoding.UTF8.GetBytes(refreshToken);
                byte[] encrypted = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
                string encoded = Convert.ToBase64String(encrypted);

                var data = new JObject
                {
                    ["refresh_token_encrypted"] = encoded
                };

                File.WriteAllText(TokenFilePath, data.ToString());
            }
            catch
            {
                // Non-fatal: token persistence failed, user will need to sign in next time
            }
        }

        /// <summary>
        /// Load and decrypt the stored refresh token.
        /// </summary>
        private static string LoadRefreshToken()
        {
            try
            {
                if (!File.Exists(TokenFilePath))
                    return null;

                string json = File.ReadAllText(TokenFilePath);
                var data = JObject.Parse(json);
                string encoded = data["refresh_token_encrypted"]?.ToString();

                if (string.IsNullOrEmpty(encoded))
                    return null;

                byte[] encrypted = Convert.FromBase64String(encoded);
                byte[] decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decrypted);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Delete stored tokens (sign out).
        /// </summary>
        public static void ClearStoredTokens()
        {
            try
            {
                if (File.Exists(TokenFilePath))
                    File.Delete(TokenFilePath);
            }
            catch
            {
                // Non-fatal
            }
        }

        private static string GenerateCodeVerifier()
        {
            byte[] bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Base64UrlEncode(bytes);
        }

        private static string GenerateCodeChallenge(string codeVerifier)
        {
            using var sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(Encoding.ASCII.GetBytes(codeVerifier));
            return Base64UrlEncode(hash);
        }

        private static string GenerateRandomString(int byteLength)
        {
            byte[] bytes = new byte[byteLength];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Base64UrlEncode(bytes);
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }
    }
}
