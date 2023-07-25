// get

using System.Net;
using HtmlAgilityPack;

const string mainPage = "https://pieraksts.mfa.gov.lv/ru/moskva";
// post
const string step1Page = "https://pieraksts.mfa.gov.lv/ru/moskva/index";
// post
const string step2Page = "https://pieraksts.mfa.gov.lv/ru/moskva/step2";
// get
const string datesPage = "https://pieraksts.mfa.gov.lv/ru/calendar/available-month-dates?year={0}&month={1}";

// xpath for token
const string tokenXpath = "//input[@name='_csrf-mfa-scheduler']";

// dates not available response text
const string datesNotAvailable = "Šobrīd visi pieejamie laiki ir aizņemti";

var cookieContainer = new CookieContainer();
var handler = new HttpClientHandler
{
    CookieContainer = cookieContainer,
    UseCookies = true,
    UseDefaultCredentials = false,
    AllowAutoRedirect = true,
    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
};
var client = new HttpClient(handler);

// step 1 - initial request, getting token

Console.WriteLine("Step 1");

var response = await client.GetAsync(mainPage);
response.EnsureSuccessStatusCode();

var content = await response.Content.ReadAsStringAsync();
var doc = new HtmlDocument();
doc.LoadHtml(content);

var token = doc.DocumentNode.SelectSingleNode(tokenXpath)?.GetAttributeValue("value", null);

if (string.IsNullOrWhiteSpace(token))
    throw new Exception("Can't find scheduler");

Console.WriteLine("Token: " + token);
    
// step 2 - set data

Console.WriteLine("Step 2");

response = await client.PostAsync(step1Page, new FormUrlEncodedContent(new Dictionary<string, string>()
{
    {"_csrf-mfa-scheduler", token},
    {"branch_office_id", "22"},
    {"Persons[0][first_name]", "Elizaveta"},
    {"Persons[0][last_name]", "Zubareva"},
    {"e_mail", "elizavetavzubareva99@gmail.com"},
    {"e_mail_repeat", "elizavetavzubareva99@gmail.com"},
    {"phone", "+79066413336"},
}));

response.EnsureSuccessStatusCode();

content = await response.Content.ReadAsStringAsync();
doc = new HtmlDocument();
doc.LoadHtml(content);

token = doc.DocumentNode.SelectSingleNode(tokenXpath)?.GetAttributeValue("value", null);

if (string.IsNullOrWhiteSpace(token))
    throw new Exception("Can't find scheduler");
    
Console.WriteLine("New token: " + token);

// step 3 - request dates page

Console.WriteLine("Step 3");

response = await client.PostAsync(step2Page, new FormUrlEncodedContent(new Dictionary<string, string>()
{
    {"_csrf-mfa-scheduler", token},
    {"Persons[0][service_ids][]", "55"},
}));

response.EnsureSuccessStatusCode();

Console.WriteLine("Step 3 done");

// step 4 - checking available dates

Console.WriteLine("Step 4 - checking available dates");

var currentMonthRequestPath = string.Format(datesPage, DateTime.UtcNow.Year, DateTime.UtcNow.Month);

response = await client.GetAsync(currentMonthRequestPath);

response.EnsureSuccessStatusCode();

content = await response.Content.ReadAsStringAsync();

if (!content.Contains(datesNotAvailable))
{
    Console.WriteLine("Dates available for current month: " + content);
    return;
}
    
Console.WriteLine($"Dates not available for current month {DateTime.UtcNow.Month}. Checking next month");

var nextMonthRequestPath = string.Format(datesPage, DateTime.UtcNow.Year, DateTime.UtcNow.Month + 1);

response = await client.GetAsync(nextMonthRequestPath);

response.EnsureSuccessStatusCode();

content = await response.Content.ReadAsStringAsync();

if (!content.Contains(datesNotAvailable))
{
    Console.WriteLine("Dates available for next month: " + content);
    return;
}

Console.WriteLine($"Dates not available for next month {DateTime.UtcNow.Month + 1}. Checking next month");