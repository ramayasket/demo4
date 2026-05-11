using Demo4.Core;
using Demo4.Core.Messages;
using Kw.Common;
using Kw.Micro.Communications;
using Kw.Micro.Logging;

namespace Demo4.Api
{
    public class Client
    {
        public const int PDF_LIMIT = 100; // максимальный размер файла в KB чтобы не убить производительность RMQ
        public const int KB = 1024;

        readonly ILogger<Client> logger;

        /// <summary>
        /// Коммуникатор (в нашем случае -- RabbitMQ)
        /// </summary>
        readonly ICommunicator communicator;
        
        /// <summary>
        /// Узел коммуникатора (эксчендж RabbitMQ)
        /// </summary>
        readonly Demo4Node node = new();

        /// <summary>
        /// Токен асинхронного ожидания
        /// </summary>
        class SyncAsyncToken
        {
            public readonly TaskBasedEvent Event = new();
            public Demo4ResponseMessage Message;
        }

        /// <summary>
        /// Реестр токенов, ожидающих ответного сообщения
        /// </summary>
        readonly Dictionary<Guid, SyncAsyncToken> syncAsyncTokens = new();

        public Client(ICommunicator c, ILogger<Client> l)
        {
            communicator = c;
            logger = l;

            communicator.Read(node, QueueNames.UPLOAD_RESPONSE, typeof(UploadResponseMessage), OnUploadResponse);
            communicator.Read(node, QueueNames.DOCUMENTS_RESPONSE, typeof(DocumentsResponseMessage), OnDocumentsResponse);
            communicator.Read(node, QueueNames.CONTENT_RESPONSE, typeof(ContentResponseMessage), OnContentResponse);
        }

        ////
        //// Обработка комманд контроллера
        ////

        public async Task<Guid> Upload(IFormFile file)
        {
            if (0 == file.Length)
                throw new ArgumentNullException(nameof(file), "Empty file");

            if (PDF_LIMIT * KB < file.Length)
                throw new ArgumentOutOfRangeException(nameof(file), $"File size exceeds the limit of {PDF_LIMIT} KB");

            byte[] data = new byte[file.Length];

            using (Stream s = file.OpenReadStream())
            {
                _ = await s.ReadAsync(data, 0, (int)file.Length);
            }

            UploadMessage message = new()
            {
                FileName = file.FileName,
                FileData = data
            };

            communicator.Write(node, QueueNames.UPLOAD, message);

            logger.Write(LL.I, $"Sent an upload request for document {file.FileName}");

            SyncAsyncToken token = await BlockAsync(message.Id);

            return ((UploadResponseMessage)token.Message).DocumentId;
        }

        public async Task<DocumentResponse[]> RequestDocuments(Guid? id)
        {
            DocumentsRequestMessage message = new() { DocumentId = id };

            communicator.Write(node, QueueNames.DOCUMENTS_REQUEST, message);

            logger.Write(LL.I, $"Sent a document list request for Id={id}");

            SyncAsyncToken token = await BlockAsync(message.Id);

            return ((DocumentsResponseMessage)token.Message).Documents;
        }

        public async Task<string> RequestContent(Guid id)
        {
            ContentRequestMessage message = new() { DocumentId = id };

            communicator.Write(node, QueueNames.CONTENT_REQUEST, message);

            logger.Write(LL.I, $"Sent a content request for Id={id}");

            SyncAsyncToken token = await BlockAsync(message.Id);

            return ((ContentResponseMessage)token.Message).Content;
        }

        /// <summary>
        /// Асинхронное ожидание ответа из очереди
        /// </summary>
        async Task<SyncAsyncToken> BlockAsync(Guid id)
        {
            SyncAsyncToken token = new();

            lock(syncAsyncTokens)
                syncAsyncTokens[id] = token;

            await token.Event.WaitAsync(); // ожидание без блокировки потока

            return token;
        }

        ////
        //// Обработка ответных сообщений от сервера
        ////

        async Task<bool> OnDocumentsResponse(CommunicatorMessage x)
        {
            var message = (DocumentsResponseMessage)x;

            logger.Write(LL.I, $"Received a documents response for request {message.Id}");

            Unblock(message);

            return true;
        }

        async Task<bool> OnContentResponse(CommunicatorMessage x)
        {
            var message = (ContentResponseMessage)x;

            logger.Write(LL.I, $"Received a content response for request {message.Id}");

            Unblock(message);

            return true;
        }

        async Task<bool> OnUploadResponse(CommunicatorMessage x)
        {
            var message = (UploadResponseMessage)x;

            logger.Write(LL.I, $"Received an upload response for request {message.Id}");

            Unblock(message);

            return true;
        }

        void Unblock(Demo4ResponseMessage message)
        {
            lock (syncAsyncTokens)
            {
                //
                // Если есть токен с id из ответного сообщения
                //
                if (syncAsyncTokens.TryGetValue(message.Id, out SyncAsyncToken? token))
                {
                    token.Message = message;
                    //
                    // Разблокируем асинхронное ожидание
                    //
                    token.Event.Set();

                    syncAsyncTokens.Remove(message.Id);
                }
            }
        }

        ////
        ////
        ////
    }
}
