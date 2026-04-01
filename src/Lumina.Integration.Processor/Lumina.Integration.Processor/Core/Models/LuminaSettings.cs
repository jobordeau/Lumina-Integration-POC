using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lumina.Integration.Processor.Core.Models
{
    public class LuminaSettings
    {
        public string ServiceBusConnectionString { get; set; } = string.Empty;
        public string TopicName { get; set; } = string.Empty;
        public string DataLakeConnection { get; set; } = string.Empty;
    }
}
