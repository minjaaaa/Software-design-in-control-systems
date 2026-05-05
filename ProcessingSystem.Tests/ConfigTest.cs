using Kolokvijum1;
using ProcessingSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ProcessingSystem.Tests
{
    public class ConfigTest
    {
        [Fact]
        public void Load_ValidXml_ReturnsCorrectConfiguration()
        {
            // Act
            var config = SystemConfigLoader.Load("SystemConfig.xml");

            // Assert
            Assert.NotNull(config);
            Assert.True(config.WorkerCount > 0);
            Assert.True(config.MaxQueueSize > 0);
            Assert.NotEmpty(config.InitialJobs);
        }
    }
}
