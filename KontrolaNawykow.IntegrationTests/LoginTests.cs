namespace KontrolaNawykow.IntegrationTests;

using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices.JavaScript;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NuGet.Common;
using Xunit;
using Xunit.Sdk;

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
        string token = await GetToken("/Account/Login");

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
            new KeyValuePair<string, string>("Username", "testuser"),
            new KeyValuePair<string, string>("Password", "Test123!")
        });

        var response = await _client.PostAsync("/Account/Login", formData);

        var responseString = await response.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("<title>Logowanie - KontrolaNawyków</title>", responseString);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldShowError()
    {

        string token = await GetToken("/Account/Login");

        var formData = new FormUrlEncodedContent(new[] 
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
            new KeyValuePair<string, string>("Username", "testuser"),
            new KeyValuePair<string, string>("Password", "WrongPass")
        });

        var response = await _client.PostAsync("/Account/Login", formData);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<title>Logowanie - KontrolaNawyków</title>", content);
        Assert.Contains("Nieprawid&#x142;owa nazwa u&#x17C;ytkownika lub has&#x142;o", content);
    }

    [Fact]
    public async Task Register_WithoutToken_ShouldBeBadRequest()
    {
        string token = await GetToken("/Account/Register");

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Username", "testuser"),
            new KeyValuePair<string, string>("Password", "Test123!"),
            new KeyValuePair<string, string>("ConfirmPassword", "Test123!"),
            new KeyValuePair<string, string>("Email", "Test@sample.com"),
        });

        var response = await _client.PostAsync("/Account/Register", formData);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);

    }

    [Fact]
    public async Task Register_Existing_ShouldReturnError()
    {
        string token = await GetToken("/Account/Register");

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Username", "testuser"),
            new KeyValuePair<string, string>("Password", "Test123!"),
            new KeyValuePair<string, string>("ConfirmPassword", "Test123!"),
            new KeyValuePair<string, string>("Email", "Test@sample.com"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
        });

        var response = await _client.PostAsync("/Account/Register", formData);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<title>Rejestracja - KontrolaNawyków</title>", content);
        Assert.Contains("Uzytkownik o takiej nazwie ju&#x17C; istnieje.", content);
    }

    [Fact]
    public async Task Register_BadConfirm_ShouldReturnError()
    {
        string token = await GetToken("/Account/Register");

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Username", "baduser"),
            new KeyValuePair<string, string>("Password", "Test123!"),
            new KeyValuePair<string, string>("ConfirmPassword", "Innehaslo"),
            new KeyValuePair<string, string>("Email", "Test@sample.com"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
        });

        var response = await _client.PostAsync("/Account/Register", formData);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<title>Rejestracja - KontrolaNawyków</title>", content);
        Assert.Contains("Hasla nie s&#x105; identyczne", content);
    }

    [Fact]
    public async Task Register_NewAccount_ShouldRedirect()
    {
        string token = await GetToken("/Account/Register");

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Username", "newuser"),
            new KeyValuePair<string, string>("Password", "Passw0rd!"),
            new KeyValuePair<string, string>("ConfirmPassword", "Passw0rd!"),
            new KeyValuePair<string, string>("Email", "Test@sample.com"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token),
        });

        var response = await _client.PostAsync("/Account/Register", formData);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<title>Twój profil - KontrolaNawyków</title>", content);
        Assert.Contains("Cześć, newuser!", content);
    }


    private async Task<string> GetToken(string path)
    {
        var formPage = await _client.GetAsync(path);

        Assert.Equal(System.Net.HttpStatusCode.OK, formPage.StatusCode);

        var form = await formPage.Content.ReadAsStringAsync();

        Regex tokenSearch = new Regex("<input name=\"__RequestVerificationToken\".*\\/>");

        var tokenField = tokenSearch.Matches(form).FirstOrDefault();
        Assert.NotNull(tokenField);

        var token = tokenField.Value;

        Regex valueSearch = new Regex("value=\".*\" />");

        var valueField = valueSearch.Matches(token).FirstOrDefault();
        Assert.NotNull(valueField);

        var value = valueField.Value;
        return value.Substring(7, value.Length - 11);
    }
}