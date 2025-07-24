namespace KontrolaNawykow.IntegrationTests;

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

public class LoginTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public LoginTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }


    [Fact]
    public async Task Login_WithoutToken_ShouldBeBadRequest()
    {
        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Username", "testuser"),
            new KeyValuePair<string, string>("Password", "Test123!")
        });

        var response = await _client.PostAsync("/Account/Login", formData);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldRedirect()
    {
        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", "???"),
            new KeyValuePair<string, string>("Username", "testuser"),
            new KeyValuePair<string, string>("Password", "Test123!")
        });

        var response = await _client.PostAsync("/Account/Login", formData);

        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Diet/Index", response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldShowError()
    {
        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", "???"),
            new KeyValuePair<string, string>("Username", "testuser"),
            new KeyValuePair<string, string>("Password", "WrongPass")
        });

        var response = await _client.PostAsync("/Account/Login", formData);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Nieprawidłowa nazwa użytkownika lub hasło", content);
    }
}