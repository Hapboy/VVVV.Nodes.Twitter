﻿using System;
using System.Threading;
using System.Runtime.Remoting.Messaging;

using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using System.ComponentModel.Composition;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;
using VVVV.Core.Logging;
using TweetSharp;

namespace VVVV.TwitterApi.Nodes
{
    [PluginInfo(Name = "Twitter Async",
                Category = "Network",
                Version = "",
                Author = "ethermammoth",
                Tags = "twitter",
                Help = "Provides access to twitter through TweetSharp API",
                AutoEvaluate = true)]
    public class TwitterApiAsync : IPluginEvaluate
    {

        //INPUT
        [Input("Auth App", IsBang = true, IsSingle = true)]
        ISpread<bool> FAuthApp;

        [Input("Auth User", IsBang = true, IsSingle = true)]
        ISpread<bool> FAuthUser;

        [Input("Token Verifier", IsSingle = true)]
        ISpread<string> FTokenVerifier;

        [Input("Token Verifier Entered", IsSingle = true, IsBang = true)]
        ISpread<bool> FTokenVerifierEntered;

        [Input("Access Token", IsSingle = true)]
        ISpread<string> FAccessTokenInput;

        [Input("Access Token Secret", IsSingle = true)]
        ISpread<string> FAccessTokenSecretInput;

        [Input("Verify Token", IsSingle = true, IsBang = true)]
        ISpread<bool> FVerifyToken;

        [Input("Tweet Message", IsSingle = true)]
        ISpread<string> FTweetMessage;

        [Input("Send Tweet", IsSingle = true, IsBang = true)]
        ISpread<bool> FSendTweet;

        [Input("Tweet Image", IsSingle = true)]
        ISpread<string> FTweetImage;

        [Input("Send Image Tweet", IsSingle = true, IsBang = true)]
        ISpread<bool> FSendImageTweet;

        [Input("Logout", IsSingle = true, IsBang = true)]
        ISpread<bool> FLogout;

        [Input("Consumer Key", Visibility = PinVisibility.Hidden, IsSingle = true)]
        ISpread<string> FConsumerKey;

        [Input("Consumer Secret", Visibility = PinVisibility.Hidden, IsSingle = true)]
        ISpread<string> FConsumerSecret;

        //OUTPUT
        [Output("Request URL", IsSingle = true)]
        ISpread<string> FRequestUrl;

        [Output("Twitter Id", IsSingle = true)]
        ISpread<int> FUserId;

        [Output("Twitter Name", IsSingle = true)]
        ISpread<string> FUserName;

        [Output("Usage Limit", IsSingle = true)]
        ISpread<string> FUseLimit;

        [Output("Require User Auth", IsSingle = true)]
        ISpread<bool> FNeedUserAuth;

        [Output("Is Authed", IsSingle = true)]
        ISpread<bool> FIsAuthed;

        [Output("Is Logged In", IsSingle = true)]
        ISpread<bool> FIsLoggedIn;

        [Output("Status", IsSingle = true)]
        ISpread<string> FStatus;

        [Import()]
        ILogger FLogger;

        Tvvvvitter twit = new Tvvvvitter();

