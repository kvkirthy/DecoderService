using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OCRWebApi.Models
{
    public interface ILogger
    {
        void AppendMessages(string message);
        bool LogAppendedMessages(EventLogEntryType entryType);
        bool Log(string message, EventLogEntryType entryType);
    }
}
