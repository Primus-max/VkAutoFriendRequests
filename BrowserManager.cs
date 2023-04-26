using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;

public class BrowserManager
{
    public static async Task<RemoteWebDriver> ConnectDriver(string profileId)
    {
        string baseUrl = "http://";
        string launchUrl = $"http://127.0.0.1:35000/automation/launch/python/{profileId}/cloud";
        var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(launchUrl);
        string responseString = await response.Content.ReadAsStringAsync();

        JObject? responseDataJson = null;
        try
        {
            responseDataJson = JObject.Parse(responseString);
        }
        catch (JsonReaderException ex)
        {
            // Handle the exception appropriately, e.g. log it or rethrow it
            Console.WriteLine($"Failed to parse response JSON: {ex.Message}");
            return null;
        }

        string? status = (string?)responseDataJson?["status"];
        string? remoteAddressWithoutHttp = (string?)responseDataJson?["url"];

        string fullRemoteAdress = Path.Combine(baseUrl, remoteAddressWithoutHttp ?? "");

        if (status == "error") { return null; }

        var options = new ChromeOptions();
        options.AddArguments("start-maximized");
        options.AddArgument($"--instance={profileId}");

        ReadOnlyDesiredCapabilities? capabilities = options.ToCapabilities() as ReadOnlyDesiredCapabilities;
        if (capabilities == null)
        {
            throw new InvalidOperationException("Failed to create capabilities from options");
        }

        RemoteWebDriver? driver = null;
        int retries = 0;
        while (driver == null && retries < 10)
        {
            try
            {
                driver = new RemoteWebDriver(new Uri(fullRemoteAdress), capabilities);
            }
            catch (WebDriverException)
            {
                await Task.Delay(1000);
                retries++;
            }
        }

        if (driver == null)
        {
            throw new InvalidOperationException("Failed to create driver after 10 retries");
        }

        return driver;
    }
}

