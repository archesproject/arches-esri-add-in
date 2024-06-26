using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace arches_arcgispro_addin.Behaviours
{
    public static class DefaultBrowserBehaviour
    {
        public static void OpenBrowser(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                if (string.IsNullOrEmpty(StaticVariables.archesInstanceURL))
                {
                    return;
                }
                url = StaticVariables.archesInstanceURL;
            }
            Process process = new Process();
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.FileName = url;
            process.Start();
        }
    }
}
