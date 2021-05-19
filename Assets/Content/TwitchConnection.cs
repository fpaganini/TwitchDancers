using System;
using System.Collections.Generic;
using UnityEngine;
using TwitchLib.Unity;
using StreamingClient.Base.Web;
using TwitchLib.Api.V5;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Collections;

public class TwitchConnection : MonoBehaviour
{
    private Api _api;
    Twitch.Base.TwitchConnection connection;
    Twitch.Base.Models.V5.Users.UserModel CurrentUser;
    Twitch.Base.Models.V5.Channel.ChannelModel CurrentChannel;

    internal static Participante GetFirst()
    {
        UnityEngine.Debug.Log(TwitchConnection.participantes.Count);
        var item = (from d in TwitchConnection.participantes select d).DefaultIfEmpty(null).FirstOrDefault();
        UnityEngine.Debug.Log(TwitchConnection.participantes.Count);
        if (item != null)
        {
            participantes.RemoveAt(0);
        }
        item = new Participante();
        item.avatar = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQyW-iPJu5iPwkIQTBf8XFy29HHQjgS1-oAHg&usqp=CAU";
        item.nome = "Teste";
        
        return item;
    }

    private const string defaultSuccessResponse = "<!DOCTYPE html><html><body><h1 style=\"text-align:center;\">Logged In Successfully</h1><p style=\"text-align:center;\">You have been logged in, you may now close this webpage</p></body></html>";
    public static string authorizationCode = null;


    public List<string> NomesEventos = new List<string>();
    public static List<Solicitacao> Solicitacoes = new List<Solicitacao>();

    private PubSub _pubSub;

    public static TwitchConnection instance;

    // Start is called before the first frame update
    void Awake()
    {
        instance = this;
        _api = new Api();
        _api.Settings.ClientId = Secrets.CLIENT_ID;
        _api.Settings.AccessToken = Secrets.OAUTH_TOKEN;
        NomesEventos.Add("DANCE");
    }

    // Update is called once per frame
    void Update()
    {

    }

    public async void TryConnect()
    {
        for (int i = 0; i < 30; i++)
        {
            if (!string.IsNullOrEmpty(authorizationCode))
            {
                connection = await Twitch.Base.TwitchConnection.ConnectViaAuthorizationCode(Secrets.CLIENT_ID, Secrets.OAUTH_TOKEN, authorizationCode, redirectUrl: "http://localhost:8919");
                CurrentUser = await connection.V5API.Users.GetCurrentUser();
                if (CurrentUser != null && CurrentUser.id != null)
                {
                    CurrentChannel = await connection.V5API.Channels.GetCurrentChannel();
                    //CanvasIntro.enabled = false;
                    UnityEngine.Debug.Log("Escutando eventos");
                    EscutaEventos();
                }
                break;
            }
            UnityEngine.Debug.Log("Twitch Tentativa " + i.ToString());
            await Task.Delay(1000);
        }

        UnityEngine.Debug.Log(authorizationCode);
    }
    public async void Inialize()
    {
        UnityEngine.Debug.Log("Twitch iniciando");
        var httpListener = new HttpListener();
        try
        {
            string url = "https://id.twitch.tv/oauth2/authorize";
            Dictionary<string, string> parameters = new Dictionary<string, string>()
            {
                { "client_id", Secrets.CLIENT_ID },
                { "scope", ConvertClientScopesToString(new Twitch.Base.OAuthClientScopeEnum[]{ Twitch.Base.OAuthClientScopeEnum.user_read, Twitch.Base.OAuthClientScopeEnum.channel_read, Twitch.Base.OAuthClientScopeEnum.channel__read__redemptions , Twitch.Base.OAuthClientScopeEnum.channel_check_subscription, Twitch.Base.OAuthClientScopeEnum.channel__read__subscriptions}) },
                { "response_type", "code" },
                { "redirect_uri", "http://localhost:8919" },
            };
            //parameters.Add("force_verify", "force");
            FormUrlEncodedContent content = new FormUrlEncodedContent(parameters.AsEnumerable());

            string finalurl = url + "?" + await content.ReadAsStringAsync();



            httpListener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            httpListener.Prefixes.Add("http://localhost:8919/");
            httpListener.Start();


            ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = finalurl, UseShellExecute = true };
            Process.Start(startInfo);

            await Task.Factory.StartNew(() =>
            {
                try
                {
                    while (httpListener != null && httpListener.IsListening)
                    {
                        try
                        {
                            HttpListenerContext context = httpListener.GetContext();
                            Task.Factory.StartNew(async (ctx) =>
                            {
                                await ProcessConnection((HttpListenerContext)ctx);
                                ((HttpListenerContext)ctx).Response.Close();
                                httpListener.Stop();

                            }, context, TaskCreationOptions.LongRunning);
                        }
                        catch (HttpListenerException) { }
                        catch (Exception ex) { UnityEngine.Debug.Log(ex); }
                    }
                }
                catch (Exception ex) { UnityEngine.Debug.Log(ex); }

                httpListener.Stop();
            }, TaskCreationOptions.LongRunning);


        }
        catch (Exception ex) { UnityEngine.Debug.Log(ex); }


        httpListener.Stop();


