using System;
using System.Net;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using Idea_center.Models;


namespace Idea_center
{
    [TestFixture]
    public class IdeaCenterApiTests
    {
        private RestClient client;
        private static string lastCreatedIdeaId;

        private const string baseUrl = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:84";

        private const string staticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiIxMzdjMDhjYy04NmI5LTRiZjMtOGMyMi0wYTVjZWNhMTZmYzQiLCJpYXQiOiIwOC8xNS8yMDI1IDEyOjIxOjAxIiwiVXNlcklkIjoiOWE3MzMxNjEtN2RmNy00MmUyLWQyZGItMDhkZGQ0ZTA4YmQ4IiwiRW1haWwiOiJ6aGVrb25pQHNtaXRoLmNvbSIsIlVzZXJOYW1lIjoiemhla29uaVNtaXRoIiwiZXhwIjoxNzU1MjgyMDYxLCJpc3MiOiJJZGVhQ2VudGVyX0FwcF9Tb2Z0VW5pIiwiYXVkIjoiSWRlYUNlbnRlcl9XZWJBUElfU29mdFVuaSJ9.GeSh91t5H0h5glJ8wBHH-0PEG5R56r45nLI0fJTx190";

        private const string loginEmail = "zhekoni@smith.com";
        private const string loginPassword = "zhekoni123";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (!string.IsNullOrWhiteSpace(staticToken))
            {
                jwtToken = staticToken;
            }
            else
            {
                jwtToken = GetjwtToken(loginEmail, loginPassword);
            }

            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };
            this.client = new RestClient(options);

        }

        private string GetjwtToken(string email, string password)
        {
            var tempClient = new RestClient(baseUrl);
            var request = new RestRequest("/api/User/Authentication",Method.Post);
            request.AddJsonBody(new { email, password });

            var response = tempClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);

                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Failed to retrieve JWT token from the response.");
                }

                return token;
            }

            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code:{response.StatusCode}, Content: {response.Content}");
            }

        }

        [Order(1)]
        [Test]
        public void Create_A_New_Idea_With_The_Required_Fields()
        {
            var ideaRequest = new IdeaDTO
            {
                Title = "Test Idea by Zhekoni",
                Description = "This is a test idea description",
                Url = ""
            };

            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(ideaRequest);
            var response = this.client.Execute(request);

            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine($"Response Content: '{response.Content}'");

            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(createResponse.Msg, Is.EqualTo("Successfully created!"));
            
        }

        [Order(2)]
        [Test]
        public void GetAllIdeas_ShouldReturnListOfIdeas()
        {
            var request = new RestRequest("/api/Idea/All");
            var response = this.client.Execute(request);

            var responseItems = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response, Is.Not.Null);
            Assert.That(responseItems, Is.Not.Empty);

            lastCreatedIdeaId = responseItems.LastOrDefault()?.id;
        }


        [Order(3)]
        [Test]
        public void Edit_TheLastIdea_ShouldReturnListOfIdeas()
        {
            var editRequest = new IdeaDTO
            {
                Title = "Edited Title",
                Description = "edited description",
                Url = ""
            };

            var request = new RestRequest("/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            request.AddJsonBody(editRequest);
            var response = this.client.Execute(request);
            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(editResponse.Msg, Is.EqualTo("Edited successfully"));
        }

        [Order(4)]
        [Test]
        public void DeleteIdea_ShouldReturnSuccessMsg()
        {
            var request = new RestRequest($"/api/Idea/Delete",Method.Delete);
            request.AddQueryParameter("ideaId", lastCreatedIdeaId);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("The idea is deleted!"));
        }

        [Order(5)]
        [Test]
        public void CreateIdea_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var ideaRequest = new IdeaDTO
            {
                Title = "",
                Description = ""
            };

            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody<IdeaDTO>(ideaRequest);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        }

        [Order(6)]
        [Test]
        public void EditIdea_ThatDoesntExist_ShouldReturnBadRequest()
        {
            var editRequest = new IdeaDTO
            {
                Title = "Edited Title",
                Description = "Edited Descritpion"
            };

            var request = new RestRequest("/api/Idea/Edit", Method.Put);
            request.AddQueryParameter("ideaId", "123422");
            request.AddJsonBody(editRequest);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("There is no such idea!"));

        }

        [Order(7)]
        [Test]
        public void DeleteIdea_ThatDoesntExist_ShouldReturnBadRequest()
        {
            var request = new RestRequest($"/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaId", "123455");
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("There is no such idea!"));
        }

            [OneTimeTearDown]
        public void Teardown()
        {
            this.client?.Dispose();
        }
    }

}