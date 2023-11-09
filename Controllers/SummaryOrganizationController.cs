using Microsoft.AspNetCore.Mvc;
using PrologMobileCodingChallenge;
using System.Text.Json;
using System.Threading.Tasks;


[Route("api/[controller]")]
public class SummaryOrganizationController : Controller
{
    private  string baseUrl = "https://607a0575bd56a60017ba2618.mockapi.io/organization";
    private  HttpClient httpClient;

    public SummaryOrganizationController(HttpClient _httpClient)
    {
        this.httpClient = _httpClient;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrganizationSummary()
    {
        var organizations = await GetOrganizations();

        var summaries = await GetSummaries(organizations);

        return Ok(summaries);
    }

    private async Task<List<Organization>> GetOrganizations()
    {
        var organizationsResponse = await httpClient.GetAsync($"{baseUrl}");
        var organizationsJson = await organizationsResponse.Content.ReadAsStringAsync();
        var organizations = JsonSerializer.Deserialize<List<Organization>>(organizationsJson);
        return organizations;
    }

    private async Task<List<SummaryOrganization>> GetSummaries(List<Organization> organizations)
    {
        List<SummaryOrganization> summaries = new List<SummaryOrganization>();

        foreach (var organization in organizations)
        {
            int totalBlacklistedPhones = 0;
            int totalPhoneUser = 0;

            var users = await GetUsersOrganization(organization.id);

            foreach (var user in users)
            {

                var userOrganizations = await GetUsersInformation(organization.id, user.id);
                // int number = 0;
                foreach (var userInfo in userOrganizations)
                {

                    if (userInfo.blacklisted)
                    {
                        totalBlacklistedPhones++;
                        totalPhoneUser++;
                    }
                }
            }

            summaries.Add(new SummaryOrganization
            {
                id = organization.id,
                name = organization.name,
                backlistTotal = totalBlacklistedPhones,
                totalCount = totalPhoneUser,
                users = users,
            });

        }
        return summaries;
    }

    private async Task<List<Users>> GetUsersOrganization(string organizationId)
    {
        try
        {
            System.Threading.Thread.Sleep(1000);
            var usersResponse = await httpClient.GetAsync($"{baseUrl}/{organizationId}/users");
            usersResponse.EnsureSuccessStatusCode();
            var usersJson = await usersResponse.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<List<Users>>(usersJson);
            return users;

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching users: {ex.Message}");
            return null;
        }
    }

    private async Task<List<UserOrganization>> GetUsersInformation(string organizationId, string userId)
    {
        var usersResponse = await httpClient.GetAsync($"{baseUrl}/{organizationId}/users/{userId}/phones");
        var usersJson = await usersResponse.Content.ReadAsStringAsync();
        var users = JsonSerializer.Deserialize<List<UserOrganization>>(usersJson);
        return users;

    }
}

