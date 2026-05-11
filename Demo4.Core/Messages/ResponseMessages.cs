using Kw.Micro.Communications;

namespace Demo4.Core.Messages
{
    /// <summary>
    /// Данные о документе
    /// </summary>
    public class DocumentResponse
    {
        public Guid Id { get; set; }
        public string FileName { get; set; }
        public int FileSize { get; set; }
        public bool ContentExtracted { get; set; }
    }

    public class Demo4ResponseMessage : CommunicatorMessage
    {
        public Guid Id { get; set; } // id исходного запроса
    }
    
    /// <summary>
    /// Ответ на запрос списка документов
    /// </summary>
    public class DocumentsResponseMessage : Demo4ResponseMessage
    {
        public DocumentResponse[] Documents { get; set; }
    }

    /// <summary>
    /// Ответ на запрос содержимого
    /// </summary>
    public class ContentResponseMessage : Demo4ResponseMessage
    {
        public string Content { get; set; }
    }

    /// <summary>
    /// Ответ на загрузку документа
    /// </summary>
    public class UploadResponseMessage : Demo4ResponseMessage
    {
        public Guid DocumentId { get; set; }
    }
}