namespace DR_opgave_SeleniumTests;

using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

public class UnitTest1 : IDisposable
{
    private readonly IWebDriver _driver;
    private readonly string _baseUrl;
    private readonly string _apiUrl;
    private readonly HttpClient _http;
    private int? _seededRecordId;

    public UnitTest1()
    {
        var options = new ChromeOptions();
        options.AddArgument("--headless=new");
        options.AddArgument("--window-size=1400,1000");
        options.AddArgument("--no-sandbox");

        _driver = new ChromeDriver(options);
        _baseUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:5173";
        _apiUrl = Environment.GetEnvironmentVariable("API_URL") ?? "http://localhost:5249/api/v2";
        _http = new HttpClient();

        SeedRecord();
    }

    private void SeedRecord()
    {
        var payload = new
        {
            name = "__selenium_test_record__",
            artist = "Test Artist",
            genre = "Test",
            releaseYear = 2000,
            trackCount = 1,
            duration = 180
        };

        var response = _http.PostAsJsonAsync($"{_apiUrl}/records", payload).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        using var doc = JsonDocument.Parse(json);
        _seededRecordId = doc.RootElement.GetProperty("id").GetInt32();
    }

    [Fact]
    public void Frontpage_Loads_And_Shows_Title()
    {
        _driver.Navigate().GoToUrl(_baseUrl);

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElement(By.TagName("body")));

        Assert.Contains("record", _driver.PageSource, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Frontpage_Has_Create_Button()
    {
        _driver.Navigate().GoToUrl(_baseUrl);

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElement(By.TagName("body")));

        var createButton = _driver.FindElement(By.Id("create-record-button"));
        Assert.NotNull(createButton);
    }

    [Fact]
    public void Frontpage_Has_Delete_Button()
    {
        _driver.Navigate().GoToUrl(_baseUrl);

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElement(By.ClassName("delete-record-button")));

        var deleteButtons = _driver.FindElements(By.ClassName("delete-record-button"));
        Assert.NotEmpty(deleteButtons);
    }

    [Fact]
    public void Frontpage_Has_Edit_Button()
    {
        _driver.Navigate().GoToUrl(_baseUrl);

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElement(By.ClassName("edit-record-button")));

        var editButtons = _driver.FindElements(By.ClassName("edit-record-button"));
        Assert.NotEmpty(editButtons);
    }

    [Fact]
    public void Frontpage_Has_Sort_Controls()
    {
        _driver.Navigate().GoToUrl(_baseUrl);

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElement(By.Id("sort-select")));

        var sortSelect = _driver.FindElement(By.Id("sort-select"));
        Assert.NotNull(sortSelect);
    }

    public void Dispose()
    {
        if (_seededRecordId.HasValue)
            _http.DeleteAsync($"{_apiUrl}/records/{_seededRecordId.Value}").GetAwaiter().GetResult();

        _http.Dispose();
        _driver.Quit();
        _driver.Dispose();
    }
}
