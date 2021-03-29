using System;
using System.Collections.Generic;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Extensions;
using Ductus.FluentDocker.Services;
using FluentAssertions;
using Xunit;


namespace HwcTests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            
            // using var container = new Builder()
            //     .DefineImage("Test")
            //     
            //     .UseContainer()
            //     .UseImage("mcr.microsoft.com/dotnet/aspnet:5.0")
            //     .Mount(@"C:/projects/funnyquotes/artifacts/FunnyQuotesUICore", "c:/app")
            //     .ExposePort(5432)
            //     .WithEnvironment(
            //         "PORT=8080",
            //         "APPROOTDIR=/app")
            //     .WaitForPort("8080/tcp", 30000 /*30s*/)
            //     .Build()
            //     .Start();
            // var config = container.GetConfiguration(true);
            // config.State.ToServiceState().Should().Be(ServiceRunningState.Running);
            
        }
    }
}