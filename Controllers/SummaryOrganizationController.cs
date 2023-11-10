using Microsoft.AspNetCore.Mvc;
using PrologMobileCodingChallenge;
using System.Text.Json;
using System.Threading.Tasks;


[Route("api/[controller]")]
public class SummaryOrganizationController : Controller
{
    private  string baseUrl = "https://607a0575bd56a60017ba2618.mockapi.io/organization";
    private readonly ILogger<SummaryOrganizationController> _logger;

    private  HttpClient httpClient;

    public SummaryOrganizationController(HttpClient _httpClient,ILogger<SummaryOrganizationController> logger)
    {
        this.httpClient = _httpClient;
         _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpGet]
    public async Task<IActionResult> GetOrganizationSummary()
    {
        var organizationsT =  GetOrganizations();
        var organizations= await organizationsT;
        
        var summariesT = GetSummaries(organizations);

        return Ok(Task.WhenAll(summariesT));
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
        var cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;

        List<SummaryOrganization> summaries = new List<SummaryOrganization>();      
        await Task.WhenAll(organizations.Select(async requestNumber =>
        {
             int totalBlacklistedPhone=0;
             int totalPhoneUser=0;
             var userOrganizations= await GetUsersOrganization(requestNumber.id,token);  

                try{
                        await Task.WhenAll(userOrganizations.Select(async user =>
                            {
                               
                             var userInfomation= await GetUsersInformation(requestNumber.id,user.id,token);

                               foreach (var userInfo in userInfomation)
                                {

                                    if (userInfo.blacklisted)
                                    {
                                        totalBlacklistedPhone++;
                                        totalPhoneUser++;
                                    }
                                }

                            }));

                    }catch(Exception ex){  
                        _logger.LogError($"Error processing organization: {ex.Message}");                            

                    }
               summaries.Add(new SummaryOrganization
                {
                    id = requestNumber.id,
                    name = requestNumber.name,
                    totalCount = totalPhoneUser,

                    backlistTotal = totalBlacklistedPhone,
                    users = userOrganizations,
                });
        }));
        return summaries;
    }

    private async Task<List<Users>> GetUsersOrganization(string organizationId, CancellationToken  token)
    {

         try
        {
               HttpResponseMessage response;
          
                response = await httpClient.GetAsync($"{baseUrl}/{organizationId}/users");
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests) {
                    //Console.WriteLine("Too many requests.");
                    token.ThrowIfCancellationRequested();
                    await Task.Delay(100, token); 
                    return await GetUsersOrganization( organizationId,   token);
                }

            if (response.IsSuccessStatusCode)
            {
                var usersJson = await response.Content.ReadAsStringAsync();
                var users = JsonSerializer.Deserialize<List<Users>>(usersJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return users;
            }
            else
            {
                 _logger.LogWarning("Operation cancelled in GetUsersInformation method.");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching user information: {ex.Message}");
            return null;
        }
        
   
    }

    private async Task<List<UserOrganization>> GetUsersInformation(string organizationId, string userId,CancellationToken token)
    {
        try {
            var usersResponse = await httpClient.GetAsync($"{baseUrl}/{organizationId}/users/{userId}/phones", token);
            var usersJson = await usersResponse.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<List<UserOrganization>>(usersJson);
            return users;
        }
        catch (OperationCanceledException) {

            Console.WriteLine("Operation cancelled in GetUsersInformation method.");
            await Task.Delay(200); 
            return await GetUsersInformation(organizationId, userId, token); 

        }catch (Exception ex){
            
             Console.WriteLine($"Error fetching user information: {ex.Message}");
            return null;
         }

    }

}

