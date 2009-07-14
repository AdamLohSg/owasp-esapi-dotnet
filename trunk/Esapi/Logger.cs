﻿/// <summary> OWASP .NET Enterprise Security API (.NET ESAPI)
/// 
/// This file is part of the Open Web Application Security Project (OWASP)
/// .NET Enterprise Security API (.NET ESAPI) project. For details, please see
/// http://www.owasp.org/index.php/Category:ESAPI.
/// 
/// Copyright (c) 2009 - The OWASP Foundation
/// 
/// The .NET ESAPI is published by OWASP under the BSD. You should read and accept the
/// LICENSE before you use, modify, and/or redistribute this software.
/// 
/// </summary>
/// <author>  Alex Smolen
/// </author>
/// <created>  2008 </created>

using System;
using System.Collections;
using Owasp.Esapi.Interfaces;
using System.Text;
using System.Diagnostics;
using log4net;
using System.Web.Security;

namespace Owasp.Esapi
{
    /// <summary>
    ///  These are fields for the logger class
    /// </summary>
    public class LogLevels
    {
        public static readonly int OFF = Int32.MaxValue;
        public static readonly int FATAL = 1000;
        public static readonly int ERROR = 800;
        public static readonly int WARN = 600;
        public static readonly int INFO = 400;
        public static readonly int DEBUG = 200;
        public static readonly int TRACE = 100;
        public static readonly int ALL = Int32.MinValue;
        public static int ParseLogLevel(string level)
        {
            if (level.ToUpper().Equals("FATAL", StringComparison.InvariantCultureIgnoreCase))
                return LogLevels.FATAL;
            if (level.ToUpper().Equals("ERROR", StringComparison.InvariantCultureIgnoreCase))
                return LogLevels.ERROR;
            if (level.ToUpper().Equals("WARNING", StringComparison.InvariantCultureIgnoreCase))
                return LogLevels.WARN;
            if (level.ToUpper().Equals("INFO", StringComparison.InvariantCultureIgnoreCase))
                return LogLevels.INFO;
            if (level.ToUpper().Equals("DEBUG", StringComparison.InvariantCultureIgnoreCase))
                return LogLevels.DEBUG;
            if (level.ToUpper().Equals("TRACE", StringComparison.InvariantCultureIgnoreCase))
                return LogLevels.TRACE;
            if (level.ToUpper().Equals("OFF", StringComparison.InvariantCultureIgnoreCase))
                return LogLevels.OFF;
            return LogLevels.ALL;
        }
    }
    
    public class LogEventTypes
    {
        public static readonly int SECURITY = 0;
        public static readonly int USABILITY = 1;
        public static readonly int PERFORMANCE = 2;
        public static readonly int FUNCTIONALITY = 3;
        public static string GetType(int type)
        {
            switch (type)
            {
                case 0:
                    return "SECURITY";    
                case 1:
                    return "USABILITY";
                case 2:
                    return "PERFORMANCE";
                case 3:
                    return "FUNCTIONALITY";
            }
            return "UNDEFINED";
        }
    }

    /// <summary> Reference implementation of the ILogger interface. This implementation uses the log4NET logging package, and marks each
    /// log message with the currently logged in user and the word "SECURITY" for security related events.
    /// 
    /// </summary>
    /// <author>  Alex Smolen (me@alexsmolen.com)
    /// </author>
    /// <since> Febraury 20, 2008
    /// </since>
    /// <seealso cref="Owasp.Esapi.Interfaces.ILogger">
    /// </seealso>
    public class Logger : ILogger
    {
        /// <summary>The Log4Net logger.</summary>
        private ILog logger = null;

        /// <summary>The application name.</summary>
        private string applicationName = null;

        /// <summary>The module name.</summary>
        private string moduleName = null;

        static Logger()
        {
            log4net.Config.XmlConfigurator.Configure();
        }

        /// <summaryThe constructor, which is hidden (private) and accessed through Esapi class.       
        /// </summary>
        public Logger(string className)
        {
            this.logger = log4net.LogManager.GetLogger(className);
            //log4net.LogManager.GetRepository().Threshold = (SecurityConfiguration) Esapi.SecurityConfiguration()).LogLevel;
        }
        
        /// <summaryThe constructor, which is hidden (private) and accessed through Esapi class.       
        /// </summary>
        /// <param name="applicationName">The application name.
        /// </param>
        /// <param name="moduleName">The module name.
        /// </param>
        private Logger(string applicationName, string moduleName)
        {
            this.applicationName = applicationName;
            this.moduleName = moduleName;            
        }
        
