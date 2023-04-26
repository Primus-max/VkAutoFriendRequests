using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System.Text.RegularExpressions;

public class FriendRequests
{
    private readonly IWebDriver _driver;
    private readonly string url = "https://vk.com/friends?section=all_requests";
    private static readonly string urlBeforeGoON = "https://vk.com/friends?act=find";
    private readonly string _profileId = "";
    private readonly string? _logFileName = "";
    private readonly string? _profileName = "";
    public int? _countFriends = 0;
    private const int MAX_COUNT_ATTEMPTS = 5;
    //private int? countRequestsFriend = 0;

    public FriendRequests(RemoteWebDriver driver, string profileId, string? logFileName, string profileName)
    {
        _driver = driver;
        _profileId = profileId;
        _logFileName = logFileName;
        _profileName = profileName;
    }

    public async Task Navigate()
    {
        WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        await Task.Run(() =>
        {
            wait.Until(driver => ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState").Equals("complete"));
        });


        await Processes.CheckRunningChromeAsync();
        // Проверяем Url на предмет блокировки
        if (_driver.Url.Contains("blocked"))
        {
            string? message = $"Этот аккаунт заблокирован: {_profileName}";
            LogManager.LogMessage(message, _logFileName);

            Console.WriteLine($"Этот аккаунт заблокирован: {_profileId}");
            _driver.Dispose();
            return;
        }

        // Проверяем страницу на предмет popup с предложением о красивом имени
        try
        {
            IWebElement popUp = _driver.FindElement(By.XPath("//div[@class='box_layout' and @onclick='boxQueue.skip=true;']"));
            if (popUp != null)
            {
                IWebElement closeButton = _driver.FindElement(By.XPath("//div[@class='box_x_button']"));
                closeButton.Click();
            }
        }
        catch (Exception) { }

        // Пробуем получить элемент в котором указано колличество друзей
        IWebElement countElement = null;
        try
        {
            countElement = await Task.Run(() => _driver.FindElement(By.CssSelector(".ui_rmenu_count")));
        }
        catch (Exception)
        {
            _driver.Dispose();
            return;
        }

        string text = countElement.Text;
        if (string.IsNullOrEmpty(text))
        {
            _driver.Dispose();
            return;
        }

        // Если мы не на нужной странице, то переходим
        if (_driver.Url != url)
        {
            await Task.Run(() => _driver.Navigate().GoToUrl(url));
        }

        // Проверяю и закрываю пустые копии chromium
        //Processes.CheckRunningChrome();
    }

    public async Task<Task> GetAllElementsOnPage()
    {
        var elements = new List<IWebElement>();
        var countRequestsFriend = 0;
        var scrollTo = 0;
        var js = (IJavaScriptExecutor)_driver;
        var attempts = 0;
        var previousElementsCount = 0;

        while (elements.Count < _countFriends)
        {
            var elementsToBeAdded = _driver.FindElements(By.CssSelector("button[id^='accept_request_']"));

            foreach (var element in elementsToBeAdded)
            {
                if (!elements.Contains(element))
                {
                    if (!CheckPageForErrors())
                    {
                        elements.Add(element);
                        await AcceptRequestAsync(element);
                        await Task.Delay(500);
                        countRequestsFriend++;
                    }
                    else
                    {
                        _driver.Dispose();
                        await Task.Delay(1000);

                        return Task.CompletedTask;
                    }
                }
            }

            if (elementsToBeAdded.Count == 0)
            {
                attempts++;

                if (attempts >= 5)
                {
                    _driver.Navigate().Refresh();
                    attempts = 0;
                    await Task.Delay(2000);
                    _countFriends = ExtractNumberFromTitle();
                    elements.Clear();
                    scrollTo = 0;
                    previousElementsCount = 0;
                }
            }
            else
            {
                var elementForHeight = _driver.FindElement(By.CssSelector("div.friends_user_row.friends_user_common.clear_fix"));
                int heightElement = elementForHeight.Size.Height;
                scrollTo += heightElement * elementsToBeAdded.Count;

                if (elementsToBeAdded.Count > previousElementsCount)
                {
                    previousElementsCount = elementsToBeAdded.Count;
                    attempts = 0;
                }
                else
                {
                    attempts++;

                    if (attempts >= 5)
                    {
                        _driver.Navigate().Refresh();
                        attempts = 0;
                        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
                        wait.Until(driver => ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState").Equals("complete"));
                        _countFriends = ExtractNumberFromTitle();
                        elements.Clear();
                        scrollTo = 0;
                        previousElementsCount = 0;
                    }
                }

                for (int i = scrollTo - heightElement * elementsToBeAdded.Count; i < scrollTo; i += 200)
                {
                    js.ExecuteScript($"window.scrollTo(0, {i})");
                    await Task.Delay(500);
                }
            }

            if (elements.Count >= _countFriends)
            {
                string message = $"Подтверждено {countRequestsFriend} заявок на дружбу в профиле: {_profileName}";
                LogManager.LogMessage(message, _logFileName);
                break;
            }
        }

        return Task.CompletedTask;
    }


    private async Task<bool> CheckElementCount(int expectedCount)
    {
        await Task.Delay(1000);

        var elements = _driver.FindElements(By.CssSelector("button[id^='accept_request_']"));

        if (elements.Count < expectedCount)
        {
            // Если количество элементов меньше ожидаемого, проверяем, есть ли кнопка "Показать еще"
            var showMoreButton = _driver.FindElements(By.CssSelector("button.show_more"));
            if (showMoreButton.Count > 0)
            {
                // Если есть кнопка "Показать еще", кликаем на нее
                showMoreButton[0].Click();
                return true;
            }
            else
            {
                // Если кнопки "Показать еще" нет, обновляем страницу и проверяем количество элементов еще раз
                _driver.Navigate().Refresh();
                return false;
            }
        }

        return true;
    }


    public async Task AcceptRequestAsync(IWebElement button)
    {
        try
        {
            var onclick = button.GetAttribute("onclick");
            var startIndex = onclick.IndexOf('(') + 1;
            var endIndex = onclick.IndexOf(',', startIndex);
            var userId = onclick.Substring(startIndex, endIndex - startIndex);
            var startIndex2 = onclick.IndexOf('\'', endIndex) + 1;
            var endIndex2 = onclick.IndexOf('\'', startIndex2);
            var requestId = onclick.Substring(startIndex2, endIndex2 - startIndex2);

            var jsExecutor = (IJavaScriptExecutor)_driver;
            var result = await Task.Run(() => jsExecutor.ExecuteScript($"Friends.acceptRequest({userId}, '{requestId}', arguments[0])", button));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Не удалось кликнуть на кнопку {ex.Message}");
        }
    }

    // Уходим и закрываем драйвер
    private async Task BeforeGoOn(string message, bool isWithLogMessage)
    {
        if (isWithLogMessage) { LogManager.LogMessage(message, _logFileName); }

        await Task.Delay(500);
        _driver.Navigate().GoToUrl(urlBeforeGoON);
        await Task.Delay(1000);
        _driver.Dispose();
    }
      

    public int ExtractNumberFromTitle()
    {
        string text = _driver.Title;
        string numberString = Regex.Match(text, @"\d+").Value;

        int number;
        if (int.TryParse(numberString, out number))
        {
            return number;
        }
        else
        {
            throw new Exception($"Failed to extract a number from the title: {text}");
        }
    }

    public async Task<Task> ProcessFriendRequests()
    {
        await Navigate();

        _countFriends = ExtractNumberFromTitle();

        await GetAllElementsOnPage();

        await BeforeGoOn("", false);

        return Task.CompletedTask;
    }

    // Проверка страницы на присутствие всплывающих окон
    private bool CheckPageForErrors()
    {
        try
        {
            // Ищем элемент с классом "box_title" и текстом "Ошибка"
            var errorTitle = _driver.FindElement(By.ClassName("box_title"));

            if (errorTitle != null || errorTitle?.Text == "Ошибка")
            {
                string? textlimitForAddFriendPerDay = "К сожалению, вы не можете добавлять больше друзей за один день. Пожалуйста, попробуйте завтра.";
                var limitForAddFriendPerDay = _driver.FindElements(By.ClassName("box_body"))
                                         .FirstOrDefault(e => e.Text.Contains(textlimitForAddFriendPerDay));


                string filePath = "sleepingProfiles.json";
                List<SleepingProfiles>? sleepingProfiles;

                if (File.Exists(filePath))
                {
                    sleepingProfiles = JsonConvert.DeserializeObject<List<SleepingProfiles>>(File.ReadAllText(filePath));
                }
                else
                {
                    Thread.Sleep(500);
                    sleepingProfiles = new List<SleepingProfiles>();
                    File.WriteAllText(filePath, JsonConvert.SerializeObject(sleepingProfiles));
                }

                if (limitForAddFriendPerDay != null)
                {
                    // Записываем в объект SleepingProfiles текущее время
                    SleepingProfiles currentProfile = new SleepingProfiles
                    {
                        FellAsleepProfile = DateTime.Now,
                        ProfilesId = _profileId
                    };

                    Thread.Sleep(500);
                    sleepingProfiles?.Add(currentProfile);
                    File.WriteAllText(filePath, JsonConvert.SerializeObject(sleepingProfiles));

                    _driver.Dispose();
                    Thread.Sleep(1000);

                    return true;
                }


                string? textLimit10000AddFriend = "К сожалению, вы не можете добавлять больше 10";
                var limit10000AddFriend = _driver.FindElements(By.ClassName("box_body"))
                                         .FirstOrDefault(e => e.Text.Contains(textLimit10000AddFriend));

                if (limit10000AddFriend != null)
                {
                    // Записываем в объект SleepingProfiles текущее время
                    SleepingProfiles currentProfile = new SleepingProfiles
                    {
                        LimitProfile = true,
                        ProfilesId = _profileId
                    };

                    Thread.Sleep(500);
                    sleepingProfiles?.Add(currentProfile);
                    File.WriteAllText(filePath, JsonConvert.SerializeObject(sleepingProfiles));

                    // Запись в логи
                    string message1 = $"Профиль {_profileName} заполнен под горлышко, 10 000 лимит";
                    LogManager.LogMessage(message1, _logFileName);

                    _driver.Dispose();
                    Thread.Sleep(1000);

                    return true;
                }
            }
        }
        catch (Exception) { }


        return false;
    }

}

