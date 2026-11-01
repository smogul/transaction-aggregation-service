using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using transaction_aggregation_api.Models;
using transaction_aggregation_app.Statements;
using transaction_aggregation_domain.Entities;

namespace transaction_aggregation_api.Controllers
{
    [ApiController]
    [Route("api")]
    public class StatementsController : ControllerBase
    {
        private readonly StatementService _statementService;

        public StatementsController(StatementService statementService)
        {
            _statementService = statementService;
        }

        private string StatementsBaseUrl => $"{Request.Scheme}://{Request.Host}/api/statements";

        [HttpPost("accounts/{accountId}/statements")]
        public async Task<ActionResult<Statement>> CreateStatement(string accountId, CreateStatementRequest request)
        {
            var statement = await _statementService.CreateStatementAsync(accountId, request.FromDate!.Value, request.ToDate!.Value);

            return CreatedAtAction(nameof(GetStatements), new { accountId = statement.AccountId }, statement);
        }

        [HttpPost("accounts/{accountId}/statements/download-link")]
        public async Task<ActionResult<DownloadLinkDTO>> CreateStatementDownloadLink(string accountId, [FromQuery, Required] DateTime? fromDate, [FromQuery, Required] DateTime? toDate)
        {
            return Ok(await _statementService.CreateStatementDownloadLinkAsync(accountId, fromDate!.Value, toDate!.Value, StatementsBaseUrl));
        }

        [HttpPost("accounts/{accountId}/statements/upload")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<Statement>> UploadStatement(string accountId, [FromForm] UploadStatementRequest request)
        {
            using var memory = new MemoryStream();
            await request.File!.CopyToAsync(memory);

            var statement = await _statementService.UploadStatementAsync(accountId, request.File.FileName, memory.ToArray());

            return CreatedAtAction(nameof(GetStatements), new { accountId = statement.AccountId }, statement);
        }

        [HttpGet("accounts/{accountId}/statements")]
        public async Task<ActionResult<IEnumerable<Statement>>> GetStatements(string accountId)
        {
            return Ok(await _statementService.GetAccountStatementsAsync(accountId));
        }

        [HttpPost("statements/{statementId}/download-link")]
        public async Task<ActionResult<DownloadLinkDTO>> CreateDownloadLink(string statementId)
        {
            return Ok(await _statementService.CreateDownloadLinkAsync(statementId, StatementsBaseUrl));
        }

        [HttpGet("statements/{statementId}/download")]
        public async Task<IActionResult> DownloadStatement(string statementId)
        {
            var (fileName, content) = await _statementService.DownloadStatementAsync(statementId);

            Response.Headers.CacheControl = "no-store";

            return File(content, "application/pdf", fileName);
        }
    }
}
