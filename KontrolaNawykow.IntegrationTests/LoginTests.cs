namespace KontrolaNawykow.IntegrationTests;

using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
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

        var responseString = await response.Content.ReadAsStringAsync();

        //using (StreamWriter outputFile = new StreamWriter(Path.Combine("C:\\source\\Projekt TAB\\KontrolaNawykow.IntegrationTests", "site.html")))
        //{

        //    outputFile.WriteLine(response.Headers.ToString());
        //    outputFile.WriteLine(responseString);
        //}

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("<title>Logowanie - KontrolaNawyków</title>", responseString);
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
            new KeyValuePair<string, string>("__RequestVerificationToken", value),
            new KeyValuePair<string, string>("Username", "testuser"),
            new KeyValuePair<string, string>("Password", "WrongPass")
        });

        var response = await _client.PostAsync("/Account/Login", formData);
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<title>Logowanie - KontrolaNawyków</title>", content);
        Assert.Contains("Nieprawid&#x142;owa nazwa u&#x17C;ytkownika lub has&#x142;o", content);
    }
}