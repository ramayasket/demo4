using AutoMapper;
using Demo4.Core.Messages;

namespace Demo4.Worker
{
    public class ResponseProfile : Profile
    {
        public ResponseProfile()
        {
            CreateMap<Document, DocumentResponse>();
        }
    }

    public class UploadProfile : Profile
    {
        public UploadProfile()
        {
            CreateMap<UploadMessage, Document>()
                .ForMember(dst => dst.FileSize, opt => opt.MapFrom(x => x.FileData.Length))
                .ForMember(dst => dst.Content, opt => opt.Ignore())
                .ForMember(dst => dst.ContentExtracted, opt => opt.Ignore())
                ;
        }
    }
}
