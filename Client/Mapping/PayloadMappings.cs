using AutoMapper;

using SharpC2.API.V1.Responses;
using SharpC2.Models;

namespace SharpC2.Mapping
{
    public class PayloadMappings : Profile
    {
        public PayloadMappings()
        {
            CreateMap<PayloadResponse, Payload>();
        }
    }
}