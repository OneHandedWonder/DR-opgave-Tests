namespace DR_opgave_SeleniumTests;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

public class UnitTest1 : IDisposable
{
    private readonly IWebDriver _driver;
    private readonly string _baseUrl;

    public UnitTest1()
    {
        var options = new ChromeOptions();
        options.AddArgument("--headless=new");
        options.AddArgument("--window-size=1400,1000");
        options.AddArgument("--no-sandbox");

        _driver = new ChromeDriver(options);
        _baseUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:5173";
    }

    [Fact]
    public void Frontpage_Loads_And_Shows_Title()
    {
        _driver.Navigate().GoToUrl(_baseUrl);

        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(d => d.FindElement(By.TagName("body")));

        Assert.Contains("record", _driver.PageSource, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        _driver.Quit();
        _driver.Dispose();
    }
}
