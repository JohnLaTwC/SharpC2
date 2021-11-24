using AutoMapper;

using SharpC2.API.V1.Responses;
using SharpC2.Models;

namespace SharpC2.Mapping
{
    public class CredentialMappings : Profile
    {
        public CredentialMappings()
        {
            CreateMap<CredentialRecordResponse, CredentialRecord>();
        }
    }
}