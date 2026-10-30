using Microsoft.AspNetCore.Mvc;
using transaction_aggregation_app.Accounts;
using transaction_aggregation_app.Balances;
using transaction_aggregation_app.Transactions;
using transaction_aggregation_domain.Entities;

namespace transaction_aggregation_api.Controllers
{
    // The door into everything about one account: its details, its balance and its transactions.
    // Behind the door, the work is still done by three separate modules: Accounts, Balances and Transactions.
    // A controller does no thinking of its own: it takes the question from the web request,
    // passes it to the right module, and sends the answer back.
    //
    // Every accountId is a 10-digit account number. Try 1000000001, 1000000002 or 2000000001.
    [ApiController]
    [Route("api/accounts")]
    public class AccountsController : ControllerBase
    {
        private readonly AccountService _accountService;
        private readonly BalanceService _balanceService;
        private readonly TransactionService _transactionService;

        public AccountsController(AccountService accountService, BalanceService balanceService, TransactionService transactionService)
        {
            _accountService = accountService;
            _balanceService = balanceService;
            _transactionService = transactionService;
        }

        /// <summary>
        /// The details of one account: its name, who owns it and how much it started with.
        /// </summary>
        /// <param name="accountId">The 10-digit account number, e.g. 1000000001. Spaces at the start or end are ignored.</param>
        [HttpGet("{accountId}")]
        public async Task<ActionResult<Account>> GetAccount(string accountId)
        {
            return Ok(await _accountService.GetAccountAsync(accountId));
        }

        /// <summary>
        /// How much money is in the account right now.
        /// </summary>
        /// <param name="accountId">The 10-digit account number, e.g. 1000000001.</param>
        [HttpGet("{accountId}/balance")]
        public async Task<ActionResult<BalanceDTO>> GetBalance(string accountId)
        {
            return Ok(await _balanceService.GetBalanceAsync(accountId));
        }

        /// <summary>
        /// Lists the account's transactions from all five data sources, already categorised, newest first.
        /// </summary>
        /// <remarks>
        /// Every filter is optional. Example: fromDate=2026-09-01, toDate=2026-09-30, category=Food, page=1, pageSize=20
        /// gives the first 20 Food transactions in September.
        /// </remarks>
        /// <param name="accountId">The 10-digit account number, e.g. 1000000001.</param>
        /// <param name="filter">Optional filters and paging.</param>
        [HttpGet("{accountId}/transactions")]
        public async Task<ActionResult<PagedResultDTO<Transaction>>> GetTransactions(string accountId, [FromQuery] TransactionFilter filter)
        {
            return Ok(await _transactionService.GetTransactionsAsync(accountId, filter));
        }

        /// <summary>
        /// Totals for the account: money in, money out, the difference, and a total per category.
        /// </summary>
        /// <param name="accountId">The 10-digit account number, e.g. 1000000001.</param>
        /// <param name="fromDate">Optional. The first day to include, e.g. 2026-09-01.</param>
        /// <param name="toDate">Optional. The last day to include, e.g. 2026-09-30.</param>
        [HttpGet("{accountId}/transactions/summary")]
        public async Task<ActionResult<TransactionSummaryDTO>> GetSummary(string accountId, DateTime? fromDate, DateTime? toDate)
        {
            return Ok(await _transactionService.GetSummaryAsync(accountId, fromDate, toDate));
        }

        /// <summary>
        /// Money in and out for each category. Example: how much went on Food.
        /// </summary>
        /// <param name="accountId">The 10-digit account number, e.g. 1000000001.</param>
        /// <param name="fromDate">Optional. The first day to include, e.g. 2026-09-01.</param>
        /// <param name="toDate">Optional. The last day to include, e.g. 2026-09-30.</param>
        [HttpGet("{accountId}/transactions/by-category")]
        public async Task<ActionResult<List<GroupTotalDTO>>> GetTotalsByCategory(string accountId, DateTime? fromDate, DateTime? toDate)
        {
            return Ok(await _transactionService.GetTotalsByCategoryAsync(accountId, fromDate, toDate));
        }

        /// <summary>
        /// Money in and out for each month. Example: "2026-09" is September 2026.
        /// </summary>
        /// <param name="accountId">The 10-digit account number, e.g. 1000000001.</param>
        /// <param name="fromDate">Optional. The first day to include, e.g. 2026-09-01.</param>
        /// <param name="toDate">Optional. The last day to include, e.g. 2026-09-30.</param>
        [HttpGet("{accountId}/transactions/by-month")]
        public async Task<ActionResult<List<GroupTotalDTO>>> GetTotalsByMonth(string accountId, DateTime? fromDate, DateTime? toDate)
        {
            return Ok(await _transactionService.GetTotalsByMonthAsync(accountId, fromDate, toDate));
        }

        /// <summary>
        /// Money in and out for each data source. Example: how much came from the Excel file.
        /// </summary>
        /// <param name="accountId">The 10-digit account number, e.g. 1000000001.</param>
        /// <param name="fromDate">Optional. The first day to include, e.g. 2026-09-01.</param>
        /// <param name="toDate">Optional. The last day to include, e.g. 2026-09-30.</param>
        [HttpGet("{accountId}/transactions/by-source")]
        public async Task<ActionResult<List<GroupTotalDTO>>> GetTotalsBySource(string accountId, DateTime? fromDate, DateTime? toDate)
        {
            return Ok(await _transactionService.GetTotalsBySourceAsync(accountId, fromDate, toDate));
        }
    }
}
