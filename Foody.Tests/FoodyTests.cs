using Foody.Tests.DTOs;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;

namespace Foody
{
    public class FoodyTests
    {
        private RestClient client;
        private static string foodId; // id на храната, която създаваме в тест 1

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken = GetJwtToken("tedora99", "123456");

            RestClientOptions options = new RestClientOptions("http://144.91.123.158:81/api/")
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            RestClient client = new RestClient("http://144.91.123.158:81");
            RestRequest request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { userName = username, password });

            RestResponse response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();
                return token;
            }

            throw new InvalidOperationException("Authentication failed: " + response.Content);
        }

        // -----------------------------
        // 1. CREATE FOOD
        // -----------------------------
        [Order(1)]
        [Test]
        public void CreateFood_ShouldReturn201()
        {
            var request = new RestRequest("Food/Create", Method.Post);

            var body = new FoodDTO
            {
                Name = "Test Food",
                Description = "Test Description",
                Url = ""
            };

            request.AddJsonBody(body);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            foodId = json.GetProperty("foodId").GetString();

            Assert.That(foodId, Is.Not.Null);
        }

        // -----------------------------
        // 2. EDIT FOOD
        // -----------------------------
        [Order(2)]
        [Test]
        public void EditFood_ShouldReturn200()
        {
            var request = new RestRequest($"Food/Edit/{foodId}", Method.Patch);

            var patchBody = new[]
            {
                new {
                    path = "/name",
                    op = "replace",
                    value = "Updated Title"
                }
            };

            request.AddJsonBody(patchBody);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            Assert.That(json.GetProperty("msg").GetString(), Is.EqualTo("Successfully edited"));
        }

        // -----------------------------
        // 3. GET ALL FOODS
        // -----------------------------
        [Order(3)]
        [Test]
        public void GetAllFoods_ShouldReturn200_AndNotEmpty()
        {
            var request = new RestRequest("Food/All", Method.Get);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            Assert.That(json.GetArrayLength(), Is.GreaterThan(0));
        }

        // -----------------------------
        // 4. DELETE FOOD
        // -----------------------------
        [Order(4)]
        [Test]
        public void DeleteFood_ShouldReturn200()
        {
            var request = new RestRequest($"Food/Delete/{foodId}", Method.Delete);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            Assert.That(json.GetProperty("msg").GetString(), Is.EqualTo("Deleted successfully!"));
        }

        // -----------------------------
        // 5. CREATE FOOD WITHOUT REQUIRED FIELDS
        // -----------------------------
        [Order(5)]
        [Test]
        public void CreateFood_MissingFields_ShouldReturn400()
        {
            var request = new RestRequest("Food/Create", Method.Post);

            var body = new FoodDTO
            {
                Name = "",
                Description = ""
            };

            request.AddJsonBody(body);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        // -----------------------------
        // 6. EDIT NON-EXISTING FOOD
        // -----------------------------
        [Order(6)]
        [Test]
        public void EditNonExistingFood_ShouldReturn404()
        {
            var request = new RestRequest("Food/Edit/999999", Method.Patch);

            var patchBody = new[]
            {
                new {
                    path = "/name",
                    op = "replace",
                    value = "DoesNotMatter"
                }
            };

            request.AddJsonBody(patchBody);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            Assert.That(json.GetProperty("msg").GetString(), Is.EqualTo("No food revues..."));
        }

        // -----------------------------
        // 7. DELETE NON-EXISTING FOOD
        // -----------------------------
        [Order(7)]
        [Test]
        public void DeleteNonExistingFood_ShouldReturn404()
        {
            var request = new RestRequest("Food/Delete/999999", Method.Delete);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            Assert.That(json.GetProperty("msg").GetString(), Is.EqualTo("No food revues..."));
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            this.client?.Dispose();
        }
    }
}
