namespace Demo4.Core
{
    public class QueueNames
    {
        //
        // Запросы (обрабатываются воркером)
        //
        public const string UPLOAD = "Worker.Upload";
        public const string DOCUMENTS_REQUEST = "Worker.Documents";
        public const string CONTENT_REQUEST = "Worker.Content";

        //
        // Ответы (обрабатываются API)
        //
        public const string UPLOAD_RESPONSE = "Api.Upload";
        public const string DOCUMENTS_RESPONSE = "Api.Documents";
        public const string CONTENT_RESPONSE = "Api.Content";
    }
}
