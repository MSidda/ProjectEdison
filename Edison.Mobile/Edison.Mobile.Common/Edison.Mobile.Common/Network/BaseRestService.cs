﻿using System;
using Edison.Mobile.Common.Auth;
using Edison.Mobile.Common.Logging;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Deserializers;

namespace Edison.Mobile.Common.Network
{
    public class BaseRestService
    {
        protected readonly AuthService authService;
        protected readonly RestClient client;
        protected readonly ILogger logger;

        public BaseRestService(AuthService authService, ILogger logger, string baseUrl)
        {
            this.logger = logger;
            this.authService = authService;

            client = new RestClient(baseUrl);
            client.AddHandler("application/json", new NewtonsoftDeserializer());
        }

        protected virtual RestRequest PrepareRequest(string endpoint, Method method, object requestBody = null)
        {
            try
            {
                var request = new RestRequest(endpoint, method) { RequestFormat = DataFormat.Json };

                AddAuthHeader(request);

                if ((method == Method.POST || method == Method.PUT) && requestBody != null)
                {
                    request.AddParameter("application/json", JsonConvert.SerializeObject(requestBody), ParameterType.RequestBody);
                }

                return request;
            }
            catch (Exception ex)
            {
                logger.Log(ex, "Error preparing REST request");
                return new RestRequest();
            }
        }

        protected virtual void AddAuthHeader(RestRequest request)
        {
            var token = authService.AuthenticationResult.IdToken;
            request.AddHeader("Authorization", $"Bearer {token}");
        }
    }

    public class NewtonsoftDeserializer : IDeserializer
    {
        public string RootElement { get; set; }
        public string Namespace { get; set; }
        public string DateFormat { get; set; }
        public string ContentType { get; set; }

        public T Deserialize<T>(IRestResponse response)
        {
            return JsonConvert.DeserializeObject<T>(response.Content);
        }
    }
}
