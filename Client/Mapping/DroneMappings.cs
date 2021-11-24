using AutoMapper;

using SharpC2.API.V1.Responses;
using SharpC2.Models;

namespace SharpC2.Mapping
{
    public class DroneMappings : Profile
    {
        public DroneMappings()
        {
            CreateMap<DroneModuleResponse.CommandResponse.ArgumentResponse, DroneModule.Command.Argument>();
            CreateMap<DroneModuleResponse.CommandResponse, DroneModule.Command>();
            CreateMap<DroneModuleResponse, DroneModule>();
            CreateMap<DroneResponse, Drone>();
        }
    }
}