        /// <summary> Log the message after optionally encoding any special characters that might inject into an HTML based log viewer.
        /// This method accepts an exception.
        /// </summary>
        /// <param name="type">The type of event.
        /// </param>
        /// <param name="message">The message to log.
        /// </param>
        /// <param name="throwable">The exception to log.
        /// </param>
        private string GetLogMessage(int type, string message, Exception throwable)
        {
            MembershipUser user =  Membership.GetUser();
            
            // Ensure no CRLF injection into logs for forging records
            String clean = message.Replace('\n', '_').Replace('\r', '_');
            
            // HTML encode log message if it will be viewed in a web browser
            if (((SecurityConfiguration)Esapi.SecurityConfiguration).LogEncodingRequired)
            {
                clean = Esapi.Encoder.EncodeForHtml(message);
                if (!message.Equals(clean))
                {
                    clean += " (Encoded)";
                }
            }
            
            // Add a printable stack trace
            if (throwable != null)
            {                
                string fqn = throwable.GetType().FullName;
                int index = fqn.LastIndexOf('.');
                if (index > 0)
                    fqn = fqn.Substring(index + 1);
                StackTrace st = new StackTrace(throwable, true);
                
                // Note: Should we have exceptions with null stack traces?

                StackFrame[] frames = st.GetFrames();
                if (frames != null)
                {
                    StackFrame frame = frames[0];
                    clean += ("\n    " + throwable.Message + " - " + fqn + " @ " + "(" + frame.GetFileName() + ":" + frame.GetFileLineNumber() + ")");
                }
            }
            
            string msg;

            if (user != null)
            {
                msg = LogEventTypes.GetType(type) + ": " + user.UserName + ": " + clean;
            }
            else
            {
                msg = LogEventTypes.GetType(type) + ": "  + clean;
            }
        
            return msg;                    
        }

        #region ILogger Members

        private int level;
        
        public int Level
        {
            get
            {
                // TODO: Fix to return inner logger level
                return 0;
                //return logger.Logger;
            }
            set
            {
                // TODO: Fix to set inner logger level
                //level = value;
            }
        }


        public void Fatal(int type, string message)
        {

            if (logger.IsFatalEnabled)
            {
                logger.Fatal(GetLogMessage(type, message, null));
            }
        }

        public void Fatal(int type, string message, Exception exception)
        {
            if (logger.IsFatalEnabled)
            {
                logger.Fatal(GetLogMessage(type, message, exception));
            }
        }

        public bool IsFatalEnabled()
        {
            return (logger.IsFatalEnabled);
        }

        public void Error(int type, string message)
        {
            if (logger.IsErrorEnabled)
            {
                logger.Error(GetLogMessage(type, message, null));
            }
        }

        public void Error(int type, string message, Exception throwable)
        {
            if (logger.IsErrorEnabled)
            {
                logger.Error(GetLogMessage(type, message, throwable));
            }          
        }

        public bool IsErrorEnabled()
        {
            return (logger.IsErrorEnabled);
        }

        public void Warning(int type, string message)
        {
            if (logger.IsWarnEnabled)
            {
                logger.Warn(GetLogMessage(type, message, null));
            }
        }

        public void Warning(int type, string message, Exception throwable)
        {
            if (logger.IsWarnEnabled)
            {
                logger.Warn(GetLogMessage(type, message, throwable));
            }
        }

        public bool IsWarningEnabled()
        {
            return (logger.IsWarnEnabled);
        }

        public void Info(int type, string message)
        {
            if (logger.IsInfoEnabled)
            {
                logger.Info(GetLogMessage(type, message, null));
            }
        }

        public void Info(int type, string message, Exception throwable)
        {
            if (logger.IsInfoEnabled)
            {
                logger.Info(GetLogMessage(type, message, throwable));
            }
            
        }

        public bool IsInfoEnabled()
        {
            return logger.IsInfoEnabled;
        }

        public void Debug(int type, string message)
        {
            if (logger.IsDebugEnabled)
            {
                logger.Debug(GetLogMessage(type, message, null));
            }
        }

        public void Debug(int type, string message, Exception throwable)
        {
            if (logger.IsDebugEnabled)
            {
                logger.Debug(GetLogMessage(type, message, throwable));
            }
        }

        public bool IsDebugEnabled()
        {
            return logger.IsDebugEnabled;
        }

        public void Trace(int type, string message)
        {
            // TODO: Add new log level
            if (logger.IsDebugEnabled)
            {
                logger.Debug(GetLogMessage(type, message, null));
            }
        }

        public void Trace(int type, string message, Exception throwable)
        {
             // TODO: Add new log level
            if (logger.IsDebugEnabled)
            {
                logger.Debug(GetLogMessage(type, message, throwable));
            }
        }

        public bool IsTraceEnabled()
        {
             // TODO: Add new log level
            return logger.IsDebugEnabled;
        }

        #endregion
    }
}