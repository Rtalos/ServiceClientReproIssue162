using Microsoft.AspNetCore.Mvc;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace ServiceClientReproIssue162.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly ServiceClient _serviceClient;

        public AccountsController(ServiceClient serviceClient)
        {
            _serviceClient = serviceClient;
        }

        [HttpGet("~/accounts")]
        public IActionResult GetAccount()
        {
            using (var context = new CrmServiceContext(_serviceClient))
            {
                var account = new Account();
                account.Name = "Test ABC";

                context.AddObject(account);
                context.SaveChanges();
            }

            return Ok();
        }
    }
}