        public void Evaluate(int SpreadMax)
        {
            if (FAuthApp[0])
            {
                AuthAppAsync auth = new AuthAppAsync(twit.AuthApp);
                IAsyncResult result = auth.BeginInvoke(FConsumerKey[0], 
                    FConsumerSecret[0], new AsyncCallback(AppAuthCallBack), null);
            }

            if (FAuthUser[0])
            {
                GetRequestTokenAsync request = new GetRequestTokenAsync(twit.GetRequestToken);
                IAsyncResult result = request.BeginInvoke(new AsyncCallback(GetRequestTokenCallBack), null);
            }

            if (FTokenVerifierEntered[0])
            {
                GetAccessTokenAsync access = new GetAccessTokenAsync(twit.GetAccessToken);
                IAsyncResult result = access.BeginInvoke(FTokenVerifier[0], 
                    new AsyncCallback(GetAccessTokenCallBack), null);
            }

            if (FVerifyToken[0])
            {
                VerifyCredentialsAsync cred = new VerifyCredentialsAsync(twit.VerifyCredentials);
                IAsyncResult result = cred.BeginInvoke( FAccessTokenInput[0], 
                    FAccessTokenSecretInput[0], new AsyncCallback(VerifyCredentialsCallBack), null);
            }

            if (FSendTweet[0])
            {
                if (FTweetMessage[0].Length > 0)
                {
                    SendTweetAsync tweet = new SendTweetAsync(twit.SendTweet);
                    IAsyncResult result = tweet.BeginInvoke(FTweetMessage[0],
                        new AsyncCallback(SendTweetCallBack), null);
                }
                else
                {
                    FLogger.Log(LogType.Error, "Twitter error: No Text Message Entered!");
                }
            }

            if (FSendImageTweet[0])
            {
                if (FTweetMessage[0].Length > 0 && File.Exists(FTweetImage[0]))
                {
                    SendImageTweetAsync tweet = new SendImageTweetAsync(twit.SendImageTweet);
                    IAsyncResult result = tweet.BeginInvoke(FTweetMessage[0], FTweetImage[0],
                        new AsyncCallback(SendImageTweetCallBack), null);
                }
                else
                {
                    FLogger.Log(LogType.Error, "Twitter error: No Text Message Entered or File path not valid!");
                }
            }

            if (FLogout[0])
            {
                twit.Logout();
            }

            //update all
            if (twit != null)
            {
                FIsAuthed[0] = twit.appAuthed;
                FIsLoggedIn[0] = twit.hasValidToken;
                FUserId[0] = twit.userId;
                FUserName[0] = twit.userName;
                FRequestUrl[0] = twit.requestUrl;
                FNeedUserAuth[0] = twit.requireUserAuth;
                FStatus[0] = twit.statusCode;
                FUseLimit[0] = twit.rateStatus;
                if (twit.tweetSended)
                {
                    FLogger.Log(LogType.Message, "Twitter: Tweet Sended!");
                    twit.tweetSended = false;
                }
            }

        }

        private void AppAuthCallBack(IAsyncResult ar)
        {
            AsyncResult result = (AsyncResult)ar;
            AuthAppAsync caller = (AuthAppAsync)result.AsyncDelegate;
            bool returnValue = caller.EndInvoke(ar);
        }

        private void VerifyCredentialsCallBack(IAsyncResult ar)
        {
            AsyncResult result = (AsyncResult)ar;
            VerifyCredentialsAsync caller = (VerifyCredentialsAsync)result.AsyncDelegate;
            bool returnValue = caller.EndInvoke(ar);
        }

        private void GetRequestTokenCallBack(IAsyncResult ar)
        {
            AsyncResult result = (AsyncResult)ar;
            GetRequestTokenAsync caller = (GetRequestTokenAsync)result.AsyncDelegate;
            bool returnValue = caller.EndInvoke(ar);
        }

        private void GetAccessTokenCallBack(IAsyncResult ar)
        {
            AsyncResult result = (AsyncResult)ar;
            GetAccessTokenAsync caller = (GetAccessTokenAsync)result.AsyncDelegate;
            bool returnValue = caller.EndInvoke(ar);
        }

        private void SendTweetCallBack(IAsyncResult ar)
        {
            AsyncResult result = (AsyncResult)ar;
            SendTweetAsync caller = (SendTweetAsync)result.AsyncDelegate;
            bool returnValue = caller.EndInvoke(ar);
        }

        private void SendImageTweetCallBack(IAsyncResult ar)
        {
            AsyncResult result = (AsyncResult)ar;
            SendImageTweetAsync caller = (SendImageTweetAsync)result.AsyncDelegate;
            bool returnValue = caller.EndInvoke(ar);
        }
    }


    public class Tvvvvitter
    {
        //Twitter Objects
        private TwitterService service;
        private OAuthRequestToken requestToken;
        private OAuthAccessToken accessToken;

        public bool requireUserAuth { get; private set; }
        public bool appAuthed { get; private set; }
        public bool hasValidToken { get; private set; }
        public string userName { get; private set; }
        public int userId { get; private set; }
        public string requestUrl { get; private set; }
        public string statusCode { get; private set; }
        public string rateStatus { get; private set; }

        public bool tweetSended;

