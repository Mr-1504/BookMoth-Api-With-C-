using BookMoth_Api_With_C_.Models;
using BookMoth_Api_With_C_.ViewModels;
using Microsoft.AspNetCore.Mvc;


namespace BookMoth_Api_With_C_.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private BookMothContext _context = new BookMothContext();
        // GET: api/<AccountController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<AccountController>/5
        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            Account account = _context.Accounts.FirstOrDefault(x => x.AccountId == id);
            if (account == null)
            {
                return NotFound();
            }
            AccountViewModel accountViewModel = new AccountViewModel
            {
                AccountId = account.AccountId,
                Email = account.Email,
                Password = account.Password,
                AccountType = account.AccountType
            };
            return Ok(accountViewModel);
        }

        // POST api/<AccountController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<AccountController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<AccountController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
