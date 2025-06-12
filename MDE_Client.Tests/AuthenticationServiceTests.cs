using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MDE_Client.Application.Services;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace MDE_Client.Tests
{
 

    public class AuthenticationServiceTests
    {
        private readonly Mock<HttpMessageHandler> _httpHandlerMock;
        private readonly HttpClient _httpClient;
        private readonly Mock<IConfiguration> _configMock;
        private readonly AuthenticationService _authService;

        public AuthenticationServiceTests()
        {
            _httpHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpHandlerMock.Object);
            _configMock = new Mock<IConfiguration>();

            _configMock.Setup(c => c["Api:BaseUrl"]).Returns("https://mockapi.test/");
            _configMock.Setup(c => c["Jwt:Issuer"]).Returns("testIssuer");
            _configMock.Setup(c => c["Jwt:Audience"]).Returns("testAudience");

            _authService = new AuthenticationService(_httpClient, _configMock.Object);
        }

       
    }

}
