using Microsoft.AspNetCore.Mvc;
using transaction_aggregation_api.Models;
using transaction_aggregation_app.Statements;
using transaction_aggregation_domain.Entities;

namespace transaction_aggregation_api.Controllers
{
    // The door into the Statements module.
    [ApiController]
    [Route("api")]
    public class StatementsController : ControllerBase
    {
        private readonly StatementService _statementService;

        public StatementsController(StatementService statementService)
        {
            _statementService = statementService;
        }

        /// <summary>
        /// Step 1 (option A): make a PDF statement from the account's transactions.
        /// </summary>
        /// <remarks>Example body: { "fromDate": "2026-09-01", "toDate": "2026-09-30" }</remarks>
        /// <param name="accountId">The 10-digit account number, e.g. 1000000001.</param>
        /// <param name="request">The first and last day the statement covers.</param>
        [HttpPost("accounts/{accountId}/statements")]
        public async Task<ActionResult<Statement>> CreateStatement(string accountId, CreateStatementRequest request)
        {
            var statement = await _statementService.CreateStatementAsync(accountId, request.FromDate!.Value, request.ToDate!.Value);

            return CreatedAtAction(nameof(GetStatements), new { accountId = statement.AccountId }, statement);
        }

        /// <summary>
        /// Step 1 (option B): upload a statement file, and we turn it into a new PDF statement.
        /// </summary>
        /// <remarks>
        /// You can upload a PDF, Word (.docx), Excel (.xlsx), picture (.png, .jpg) or text (.txt, .csv, .json, .xml, .md) file.
        /// A PDF is kept as it is. Any other file is drawn onto a PDF page with the account details at the top.
        /// </remarks>
        /// <param name="accountId">The 10-digit account number, e.g. 1000000001.</param>
        /// <param name="request">The file, and the first and last day it covers.</param>
        [HttpPost("accounts/{accountId}/statements/upload")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<Statement>> UploadStatement(string accountId, [FromForm] UploadStatementRequest request)
        {
            // Copy the uploaded file into memory, so the Statements module gets plain bytes.
            using var memory = new MemoryStream();
            await request.File!.CopyToAsync(memory);

            var statement = await _statementService.UploadStatementAsync(
                accountId, request.FromDate!.Value, request.ToDate!.Value, request.File.FileName, memory.ToArray());

            return CreatedAtAction(nameof(GetStatements), new { accountId = statement.AccountId }, statement);
        }

        /// <summary>
        /// Every statement that has been made for an account, newest first.
        /// </summary>
        /// <param name="accountId">The 10-digit account number, e.g. 1000000001.</param>
        [HttpGet("accounts/{accountId}/statements")]
        public async Task<ActionResult<IEnumerable<Statement>>> GetStatements(string accountId)
        {
            return Ok(await _statementService.GetAccountStatementsAsync(accountId));
        }

        /// <summary>
        /// Step 2: get a secret download link for a statement. The link stops working after a few minutes.
        /// </summary>
        /// <param name="statementId">The statement's id from step 1, e.g. 3f2b6c1e-8d4a-4c3b-9e51-2a7d0f6b1c9e.</param>
        [HttpPost("statements/{statementId}/download-link")]
        public async Task<ActionResult<DownloadLinkDTO>> CreateDownloadLink(string statementId)
        {
            var downloadBaseUrl = $"{Request.Scheme}://{Request.Host}/api/statements/download";

            return Ok(await _statementService.CreateDownloadLinkAsync(statementId, downloadBaseUrl));
        }

        /// <summary>
        /// Step 3: download the PDF using the secret link.
        /// </summary>
        /// <param name="token">The last part of the link from step 2.</param>
        [HttpGet("statements/download/{token}")]
        public async Task<IActionResult> DownloadStatement(string token)
        {
            var (fileName, content) = await _statementService.DownloadStatementAsync(token);

            // Ask browsers and proxies not to keep a copy of the bank statement.
            Response.Headers.CacheControl = "no-store";

            return File(content, "application/pdf", fileName);
        }
    }
}
