using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SignalRTest.Repository;
using SIgnalRTest.Domain.IServices;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalRTest.Service
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddRepoService(this IServiceCollection service)
        {
            service.AddScoped<IMessageRepository, MessageRepository>();
            service.AddScoped<IGroupRepository, GroupRepository>();
            return service;
        }
    }
}
