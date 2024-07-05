using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SampleOtlp.Monitoring.Extensions;

public static class LogExtension
{
    public static ILogger WithEntityId(this ILogger logger, string entityId)
    {
        return logger;
    }
}