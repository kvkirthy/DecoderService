using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web;

namespace OCRWebApi.Models
{
    public class Logger : ILogger
    {
        
        EventLog _logger;
        public Logger()
        {
            string _logName = "Decoder";
            try
            {                
                if (!EventLog.SourceExists(_logName))
                    EventLog.CreateEventSource(_logName, _logName);

                _logger = new EventLog(_logName);
                _logger.Source = _logName;

            }
            catch
            {
                 _logger = new EventLog("Application");
                 _logger.Source = "Application";
            }
        }

        StringBuilder messages = new StringBuilder();
        public void AppendMessages(string message)
        {
            messages.Append(message);            
        }

        public bool LogAppendedMessages(EventLogEntryType entryType)
        {
            try
            {
                _logger.WriteEntry(messages.ToString(), entryType);
                return true;
            }
            catch
            {
                return false;
            }            
        }

        public bool Log(string message, EventLogEntryType entryType)
        {
            try
            {
                _logger.WriteEntry(message, entryType);
                messages.Clear();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}