using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Authenticator
{
    public static class AssemblyInfoHelper
    {
        public static string GetTitle()
        {
            Assembly currentAssem = typeof(MainWindow).Assembly;
            object[] attribs = currentAssem.GetCustomAttributes(typeof(AssemblyTitleAttribute), true);
            if (attribs.Length > 0)
            {
                return ((AssemblyTitleAttribute)attribs[0]).Title;
            }
            return "";
        }

        public static string GetDescription()
        {
            Assembly currentAssem = typeof(MainWindow).Assembly;
            object[] attribs = currentAssem.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), true);
            if (attribs.Length > 0)
            {
                return ((AssemblyDescriptionAttribute)attribs[0]).Description;
            }
            return "";
        }

        public static string GetCompany()
        {
            Assembly currentAssem = typeof(MainWindow).Assembly;
            object[] attribs = currentAssem.GetCustomAttributes(typeof(AssemblyCompanyAttribute), true);
            if (attribs.Length > 0)
            {
                return ((AssemblyCompanyAttribute)attribs[0]).Company;
            }
            return "";
        }

        public static string GetCopyright()
        {
            Assembly currentAssem = typeof(MainWindow).Assembly;
            object[] attribs = currentAssem.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), true);
            if (attribs.Length > 0)
            {
                return ((AssemblyCopyrightAttribute)attribs[0]).Copyright;
            }
            return "";
        }

        public static string GetURL()
        {
            Assembly currentAssem = typeof(MainWindow).Assembly;
            object[] attribs = currentAssem.GetCustomAttributes(typeof(AssemblyURLAttribute), true);
            if (attribs.Length > 0)
            {
                return ((AssemblyURLAttribute)attribs[0]).URL;
            }
            return "";
        }
    }

    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    [ComVisible(true)]
    public class AssemblyURLAttribute : Attribute
    {
        private string m_url;
        public string URL
        {
            get
            {
                return m_url;
            }
        }

        public AssemblyURLAttribute(string url)
        {
            m_url = url;
        }
    }
}
