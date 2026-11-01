using Microsoft.AspNetCore.Mvc;
using transaction_aggregation_app.Accounts;
using transaction_aggregation_app.Balances;
using transaction_aggregation_domain.Entities;

namespace transaction_aggregation_api.Controllers
{
    [ApiController]
    [Route("api/accounts")]
    public class AccountsController : ControllerBase
    {
        private readonly AccountService _accountService;
        private readonly BalanceService _balanceService;

        public AccountsController(AccountService accountService, BalanceService balanceService)
        {
            _accountService = accountService;
            _balanceService = balanceService;
        }

        [HttpGet("{accountId}")]
        public async Task<ActionResult<Account>> GetAccount(string accountId)
        {
            return Ok(await _accountService.GetAccountAsync(accountId));
        }

        [HttpGet("{accountId}/balance")]
        public async Task<ActionResult<BalanceDTO>> GetBalance(string accountId)
        {
            return Ok(await _balanceService.GetBalanceAsync(accountId));
        }
    }
}
