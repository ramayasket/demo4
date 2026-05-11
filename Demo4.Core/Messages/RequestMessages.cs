using Kw.Micro.Communications;

namespace Demo4.Core.Messages
{
    public class Demo4RequestMessage : CommunicatorMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
    }
    
    /// <summary>
    /// Информация о документе для отправки на обработку
    /// </summary>
    public class UploadMessage : Demo4RequestMessage
    {
        public string FileName { get; set; }
        public byte[] FileData { get; set; }
    }

    /// <summary>
    /// Запрос списка документов
    /// </summary>
    public class DocumentsRequestMessage : Demo4RequestMessage
    {
        public Guid? DocumentId { get; set; } // если пусто, то все документы
    }

    /// <summary>
    /// Запрос содержимого документа
    /// </summary>
    public class ContentRequestMessage : Demo4RequestMessage
    {
        public Guid DocumentId { get; set; }
    }
}