using Demo4.Core;
using Demo4.Core.Messages;
using Kw.Micro.Communications;
using System.Text;
using AutoMapper;
using Kw.Common;
using Kw.Micro;
using Kw.Micro.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;

#pragma warning disable CS4014

namespace Demo4.Worker
{
    internal class Server(ICommunicator communicator, ILogger<Server> logger, IMapper mapper)
    {
        readonly Demo4Node node = new();

        /// <summary>
        /// Начать прослушивание очередей
        /// </summary>
        public void Listen()
        {
            communicator.Read(node, QueueNames.UPLOAD, typeof(UploadMessage), OnUpload);
            communicator.Read(node, QueueNames.DOCUMENTS_REQUEST, typeof(DocumentsRequestMessage), OnDocumentsRequest);
            communicator.Read(node, QueueNames.CONTENT_REQUEST, typeof(ContentRequestMessage), OnContentRequest);
        }

        async Task<bool> OnUpload(CommunicatorMessage x)
        {
            var message = (UploadMessage)x;

            logger.Write(LL.I, $"Processing an upload request for document {message.FileName}");

            Document doc = mapper.Map<Document>(message);

            using (var scope = ServiceEnvironment.Scope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DocumentContext>();

                await context.Documents.AddAsync(doc);
                await context.SaveChangesAsync();

                logger.Write(LL.I, $"Document {doc.FileName} saved to database as {doc.Id}");

                UploadResponseMessage response = new()
                {
                    Id = message.Id,
                    DocumentId = doc.Id,
                };

                communicator.Write(node, QueueNames.UPLOAD_RESPONSE, response);
            }

            //
            // запуск извлечения содержимого в отдельном потоке
            //
            ExtractContent(doc.Id);

            return true;
        }

        async Task ExtractContent(Guid id)
        {
            using (var scope = ServiceEnvironment.Scope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DocumentContext>();

                Document? doc = await context.Documents.AsTracking().SingleOrDefaultAsync(x => x.Id == id);

                if (null != doc)
                {
                    await ExtractContent(doc);
                    await context.SaveChangesAsync();

                    logger.Write(LL.I, $"Content extracted for document {doc.Id} [{doc.FileName}]");
                }
            }
        }

        async Task ExtractContent(Document doc)
        {
            try
            {
                using (TemporaryFile file = new())
                {
                    using (Stream stream = file.OpenWrite())
                    {
                        await stream.WriteAsync(doc.FileData, 0, doc.FileSize);
                    }

                    using PdfDocument pdf = PdfDocument.Open(file.Path);

                    StringBuilder text = new();

                    foreach (var page in pdf.GetPages())
                        text.AppendLine(page.Text);

                    doc.Content = text.ToString();
                    doc.ContentExtracted = true;
                }
            }
            catch (Exception x)
            {
                logger.Write(x, $"Can't extract content from document {doc.Id} [{doc.FileName}]");
                throw;
            }
        }

        async Task<bool> OnDocumentsRequest(CommunicatorMessage x)
        {
            var message = (DocumentsRequestMessage)x;

            logger.Write(LL.I, $"Processing a documents request for Id={message.DocumentId}");

            DocumentsResponseMessage response;

            using (var scope = ServiceEnvironment.Scope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DocumentContext>();

                IQueryable<Document> query = context.Documents;

                if (null != message.DocumentId)
                    query = query.Where(d => message.DocumentId == d.Id);

                Document[] documents = await query.ToArrayAsync();

                response = new()
                {
                    Id = message.Id,
                    Documents = mapper.Map<DocumentResponse[]>(documents),
                };
            }

            communicator.Write(node, QueueNames.DOCUMENTS_RESPONSE, response);
            
            return true;
        }

        async Task<bool> OnContentRequest(CommunicatorMessage x)
        {
            var message = (ContentRequestMessage)x;

            logger.Write(LL.I, $"Processing a content request for Id={message.DocumentId}");

            ContentResponseMessage response;

            using (var scope = ServiceEnvironment.Scope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DocumentContext>();

                Document? document = await context.Documents.SingleOrDefaultAsync(d => message.DocumentId == d.Id);

                response = new()
                {
                    Id = message.Id,
                    Content = document?.Content ?? $"<document with Id {message.DocumentId} not found>",
                };
            }

            communicator.Write(node, QueueNames.CONTENT_RESPONSE, response);
            
            return true;
        }
    }
}
