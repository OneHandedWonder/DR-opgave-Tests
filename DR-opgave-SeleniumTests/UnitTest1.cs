namespace DR_opgave_SeleniumTests;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

public class UnitTest1 : IDisposable
{
    private readonly IWebDriver _driver;
    private readonly string _baseUrl;
<<<<<<< Updated upstream
=======
    private readonly string _apiUrl;
    private readonly string _authUrl;
    private readonly HttpClient _http;
    private int? _seededRecordId;
    private string? _authToken;

    private const string TestUsername = "admin";
    private const string TestPassword = "admin123";
>>>>>>> Stashed changes

    public UnitTest1()
    {
        var options = new ChromeOptions();
        options.AddArgument("--headless=new");
        options.AddArgument("--window-size=1400,1000");
        options.AddArgument("--no-sandbox");

        _driver = new ChromeDriver(options);
        _baseUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:5173";
<<<<<<< Updated upstream
=======
        _apiUrl = Environment.GetEnvironmentVariable("API_URL") ?? "http://localhost:5249/api/v2";
        _authUrl = _apiUrl.Replace("/api/v2", "/api/auth");
        _http = new HttpClient();

        SeedRecord();
        SignIn();
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
>>>>>>> Stashed changes
    }

    private void SignIn()
    {
        var payload = new
        {
            username = TestUsername,
            password = TestPassword
        };

        var response = _http.PostAsJsonAsync($"{_authUrl}/signin", payload).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        using var doc = JsonDocument.Parse(json);
        _authToken = doc.RootElement.GetProperty("token").GetString();
    }

    private void GoToSignedInPage()
    {
        _driver.Navigate().GoToUrl(_baseUrl);

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElement(By.TagName("body")));

        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "window.localStorage.setItem('dr-token', arguments[0]); window.localStorage.setItem('dr-user', arguments[1]);",
            _authToken,
            TestUsername);

        _driver.Navigate().Refresh();

        wait.Until(d => d.FindElement(By.TagName("table")));
    }

    [Fact]
    public void Frontpage_Loads_And_Shows_Title()
    {
        GoToSignedInPage();

        Assert.Contains("Musikarkiv", _driver.PageSource, StringComparison.OrdinalIgnoreCase);
    }

<<<<<<< Updated upstream
=======
    [Fact]
    public void Frontpage_Has_Create_Button()
    {
        GoToSignedInPage();

        var createButton = _driver.FindElement(By.Id("create-record-button"));
        Assert.NotNull(createButton);
    }

    [Fact]
    public void Frontpage_Has_Delete_Button()
    {
        GoToSignedInPage();

        var deleteButtons = _driver.FindElements(By.ClassName("delete-record-button"));
        Assert.NotEmpty(deleteButtons);
    }

    [Fact]
    public void Frontpage_Has_Edit_Button()
    {
        GoToSignedInPage();

        var editButtons = _driver.FindElements(By.ClassName("edit-record-button"));
        Assert.NotEmpty(editButtons);
    }

    [Fact]
    public void Frontpage_Has_Sort_Controls()
    {
        GoToSignedInPage();

        var sortSelect = _driver.FindElement(By.Id("sort-column"));
        Assert.NotNull(sortSelect);
    }

    [Fact]
    public void Frontpage_Has_SortOrder_Buttons()
    {
        GoToSignedInPage();

        var sortButtons = _driver.FindElements(By.CssSelector(".segmented button"));
        Assert.Equal(2, sortButtons.Count);
    }

    [Fact]
    public void Frontpage_Has_Refresh_Button()
    {
        GoToSignedInPage();

        var refreshButton = _driver.FindElement(By.XPath("//button[normalize-space()='Opdater']"));
        Assert.NotNull(refreshButton);
    }

    [Fact]
    public void Frontpage_Has_Records_Table()
    {
        GoToSignedInPage();

        var table = _driver.FindElement(By.TagName("table"));
        Assert.NotNull(table);
    }

    [Fact]
    public void Frontpage_Has_Filter_TextInput()
    {
        GoToSignedInPage();

        var input = _driver.FindElement(By.CssSelector("input[placeholder='Søg i tekst']"));
        Assert.NotNull(input);
    }

    [Fact]
    public void Frontpage_Has_Filter_GenreInput()
    {
        GoToSignedInPage();

        var input = _driver.FindElement(By.CssSelector("input[placeholder='Rock, Jazz, Pop']"));
        Assert.NotNull(input);
    }

    [Fact]
    public void Frontpage_Has_Filter_MinYearInput()
    {
        GoToSignedInPage();

        var input = _driver.FindElement(By.CssSelector("input[placeholder='1960']"));
        Assert.NotNull(input);
    }

    [Fact]
    public void Frontpage_Has_Filter_MaxYearInput()
    {
        GoToSignedInPage();

        var input = _driver.FindElement(By.CssSelector("input[placeholder='2026']"));
        Assert.NotNull(input);
    }

    [Fact]
    public void Frontpage_Has_ClearFilters_Button()
    {
        GoToSignedInPage();

        var clearButton = _driver.FindElement(By.ClassName("btn-clear"));
        Assert.NotNull(clearButton);
    }

>>>>>>> Stashed changes
    public void Dispose()
    {
        _driver.Quit();
        _driver.Dispose();
    }
}
