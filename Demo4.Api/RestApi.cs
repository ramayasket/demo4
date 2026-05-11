using System.ComponentModel.DataAnnotations;
using Demo4.Core.Messages;
using Microsoft.AspNetCore.Mvc;

namespace Demo4.Api
{
    [ApiController]
    [Route("demo4")]
    public class RestApi(Client client) : ControllerBase
    {
        [HttpPost("upload")]
        public async Task<ActionResult> Upload(IFormFile file)
        {
            Guid id = await client.Upload(file);

            return Ok($"Document {file.FileName} uploaded: Id={id}");
        }

        [HttpGet("documents")]
        public async Task<ActionResult<DocumentResponse[]>> RequestDocuments(Guid? id)
        {
            var documents = await client.RequestDocuments(id);

            return Ok(documents);
        }

        [HttpGet("content")]
        public async Task<ActionResult<string>> RequestContent([Required] Guid id)
        {
            string content = await client.RequestContent(id);

            return Ok(content);
        }
    }
}
