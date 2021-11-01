using AutoMapper;

using SharpC2.API.V1.Requests;
using SharpC2.API.V1.Responses;

using TeamServer.Models;

namespace TeamServer.Mapping
{
    public class DroneProfiles : Profile
    {
        public DroneProfiles()
        {
            CreateMap<DroneTaskRequest, DroneTask>();
            CreateMap<DroneTask, DroneTaskResponse>()
                .ForMember(t => t.Result, opt => opt.MapFrom(t => t.Result));

            CreateMap<DroneModule, DroneModuleResponse>();
            CreateMap<DroneModule.Command, DroneModuleResponse.CommandResponse>();
            CreateMap<DroneModule.Command.Argument, DroneModuleResponse.CommandResponse.ArgumentResponse>();
            
            CreateMap<Drone, DroneResponse>()
                .ForMember(d => d.Guid, opt => opt.MapFrom(d => d.Metadata.Guid))
                .ForMember(d => d.Parent, opt => opt.MapFrom(d => d.Parent))
                .ForMember(d => d.Address, opt => opt.MapFrom(d => d.Metadata.Address))
                .ForMember(d => d.Hostname, opt => opt.MapFrom(d => d.Metadata.Hostname))
                .ForMember(d => d.Username, opt => opt.MapFrom(d => d.Metadata.Username))
                .ForMember(d => d.Process, opt => opt.MapFrom(d => d.Metadata.Process))
                .ForMember(d => d.Pid, opt => opt.MapFrom(d => d.Metadata.Pid))
                .ForMember(d => d.Integrity, opt => opt.MapFrom(d => d.Metadata.Integrity))
                .ForMember(d => d.Arch, opt => opt.MapFrom(d => d.Metadata.Arch))
                .ForMember(d => d.Modules, opt => opt.MapFrom(d => d.Modules));
        }
    }
}