        public Tvvvvitter()
        {
            appAuthed = false;
            hasValidToken = false;
            userId = 0;
            userName = "";
            requestUrl = "";
            statusCode = "";
            rateStatus = "";
            tweetSended = false;
        }

        public bool AuthApp(string consumer, string secret)
        {
            if (consumer.Length > 2 && secret.Length > 2)
            {
                service = new TwitterService(consumer, secret);
                appAuthed = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool VerifyCredentials(string accessToken, string accessSecret)
        {
            if (!appAuthed)
                return false;

            service.AuthenticateWith(accessToken, accessSecret);
            
            VerifyCredentialsOptions opt = new VerifyCredentialsOptions();
            TwitterUser usr = service.VerifyCredentials(opt);

            if (usr != null)
            {
                userName = usr.Name;
                userId = (int)usr.Id;
                hasValidToken = true;
                return true;
            }

            return false;
        }

        public bool GetRequestToken()
        {
            if (!appAuthed)
                return false;

            requestToken = service.GetRequestToken();
            Uri uri = service.GetAuthorizationUri(requestToken);
            if (requestToken != null && uri != null)
            {
                requestUrl = uri.ToString();
                requireUserAuth = true;
                return true;
            }
            return false;
        }

        public bool GetAccessToken(string verifier)
        {
            if (!requireUserAuth || !appAuthed)
                return false;

            accessToken = service.GetAccessToken(requestToken, verifier);
            if (accessToken != null)
            {
                service.AuthenticateWith(accessToken.Token, accessToken.TokenSecret);
                VerifyCredentialsOptions opt = new VerifyCredentialsOptions();
                TwitterUser usr = service.VerifyCredentials(opt);

                if (usr != null)
                {
                    userName = usr.Name;
                    userId = (int)usr.Id;
                    hasValidToken = true;
                    requireUserAuth = false;
                    requestUrl = "";
                    return true;
                }
            }
            return false;
        }

        public bool SendTweet(string message)
        {
            if (!hasValidToken)
                return false;

            SendTweetOptions opt = new SendTweetOptions();
            opt.Status = message;
            try
            {
                TwitterStatus status = service.SendTweet(opt);
                if (status != null)
                {
                    if (service.Response.StatusCode == HttpStatusCode.OK)
                    {
                        statusCode = service.Response.StatusCode.ToString();
                        tweetSended = true;
                        return true;
                    }

                    if (service.Response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        NeedAuthentication();
                        return false;
                    }
                }
            }
            catch (Exception error)
            {
                statusCode = error.Message;
            }

            return false;
        }

        public bool SendImageTweet(string message, string path)
        {
            if (!hasValidToken)
                return false;

            using (var stream = new FileStream(path, FileMode.Open))
            {
                SendTweetWithMediaOptions tweetOpts = new SendTweetWithMediaOptions();
                tweetOpts.Status = message;
                tweetOpts.Images = new Dictionary<string, Stream> { { "image", stream } };
                try
                {
                    TwitterStatus status = service.SendTweetWithMedia(tweetOpts);
                    if (status != null)
                    {
                        if (service.Response.StatusCode == HttpStatusCode.OK)
                        {
                            statusCode = service.Response.StatusCode.ToString();
                            tweetSended = true;
                            return true;
                        }

                        if(service.Response.StatusCode == HttpStatusCode.Unauthorized)
                        {
                            NeedAuthentication();
                            return false;
                        }
                    }
                }
                catch (Exception error)
                {
                    statusCode = error.Message;
                }
            }
            return false;
        }

        public void Logout()
        {
            NeedAuthentication();
        }
        
        private void NeedAuthentication()
        {
            appAuthed = false;
            hasValidToken = false;
            userId = 0;
            userName = "";
            requestUrl = "";
            statusCode = "";
            rateStatus = "";
            tweetSended = false;
        }
    }

    public delegate bool AuthAppAsync(string consumer, string secret);
    public delegate bool VerifyCredentialsAsync(string accessToken, string accessSecret);
    public delegate bool GetRequestTokenAsync();
    public delegate bool GetAccessTokenAsync(string verifier);
    public delegate bool SendTweetAsync(string message);
    public delegate bool SendImageTweetAsync(string message, string path);
}