        TryConnect();





    }



    internal static string ConvertClientScopesToString(IEnumerable<Twitch.Base.OAuthClientScopeEnum> scopes)
    {
        string result = "";

        foreach (string scopeName in StreamingClient.Base.Util.EnumHelper.GetEnumNames(scopes))
        {
            result += scopeName.Replace("__", ":") + " ";
        }

        if (result.Length > 0)
        {
            result = result.Substring(0, result.Length - 1);
        }

        return result;
    }

    public void EscutaEventos()
    {
        if (_pubSub == null)
        {
            _pubSub = new PubSub();
            _pubSub.OnRewardRedeemed += _pubSub_OnRewardRedeemed;
            _pubSub.OnPubSubServiceConnected += _pubSub_OnPubSubServiceConnected; ;
            _pubSub.Connect();
            StartTwitchGame();

        }
    }

    public bool PubSubConnected;

    private void _pubSub_OnPubSubServiceConnected(object sender, EventArgs e)
    {
        UnityEngine.Debug.Log("Twitch PubSubServiceConnected!");

        // On connect listen to Bits evadsent
        // Please note that listening to the whisper events requires the chat_login scope in the OAuth token.
        _pubSub.ListenToRewards(CurrentChannel.id);

        // SendTopics accepts an oauth optionally, which is necessary for some topics, such as bit events.
        _pubSub.SendTopics(Secrets.OAUTH_TOKEN);

        PubSubConnected = true;

    }


    private async void _pubSub_OnRewardRedeemed(object sender, TwitchLib.PubSub.Events.OnRewardRedeemedArgs e)
    {
        if (NomesEventos.Contains(e.RewardTitle.ToUpper().Trim()))
        {

            UnityEngine.Debug.Log("Twitch Reward:" + e.DisplayName);
            UnityEngine.Debug.Log("Twitch Reward:" + e.Login);
            var user = await connection.V5API.Users.GetUserByLogin(e.Login);
            var c = await connection.V5API.Channels.GetChannel(user);
            var sub = await connection.V5API.Channels.GetChannelUserSubscription(c, user);
            //UnityEngine.Debug.Log(c.logo);

            Solicitacoes.Add(new Solicitacao() { id = e.RewardId.ToString(), nome = e.DisplayName, avatar = c.logo, sub = sub == null ? "" : sub.sub_plan, tower = e.RewardTitle.ToUpper().Trim() });

        }

    }

    protected async Task ProcessConnection(HttpListenerContext listenerContext)
    {
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError;
        string result = string.Empty;

        string token = GetRequestParameter(listenerContext, "code");
        if (!string.IsNullOrEmpty(token))
        {
            statusCode = HttpStatusCode.OK;
            result = defaultSuccessResponse;

            authorizationCode = token;
        }

        await this.CloseConnection(listenerContext, statusCode, result);
    }

    protected async Task CloseConnection(HttpListenerContext listenerContext, HttpStatusCode statusCode, string content)
    {
        listenerContext.Response.Headers["Access-Control-Allow-Origin"] = "*";
        listenerContext.Response.StatusCode = (int)statusCode;
        listenerContext.Response.StatusDescription = statusCode.ToString();

        byte[] buffer = Encoding.UTF8.GetBytes(content);
        await listenerContext.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
    }

    protected string GetRequestParameter(HttpListenerContext listenerContext, string parameter)
    {
        if (listenerContext.Request.RawUrl.Contains(parameter))
        {
            string searchString = "?" + parameter + "=";
            int startIndex = listenerContext.Request.RawUrl.IndexOf(searchString);
            if (startIndex < 0)
            {
                searchString = "&" + parameter + "=";
                startIndex = listenerContext.Request.RawUrl.IndexOf(searchString);
                if (startIndex < 0)
                {
                    searchString = "#" + parameter + "=";
                    startIndex = listenerContext.Request.RawUrl.IndexOf(searchString);
                }
            }

            if (startIndex >= 0)
            {
                string token = listenerContext.Request.RawUrl.Substring(startIndex + searchString.Length);

                int endIndex = token.IndexOf("&");
                if (endIndex > 0)
                {
                    token = token.Substring(0, endIndex);
                }
                return token;
            }
        }
        return null;
    }





    public void StartTwitchGame()
    {
        StartCoroutine(CheckForTwitchTowers());
    }


    private IEnumerator CheckForTwitchTowers()
    {
        yield return new WaitForSeconds(2);
        while (true)
        {
            yield return new WaitForSeconds(1);

            if (TwitchConnection.Solicitacoes.Count > 0)
            {
                var item = TwitchConnection.Solicitacoes[0];
                AdicionaParticipante(item.nome, item.avatar);
                TwitchConnection.Solicitacoes.RemoveAt(0);


                //Debug.Log(tower.unitName);
            }


        }
    }

    private int GetTwitchTowersCount(string groupName)
    {
        int count = 1;
        foreach (var item in TwitchConnection.Solicitacoes)
        {
            if (item.tower.ToLower() == groupName.ToLower())
            {
                count += 1;
            }
        }
        return count;
    }

    public void AdicionaParticipante(string nome, string avatar)
    {
        foreach (var item in participantes)
        {
            if (item.nome == nome)
                return;
        }
        participantes.Add(new Participante() { nome = nome, avatar = avatar });
    }

    public static List<Participante> participantes { get; set; } = new List<Participante>();


}

public class Solicitacao
{
    public string id;
    public string nome;
    public string avatar;
    public string tower;
    internal string sub;
}

public class Participante
{
    public string nome;
    public string avatar;
    public int pontuacao;

}

