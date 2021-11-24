using AutoMapper;

using SharpC2.API.V1.Responses;
using SharpC2.Models;

namespace SharpC2.Mapping
{
    public class HostedFileMappings : Profile
    {
        public HostedFileMappings()
        {
            CreateMap<HostedFileResponse, HostedFile>();
        }
    }
}