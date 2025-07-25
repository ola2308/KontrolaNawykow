namespace KontrolaNawykow.IntegrationTests;

using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
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
        //Wydobycie tokenu
        var formPage = await _client.GetAsync("/Account/Login");

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
        value = value.Substring(7, value.Length - 11);
        //Wydobycie tokenu - koniec

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("__RequestVerificationToken", value),
            new KeyValuePair<string, string>("Username", "testuser"),
            new KeyValuePair<string, string>("Password", "Test123!")
        });

        var response = await _client.PostAsync("/Account/Login", formData);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/Diet/Index", response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldShowError()
    {

        //Wydobycie tokenu
        var formPage = await _client.GetAsync("/Account/Login");

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
        value = value.Substring(7, value.Length - 11);
        //Wydobycie tokenu - koniec

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